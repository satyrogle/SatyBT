namespace SatyBT
{
    /// <summary>
    /// Always returns Success after the child finishes, regardless of
    /// whether the child returned Success or Failure.
    /// Running passes through unchanged.
    /// </summary>
    public sealed class Succeeder : DecoratorNode
    {
        public Succeeder(NodeBase child) : base(child) { }

        public override BTStatus Tick()
        {
            BTStatus status = Child.Update();
            return status == BTStatus.Running ? BTStatus.Running : BTStatus.Success;
        }
    }
}
