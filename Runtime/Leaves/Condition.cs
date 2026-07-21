using System;

namespace SatyBT
{
    /// <summary>
    /// Leaf node that evaluates a boolean predicate.
    /// Returns Success if the predicate is true, Failure otherwise.
    /// Never returns Running.
    /// Intended for quick prototyping; production code should subclass
    /// <see cref="NodeBase"/> with named condition classes.
    /// </summary>
    public sealed class Condition : NodeBase
    {
        private readonly Func<bool> _predicate;

        public Condition(Func<bool> predicate)
        {
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        public override BTStatus Tick(float deltaTime)
        {
            return _predicate() ? BTStatus.Success : BTStatus.Failure;
        }
    }
}
