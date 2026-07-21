namespace SatyBT
{
    /// <summary>
    /// Ticks children left-to-right. Returns Success on the first child
    /// that succeeds. Returns Failure only if every child fails.
    /// Returns Running if a child is still running.
    /// </summary>
    public sealed class Selector : CompositeNode
    {
        private int _currentChild;

        /// <summary>Create an empty selector; add children with AddChild.</summary>
        public Selector() : base(8) { }

        /// <summary>Create a selector with the given children, in priority order.</summary>
        public Selector(params NodeBase[] children) : base(children) { }

        public override BTStatus Tick(float deltaTime)
        {
            _currentChild = ConsumePendingInterrupt(_currentChild);

            for (; _currentChild < ChildCount; _currentChild++)
            {
                BTStatus status = GetChild(_currentChild).Update(deltaTime);

                if (status == BTStatus.Running)
                    return BTStatus.Running;

                if (status == BTStatus.Success)
                {
                    _currentChild = 0;
                    return BTStatus.Success;
                }
            }

            _currentChild = 0;
            return BTStatus.Failure;
        }

        protected override void OnEnter()
        {
            _currentChild = 0;
        }

        protected override void OnReset()
        {
            _currentChild = 0;
        }
    }
}
