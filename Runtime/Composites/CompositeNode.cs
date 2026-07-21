using System.Collections.Generic;

namespace SatyBT
{
    /// <summary>
    /// Base class for nodes that hold an ordered list of children.
    /// Supports runtime insertion and removal (used by <see cref="NodeInjector"/>).
    /// The internal list is pre-allocated; no GC occurs on Insert/Remove.
    /// </summary>
    public abstract class CompositeNode : NodeBase
    {
        private readonly List<NodeBase> _children;

        // Index an observer has asked the composite to rewind to on its next
        // tick, or -1 if none pending. See RequestInterrupt / ConsumePendingInterrupt.
        private int _pendingInterrupt = -1;

        public int ChildCount => _children.Count;

        protected CompositeNode(int initialCapacity = 8)
        {
            _children = new List<NodeBase>(initialCapacity);
        }

        /// <summary>
        /// Construct with an initial set of children, added in order.
        /// Enables fluent tree building without repeated AddChild calls.
        /// </summary>
        protected CompositeNode(params NodeBase[] children)
        {
            int capacity = children != null && children.Length > 8 ? children.Length : 8;
            _children = new List<NodeBase>(capacity);
            if (children != null)
            {
                for (int i = 0; i < children.Length; i++)
                    AddChild(children[i]);
            }
        }

        public CompositeNode AddChild(NodeBase child)
        {
            child.Blackboard = Blackboard;
            child.Parent = this;
            _children.Add(child);
            return this;
        }

        public NodeBase GetChild(int index) => _children[index];

        /// <summary>Index of a child, or -1 if it is not present.</summary>
        public int IndexOf(NodeBase child) => _children.IndexOf(child);

        /// <summary>
        /// Insert a child at a specific index. Used by the injection system.
        /// </summary>
        public void Insert(int index, NodeBase child)
        {
            child.Blackboard = Blackboard;
            child.Parent = this;
            if (index >= _children.Count)
                _children.Add(child);
            else
                _children.Insert(index, child);
        }

        /// <summary>
        /// Remove a specific child node. Returns true if found and removed.
        /// </summary>
        public bool Remove(NodeBase child)
        {
            bool removed = _children.Remove(child);
            if (removed)
                child.Parent = null;
            return removed;
        }

        /// <summary>
        /// Ask this composite to rewind evaluation to <paramref name="fromIndex"/>
        /// on its next tick, aborting a lower-or-equal-priority running child.
        /// Called by an <see cref="ObserverDecorator"/> whose condition became
        /// true. Allocation-free; the request is stored as a single index and
        /// applied at the start of the next tick.
        /// </summary>
        internal void RequestInterrupt(int fromIndex)
        {
            if (fromIndex < 0)
                return;
            if (_pendingInterrupt < 0 || fromIndex < _pendingInterrupt)
                _pendingInterrupt = fromIndex;
        }

        /// <summary>
        /// Apply any pending interrupt for a cursor-based composite. If an
        /// interrupt targets a priority at or above the running child, the
        /// running child is aborted and the returned cursor is the interrupt
        /// index; otherwise the cursor is unchanged. Call at the start of Tick.
        /// </summary>
        protected int ConsumePendingInterrupt(int currentCursor)
        {
            int pending = _pendingInterrupt;
            if (pending < 0)
                return currentCursor;

            _pendingInterrupt = -1;

            // Ignore a request to rewind to a lower priority than the child
            // already running, so a running higher-priority child is never
            // abandoned without being aborted.
            if (pending > currentCursor)
                return currentCursor;

            if (currentCursor < _children.Count)
                _children[currentCursor].Abort();

            return pending;
        }

        /// <summary>Reset this composite and every child back to idle.</summary>
        internal override void Reset()
        {
            base.Reset();
            _pendingInterrupt = -1;
            for (int i = 0; i < _children.Count; i++)
                _children[i].Reset();
        }

        /// <summary>Abort this composite and every child, cascading cleanup.</summary>
        internal override void Abort()
        {
            base.Abort();
            _pendingInterrupt = -1;
            for (int i = 0; i < _children.Count; i++)
                _children[i].Abort();
        }

        private protected override void ActivateChildren()
        {
            for (int i = 0; i < _children.Count; i++)
                _children[i].Activate();
        }

        private protected override void DeactivateChildren()
        {
            for (int i = 0; i < _children.Count; i++)
                _children[i].Deactivate();
        }
    }
}
