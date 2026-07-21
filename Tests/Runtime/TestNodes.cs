namespace SatyBT.Tests
{
    /// <summary>
    /// Leaf that returns a fixed status and counts lifecycle callbacks.
    /// Used to assert exactly which nodes were ticked, entered, or aborted.
    /// </summary>
    internal sealed class ProbeNode : NodeBase
    {
        public BTStatus Result;
        public int Ticks;
        public int Enters;
        public int Exits;
        public int Aborts;
        public int Resets;

        public ProbeNode(BTStatus result = BTStatus.Running)
        {
            Result = result;
        }

        public override BTStatus Tick(float deltaTime)
        {
            Ticks++;
            return Result;
        }

        protected override void OnEnter() => Enters++;
        protected override void OnExit(BTStatus status) => Exits++;
        protected override void OnAbort() => Aborts++;
        protected override void OnReset() => Resets++;
    }

    /// <summary>
    /// Leaf that returns Running for a fixed number of ticks, then a final
    /// status. Resets its countdown on enter, so it can run more than once.
    /// </summary>
    internal sealed class CountdownNode : NodeBase
    {
        private readonly int _runningTicks;
        private readonly BTStatus _final;
        private int _count;

        public int Ticks;
        public int Enters;
        public int Aborts;

        public CountdownNode(int runningTicks, BTStatus final)
        {
            _runningTicks = runningTicks;
            _final = final;
        }

        public override BTStatus Tick(float deltaTime)
        {
            Ticks++;
            if (_count < _runningTicks)
            {
                _count++;
                return BTStatus.Running;
            }
            return _final;
        }

        protected override void OnEnter()
        {
            Enters++;
            _count = 0;
        }

        protected override void OnReset() => _count = 0;
        protected override void OnAbort() => Aborts++;
    }
}
