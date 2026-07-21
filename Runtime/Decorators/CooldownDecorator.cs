namespace SatyBT
{
    /// <summary>
    /// Gates its child behind a cooldown. Once the child completes, the
    /// decorator returns Failure for <c>cooldownSeconds</c> without ticking
    /// the child again. The cooldown is driven by the deltaTime passed to
    /// <see cref="Tick"/>, so there is no dependency on UnityEngine.Time.
    ///
    /// When <c>onSuccessOnly</c> is true (the default) the cooldown starts
    /// only when the child succeeds; a child failure does not start it.
    /// When false, any completion (Success or Failure) starts the cooldown.
    /// The tick on which the child completes returns the child's real status;
    /// subsequent ticks return Failure until the cooldown elapses.
    /// </summary>
    public sealed class CooldownDecorator : DecoratorNode
    {
        private readonly float _cooldownSeconds;
        private readonly bool _onSuccessOnly;

        // Seconds of cooldown remaining. <= 0 means the child may run.
        private float _remaining;

        /// <param name="child">The gated child node.</param>
        /// <param name="cooldownSeconds">
        /// Cooldown length in seconds. 0 or negative disables the gate.
        /// </param>
        /// <param name="onSuccessOnly">
        /// True (default): start the cooldown only after the child succeeds.
        /// False: start it after any completion.
        /// </param>
        public CooldownDecorator(NodeBase child, float cooldownSeconds, bool onSuccessOnly = true)
            : base(child)
        {
            _cooldownSeconds = cooldownSeconds;
            _onSuccessOnly = onSuccessOnly;
        }

        public override BTStatus Tick(float deltaTime)
        {
            if (_remaining > 0f)
            {
                _remaining -= deltaTime;
                if (_remaining > 0f)
                    return BTStatus.Failure; // still cooling down; child not ticked
                _remaining = 0f;
            }

            BTStatus status = Child.Update(deltaTime);

            if (status == BTStatus.Running)
                return BTStatus.Running;

            bool startCooldown = _onSuccessOnly ? status == BTStatus.Success : true;
            if (startCooldown && _cooldownSeconds > 0f)
                _remaining = _cooldownSeconds;

            return status;
        }

        protected override void OnReset()
        {
            _remaining = 0f;
        }
    }
}
