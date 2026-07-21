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

        public int ChildCount => _children.Count;

        protected CompositeNode(int initialCapacity = 8)
        {
            _children = new List<NodeBase>(initialCapacity);
        }

        public CompositeNode AddChild(NodeBase child)
        {
            child.Blackboard = Blackboard;
            _children.Add(child);
            return this;
        }

        public NodeBase GetChild(int index) => _children[index];

        /// <summary>
        /// Insert a child at a specific index. Used by the injection system.
        /// </summary>
        public void Insert(int index, NodeBase child)
        {
            child.Blackboard = Blackboard;
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
            return _children.Remove(child);
        }

        /// <summary>Reset this composite and every child back to idle.</summary>
        internal override void Reset()
        {
            base.Reset();
            for (int i = 0; i < _children.Count; i++)
                _children[i].Reset();
        }

        /// <summary>Abort this composite and every child, cascading cleanup.</summary>
        internal override void Abort()
        {
            base.Abort();
            for (int i = 0; i < _children.Count; i++)
                _children[i].Abort();
        }
    }
}
