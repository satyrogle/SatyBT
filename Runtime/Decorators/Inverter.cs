namespace SatyBT
{
    /// <summary>
    /// Inverts the child's result: Success becomes Failure and vice versa.
    /// Running passes through unchanged.
    /// </summary>
    public sealed class Inverter : DecoratorNode
    {
        public Inverter(NodeBase child) : base(child) { }

        public override BTStatus Tick(float deltaTime)
        {
            BTStatus status = Child.Update(deltaTime);

            return status switch
            {
                BTStatus.Success => BTStatus.Failure,
                BTStatus.Failure => BTStatus.Success,
                _ => BTStatus.Running
            };
        }
    }
}
