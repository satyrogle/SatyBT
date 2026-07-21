using NUnit.Framework;

namespace SatyBT.Tests
{
    public class CompositeTests
    {
        [Test]
        public void Sequence_ShortCircuitsOnFailure()
        {
            var c0 = new ProbeNode(BTStatus.Success);
            var c1 = new ProbeNode(BTStatus.Failure);
            var c2 = new ProbeNode(BTStatus.Success);
            var seq = new Sequence(c0, c1, c2);

            Assert.AreEqual(BTStatus.Failure, seq.Update(0f));
            Assert.AreEqual(1, c0.Ticks);
            Assert.AreEqual(1, c1.Ticks);
            Assert.AreEqual(0, c2.Ticks, "child after the failing one must not be ticked");
        }

        [Test]
        public void Sequence_SucceedsWhenAllSucceed()
        {
            var seq = new Sequence(new ProbeNode(BTStatus.Success), new ProbeNode(BTStatus.Success));
            Assert.AreEqual(BTStatus.Success, seq.Update(0f));
        }

        [Test]
        public void Selector_ShortCircuitsOnSuccess()
        {
            var c0 = new ProbeNode(BTStatus.Failure);
            var c1 = new ProbeNode(BTStatus.Success);
            var c2 = new ProbeNode(BTStatus.Failure);
            var sel = new Selector(c0, c1, c2);

            Assert.AreEqual(BTStatus.Success, sel.Update(0f));
            Assert.AreEqual(1, c0.Ticks);
            Assert.AreEqual(1, c1.Ticks);
            Assert.AreEqual(0, c2.Ticks, "child after the succeeding one must not be ticked");
        }

        [Test]
        public void Selector_FailsWhenAllFail()
        {
            var sel = new Selector(new ProbeNode(BTStatus.Failure), new ProbeNode(BTStatus.Failure));
            Assert.AreEqual(BTStatus.Failure, sel.Update(0f));
        }

        [Test]
        public void Selector_ResumesAtRunningChild()
        {
            var c0 = new ProbeNode(BTStatus.Failure);
            var c1 = new ProbeNode(BTStatus.Running);
            var sel = new Selector(c0, c1);

            Assert.AreEqual(BTStatus.Running, sel.Update(0f));
            Assert.AreEqual(BTStatus.Running, sel.Update(0f));

            Assert.AreEqual(1, c0.Ticks, "higher-priority child must not be re-evaluated while a lower one runs");
            Assert.AreEqual(2, c1.Ticks);
        }

        [Test]
        public void Sequence_ResumesAtRunningChild()
        {
            var c0 = new ProbeNode(BTStatus.Success);
            var c1 = new ProbeNode(BTStatus.Running);
            var seq = new Sequence(c0, c1);

            Assert.AreEqual(BTStatus.Running, seq.Update(0f));
            Assert.AreEqual(BTStatus.Running, seq.Update(0f));

            Assert.AreEqual(1, c0.Ticks, "earlier child must not be re-run while a later one runs");
            Assert.AreEqual(2, c1.Ticks);
        }
    }
}
