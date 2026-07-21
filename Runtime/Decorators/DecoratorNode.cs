namespace SatyBT
{
    /// <summary>
    /// Base class for nodes that wrap a single child and modify its result.
    /// </summary>
    public abstract class DecoratorNode : NodeBase
    {
        public NodeBase Child { get; }

        protected DecoratorNode(NodeBase child)
        {
            Child = child;
            if (child != null)
                child.Parent = this;
        }

        /// <summary>Reset this decorator and its child back to idle.</summary>
        internal override void Reset()
        {
            base.Reset();
            Child?.Reset();
        }

        /// <summary>Abort this decorator and its child, cascading cleanup.</summary>
        internal override void Abort()
        {
            base.Abort();
            Child?.Abort();
        }

        private protected override void ActivateChildren() => Child?.Activate();

        private protected override void DeactivateChildren() => Child?.Deactivate();
    }
}
