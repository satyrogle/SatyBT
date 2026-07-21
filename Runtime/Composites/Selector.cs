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

        public override BTStatus Tick()
        {
            for (; _currentChild < ChildCount; _currentChild++)
            {
                BTStatus status = GetChild(_currentChild).Update();

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
