using System;

namespace SatyBT
{
    /// <summary>
    /// A guard decorator with reactive "observer abort" behaviour. It watches
    /// one blackboard key and re-evaluates a condition whenever that key
    /// changes, letting a subtree react to state changes without polling.
    ///
    /// As a guard: while ticked, the decorator runs its child only when the
    /// condition holds; otherwise it returns Failure without ticking the child.
    ///
    /// Abort behaviour is selected with <see cref="AbortMode"/>:
    /// <list type="bullet">
    /// <item><b>Self</b> — if the condition stops holding while the child
    /// subtree is running, the subtree is aborted on the next tick.</item>
    /// <item><b>LowerPriority</b> — when the condition becomes true, the
    /// decorator asks its nearest composite ancestor to rewind to this
    /// decorator's branch on the next tick, aborting any lower-priority child
    /// that was running. This is how a high-priority reaction (e.g. "player
    /// spotted") pre-empts a lower-priority behaviour (e.g. "patrol").</item>
    /// <item><b>Both</b> — both of the above.</item>
    /// <item><b>None</b> — pure guard, no subscription.</item>
    /// </list>
    ///
    /// Subscription lifecycle: the decorator subscribes to the watched key when
    /// it becomes part of a live tree (<see cref="NodeBase.OnActivated"/>) and
    /// unsubscribes when removed (<see cref="NodeBase.OnDeactivated"/>) — not on
    /// enter/exit. This is deliberate: a LowerPriority observer must keep
    /// watching while a lower-priority sibling runs and the observer itself is
    /// not being ticked, which enter/exit subscription cannot do. Injected
    /// subtrees subscribe on injection and unsubscribe on removal via the same
    /// activation hooks. The change callback is cached once at construction, so
    /// nothing allocates on the tick path.
    /// </summary>
    public sealed class ObserverDecorator : DecoratorNode
    {
        /// <summary>Which running nodes an observer may abort.</summary>
        public enum AbortMode
        {
            /// <summary>Guard only; never subscribes or aborts.</summary>
            None,

            /// <summary>Abort this subtree when the condition stops holding.</summary>
            Self,

            /// <summary>Pre-empt lower-priority siblings when the condition becomes true.</summary>
            LowerPriority,

            /// <summary>Both Self and LowerPriority.</summary>
            Both
        }

        private readonly string _key;
        private readonly Func<bool> _condition;
        private readonly AbortMode _abortMode;
        private readonly Action<string> _onKeyChanged; // cached to avoid per-callback allocation

        private bool _subscribed;
        private bool _selfAbortRequested;

        /// <param name="child">The guarded child subtree.</param>
        /// <param name="key">Blackboard key to watch for changes.</param>
        /// <param name="condition">
        /// Predicate evaluated on each change (and each tick) to decide whether
        /// the guard holds. Typically reads <paramref name="key"/> from the
        /// blackboard.
        /// </param>
        /// <param name="abortMode">Which aborts this observer performs.</param>
        public ObserverDecorator(NodeBase child, string key, Func<bool> condition, AbortMode abortMode)
            : base(child)
        {
            _key = key;
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
            _abortMode = abortMode;
            _onKeyChanged = OnKeyChanged;
        }

        public override BTStatus Tick(float deltaTime)
        {
            if (_selfAbortRequested)
            {
                _selfAbortRequested = false;
                Child.Abort();
                return BTStatus.Failure;
            }

            if (!_condition())
                return BTStatus.Failure;

            return Child.Update(deltaTime);
        }

        protected override void OnActivated()
        {
            if (_subscribed || _abortMode == AbortMode.None || Blackboard == null)
                return;
            Blackboard.Subscribe(_key, _onKeyChanged);
            _subscribed = true;
        }

        protected override void OnDeactivated()
        {
            if (!_subscribed || Blackboard == null)
                return;
            Blackboard.Unsubscribe(_key, _onKeyChanged);
            _subscribed = false;
        }

        protected override void OnReset()
        {
            _selfAbortRequested = false;
        }

        // Fired off the tick path when the watched key changes.
        private void OnKeyChanged(string key)
        {
            bool conditionMet = _condition();

            if (conditionMet &&
                (_abortMode == AbortMode.LowerPriority || _abortMode == AbortMode.Both))
            {
                RequestParentInterrupt();
            }

            if (!conditionMet && IsRunning &&
                (_abortMode == AbortMode.Self || _abortMode == AbortMode.Both))
            {
                _selfAbortRequested = true;
            }
        }

        // Walk up to the nearest composite ancestor and ask it to rewind to
        // the branch that contains this decorator. Pointer-chasing only; no
        // allocation.
        private void RequestParentInterrupt()
        {
            NodeBase branch = this;
            NodeBase parent = Parent;
            while (parent != null && !(parent is CompositeNode))
            {
                branch = parent;
                parent = parent.Parent;
            }

            if (parent is CompositeNode composite)
            {
                int index = composite.IndexOf(branch);
                if (index >= 0)
                    composite.RequestInterrupt(index);
            }
        }
    }
}
