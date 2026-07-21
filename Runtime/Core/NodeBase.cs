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

        /// <summary>Evaluate this node. Must return Success, Failure, or Running.</summary>
        public abstract BTStatus Tick();

        // ── Internal lifecycle tracking ──────────────────────────────

        private bool _isRunning;

        /// <summary>
        /// Called by the tree each frame. Manages enter/exit lifecycle
        /// around the subclass <see cref="Tick"/> implementation.
        /// </summary>
        internal BTStatus Update()
        {
            if (!_isRunning)
            {
                OnEnter();
                _isRunning = true;
            }

            BTStatus status = Tick();

            if (status != BTStatus.Running)
            {
                OnExit(status);
                _isRunning = false;
            }

            return status;
        }

        /// <summary>
        /// Force-reset this node to idle state without calling OnExit.
        /// Used by abort and injection systems.
        /// </summary>
        internal void Reset()
        {
            _isRunning = false;
        }
    }
}
