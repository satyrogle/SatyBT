namespace SatyBT
{
    /// <summary>
    /// Repeats its child a fixed number of times, or indefinitely if
    /// <paramref name="repeatCount"/> is 0. Returns Running while repeating.
    /// Returns Success after completing all repetitions.
    /// If the child returns Failure, the repeater returns Failure immediately.
    /// </summary>
    public sealed class Repeater : DecoratorNode
    {
        private readonly int _repeatCount;
        private int _currentCount;

        /// <param name="child">The child node to repeat.</param>
        /// <param name="repeatCount">
        /// Number of times to repeat. 0 = infinite (always returns Running).
        /// </param>
        public Repeater(NodeBase child, int repeatCount = 0) : base(child)
        {
            _repeatCount = repeatCount;
        }

        public override BTStatus Tick()
        {
            BTStatus status = Child.Update();

            if (status == BTStatus.Failure)
                return BTStatus.Failure;

            if (status == BTStatus.Running)
                return BTStatus.Running;

            // Child succeeded
            _currentCount++;

            if (_repeatCount > 0 && _currentCount >= _repeatCount)
            {
                _currentCount = 0;
                return BTStatus.Success;
            }

            // Keep going
            return BTStatus.Running;
        }

        protected override void OnEnter()
        {
            _currentCount = 0;
        }

        protected override void OnReset()
        {
            _currentCount = 0;
        }
    }
}
