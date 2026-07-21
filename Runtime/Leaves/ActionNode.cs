using System;

namespace SatyBT
{
    /// <summary>
    /// Leaf node that executes a delegate returning <see cref="BTStatus"/>.
    /// Intended for quick prototyping; production code should subclass
    /// <see cref="NodeBase"/> with named action classes.
    /// </summary>
    public sealed class ActionNode : NodeBase
    {
        private readonly Func<BTStatus> _action;

        public ActionNode(Func<BTStatus> action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public override BTStatus Tick()
        {
            return _action();
        }
    }
}
