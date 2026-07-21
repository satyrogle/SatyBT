namespace SatyBT
{
    /// <summary>
    /// Abstract base for every node in a SatyBT tree.
    /// Subclasses implement <see cref="Tick"/> and optionally override
    /// <see cref="OnEnter"/> / <see cref="OnExit"/>.
    /// No allocation occurs during the tick path.
    /// </summary>
    public abstract class NodeBase
    {
        /// <summary>Shared data store propagated from the owning tree.</summary>
        public Blackboard Blackboard { get; internal set; }

        /// <summary>Called once when the node first receives a tick after being idle.</summary>
        protected virtual void OnEnter() { }

        /// <summary>Called once when the node returns Success or Failure after Running.</summary>
        protected virtual void OnExit(BTStatus status) { }

        /// <summary>
        /// Reset node-local state (counters, cursors) back to idle defaults.
        /// Invoked by both <see cref="Reset"/> and <see cref="Abort"/>.
        /// Override in nodes that carry per-run state.
        /// </summary>
        protected virtual void OnReset() { }

        /// <summary>
        /// Cleanup hook fired when a running node is aborted (for example
        /// unsubscribing from external events). Default is a no-op.
        /// </summary>
        protected virtual void OnAbort() { }

        /// <summary>
        /// Evaluate this node. Must return Success, Failure, or Running.
        /// <paramref name="deltaTime"/> is the time in seconds since the
        /// previous tick, threaded down from the tree so time-based nodes
        /// (for example cooldowns) need no dependency on UnityEngine.Time.
        /// </summary>
        public abstract BTStatus Tick(float deltaTime);

        // ── Internal lifecycle tracking ──────────────────────────────

        private bool _isRunning;

        /// <summary>
        /// Called by the tree each frame. Manages enter/exit lifecycle
        /// around the subclass <see cref="Tick"/> implementation.
        /// </summary>
        internal BTStatus Update(float deltaTime)
        {
            if (!_isRunning)
            {
                OnEnter();
                _isRunning = true;
            }

            BTStatus status = Tick(deltaTime);

            if (status != BTStatus.Running)
            {
                OnExit(status);
                _isRunning = false;
            }

            return status;
        }

        /// <summary>
        /// Force this node — and, for composites and decorators, its whole
        /// subtree — back to idle without firing OnExit. Cascades so no
        /// descendant is left flagged as running. Used when reusing a subtree.
        /// </summary>
        internal virtual void Reset()
        {
            _isRunning = false;
            OnReset();
        }

        /// <summary>
        /// Interrupt a running node — and its whole subtree — firing
        /// <see cref="OnAbort"/> on any node that was running and returning
        /// every node to idle. Cascades so no descendant is left flagged as
        /// running. Used by the parallel composite, observer aborts, and node
        /// removal via <see cref="NodeInjector"/>.
        /// </summary>
        internal virtual void Abort()
        {
            if (_isRunning)
                OnAbort();
            _isRunning = false;
            OnReset();
        }
    }
}
