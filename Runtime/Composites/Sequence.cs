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

        public override BTStatus Tick()
        {
            for (; _currentChild < ChildCount; _currentChild++)
            {
                BTStatus status = GetChild(_currentChild).Update();

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
