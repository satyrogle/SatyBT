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
        }
    }
}
