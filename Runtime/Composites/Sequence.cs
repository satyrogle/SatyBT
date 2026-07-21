namespace SatyBT
{
    /// <summary>
    /// Ticks children left-to-right. Returns Failure on the first child
    /// that fails. Returns Success only if every child succeeds.
    /// Returns Running if a child is still running.
    /// </summary>
    public sealed class Sequence : CompositeNode
    {
        private int _currentChild;

        /// <summary>Create an empty sequence; add children with AddChild.</summary>
        public Sequence() : base(8) { }

        /// <summary>Create a sequence with the given children, in order.</summary>
        public Sequence(params NodeBase[] children) : base(children) { }

        public override BTStatus Tick(float deltaTime)
        {
            _currentChild = ConsumePendingInterrupt(_currentChild);

            for (; _currentChild < ChildCount; _currentChild++)
            {
                BTStatus status = GetChild(_currentChild).Update(deltaTime);

                if (status == BTStatus.Running)
                    return BTStatus.Running;

                if (status == BTStatus.Failure)
                {
                    _currentChild = 0;
                    return BTStatus.Failure;
                }
            }

            _currentChild = 0;
            return BTStatus.Success;
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
