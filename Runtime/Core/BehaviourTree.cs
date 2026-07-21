namespace SatyBT
{
    /// <summary>
    /// Container for a behaviour tree. Holds the root node, the shared
    /// <see cref="Blackboard"/>, and the <see cref="NodeInjector"/>.
    /// Call <see cref="Tick"/> once per frame (or at your desired frequency).
    /// </summary>
    public sealed class BehaviourTree
    {
        public NodeBase Root { get; }
        public Blackboard Blackboard { get; }
        public NodeInjector Injector { get; }

        /// <summary>Total ticks since this tree was created.</summary>
        public int TickCount { get; private set; }

        public BehaviourTree(NodeBase root)
        {
            Root = root;
            Blackboard = new Blackboard();
            Injector = new NodeInjector(this);
            PropagateBlackboard(root);
        }

        /// <summary>
        /// Tick the tree once, advancing it by <paramref name="deltaTime"/>
        /// seconds. Returns the root node's status.
        /// </summary>
        public BTStatus Tick(float deltaTime)
        {
            TickCount++;
            Injector.ProcessExpirations(TickCount);
            return Root.Update(deltaTime);
        }

        /// <summary>
        /// Recursively assign the blackboard to a node and its children.
        /// </summary>
        internal void PropagateBlackboard(NodeBase node)
        {
            node.Blackboard = Blackboard;

            if (node is CompositeNode composite)
            {
                for (int i = 0; i < composite.ChildCount; i++)
                    PropagateBlackboard(composite.GetChild(i));
            }
            else if (node is DecoratorNode decorator && decorator.Child != null)
            {
                PropagateBlackboard(decorator.Child);
            }
        }
    }
}
