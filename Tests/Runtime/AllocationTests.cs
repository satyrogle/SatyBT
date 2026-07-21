using NUnit.Framework;

namespace SatyBT.Tests
{
    public class AllocationTests
    {
        // Verifies the headline claim: ticking a fully-populated tree — every
        // node type, a live injection, and an active observer subscription —
        // allocates no managed memory on the steady-state tick path.
        [Test]
        public void Tick_DoesNotAllocate()
        {
            var tree = BuildTree();

            // Warm up so one-time allocations are done before measuring: JIT,
            // Parallel's completion buffer, the injector growing its child list,
            // and the observer's subscription.
            for (int i = 0; i < 10; i++)
                tree.Tick(0.016f);

            Assert.That(() => { tree.Tick(0.016f); },
                UnityEngine.TestTools.Constraints.Is.Not.AllocatingGCMemory());
        }

        private static BehaviourTree BuildTree()
        {
            BehaviourTree tree = null;

            // Exercises Sequence, Condition, Inverter, Parallel, Succeeder,
            // ActionNode, and an Observer subscription.
            var branch1 = new ObserverDecorator(
                new Sequence(
                    new Condition(() => tree.Blackboard.Get<bool>("flag")),
                    new Inverter(new Condition(() => tree.Blackboard.Get<bool>("never"))),
                    new Parallel(
                        Parallel.SuccessPolicy.RequireAll, Parallel.FailurePolicy.RequireOne,
                        new ActionNode(() => BTStatus.Success),
                        new Succeeder(new ActionNode(() => BTStatus.Failure)))),
                "flag", () => tree.Blackboard.Get<bool>("flag"),
                ObserverDecorator.AbortMode.LowerPriority);

            // These stay Running forever so the tree never settles and the
            // Repeater / Cooldown / Succeeder paths are exercised each tick.
            var branch2 = new Repeater(new ActionNode(() => BTStatus.Running), 0);
            var branch3 = new CooldownDecorator(new ActionNode(() => BTStatus.Running), 1f, true);
            var branch4 = new Succeeder(new ActionNode(() => BTStatus.Running));

            var root = new Parallel(
                Parallel.SuccessPolicy.RequireAll, Parallel.FailurePolicy.RequireOne,
                branch1, branch2, branch3, branch4);

            tree = new BehaviourTree(root);
            tree.Blackboard.Set("flag", true);
            tree.Blackboard.Set("never", false);

            // A long-lived injection so the injector always has an entry to scan.
            tree.Injector.Inject(
                "probe", new ActionNode(() => BTStatus.Running), root, 0, durationTicks: 1_000_000);

            return tree;
        }
    }
}
