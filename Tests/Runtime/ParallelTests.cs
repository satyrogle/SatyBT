using NUnit.Framework;

namespace SatyBT.Tests
{
    public class ParallelTests
    {
        [Test]
        public void RequireAllSuccess_ResolvesAcrossDifferentTicks()
        {
            var childA = new CountdownNode(2, BTStatus.Success); // succeeds on 3rd tick
            var childB = new CountdownNode(0, BTStatus.Success); // succeeds on 1st tick
            var parallel = new Parallel(
                Parallel.SuccessPolicy.RequireAll, Parallel.FailurePolicy.RequireOne, childA, childB);

            Assert.AreEqual(BTStatus.Running, parallel.Update(0f));
            Assert.AreEqual(BTStatus.Running, parallel.Update(0f));
            Assert.AreEqual(BTStatus.Success, parallel.Update(0f));

            Assert.AreEqual(1, childB.Ticks, "a finished child must not be re-ticked");
            Assert.AreEqual(3, childA.Ticks);
        }

        [Test]
        public void RequireOneFailure_FailsAndAbortsRunningChildren()
        {
            var running = new ProbeNode(BTStatus.Running);
            var failing = new ProbeNode(BTStatus.Failure);
            var parallel = new Parallel(
                Parallel.SuccessPolicy.RequireAll, Parallel.FailurePolicy.RequireOne, running, failing);

            Assert.AreEqual(BTStatus.Failure, parallel.Update(0f));
            Assert.AreEqual(1, running.Aborts, "still-running children must be aborted when the policy resolves");
        }

        [Test]
        public void RequireOneSuccess_SucceedsAndAbortsRunningChildren()
        {
            var running = new ProbeNode(BTStatus.Running);
            var succeeding = new ProbeNode(BTStatus.Success);
            var parallel = new Parallel(
                Parallel.SuccessPolicy.RequireOne, Parallel.FailurePolicy.RequireAll, running, succeeding);

            Assert.AreEqual(BTStatus.Success, parallel.Update(0f));
            Assert.AreEqual(1, running.Aborts);
        }

        [Test]
        public void RequireAllFailure_OnlyFailsWhenEveryChildFails()
        {
            var a = new CountdownNode(1, BTStatus.Failure); // fails on 2nd tick
            var b = new ProbeNode(BTStatus.Failure);        // fails on 1st tick
            var parallel = new Parallel(
                Parallel.SuccessPolicy.RequireAll, Parallel.FailurePolicy.RequireAll, a, b);

            Assert.AreEqual(BTStatus.Running, parallel.Update(0f)); // only b has failed
            Assert.AreEqual(BTStatus.Failure, parallel.Update(0f)); // now a has failed too
        }
    }
}
