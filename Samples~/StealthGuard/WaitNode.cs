namespace SatyBT.Samples
{
    /// <summary>
    /// Leaf that returns Running until a duration has elapsed, then Success.
    /// Demonstrates using the deltaTime threaded through Tick instead of
    /// reaching for UnityEngine.Time, and resetting per-run state on enter.
    /// </summary>
    public sealed class WaitNode : NodeBase
    {
        private readonly float _seconds;
        private float _elapsed;

        public WaitNode(float seconds)
        {
            _seconds = seconds;
        }

        public override BTStatus Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            return _elapsed >= _seconds ? BTStatus.Success : BTStatus.Running;
        }

        protected override void OnEnter() => _elapsed = 0f;
        protected override void OnReset() => _elapsed = 0f;
    }
}
