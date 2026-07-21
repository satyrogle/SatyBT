using NUnit.Framework;

namespace SatyBT.Tests
{
    public class ResetAbortTests
    {
        [Test]
        public void Abort_CascadesThroughDecoratorsAndComposites()
        {
            var leaf = new ProbeNode(BTStatus.Running);
            var inner = new Inverter(leaf);
            var seq = new Sequence(inner);
            var tree = new BehaviourTree(seq);

            tree.Tick(0f); // whole subtree is running
            Assert.IsTrue(leaf.IsRunning);
            Assert.IsTrue(inner.IsRunning);
            Assert.IsTrue(seq.IsRunning);

            seq.Abort();

            Assert.IsFalse(leaf.IsRunning, "leaf must not be left running after abort");
            Assert.IsFalse(inner.IsRunning);
            Assert.IsFalse(seq.IsRunning);
            Assert.AreEqual(1, leaf.Aborts);
        }

        [Test]
        public void Reset_CascadesThroughChildren()
        {
            var leaf = new ProbeNode(BTStatus.Running);
            var seq = new Sequence(leaf);
            var tree = new BehaviourTree(seq);

            tree.Tick(0f);
            Assert.IsTrue(leaf.IsRunning);

            seq.Reset();

            Assert.IsFalse(leaf.IsRunning, "reset must clear running state throughout the subtree");
            Assert.IsFalse(seq.IsRunning);
        }

        [Test]
        public void RemovingRunningInjectedSubtree_LeavesNoRunningNodes()
        {
            var leaf = new ProbeNode(BTStatus.Running);
            var subtree = new Sequence(leaf);
            var root = new Selector(new ProbeNode(BTStatus.Failure));
            var tree = new BehaviourTree(root);

            tree.Injector.Inject("s", subtree, root, position: 0, durationTicks: 0);
            tree.Tick(0f); // injected subtree (higher priority) runs
            Assert.IsTrue(leaf.IsRunning);

            tree.Injector.Remove("s");

            Assert.IsFalse(leaf.IsRunning, "removed subtree must be fully aborted");
            Assert.AreEqual(1, leaf.Aborts);
        }

        [Test]
        public void ReRunningAResetSubtree_EntersFreshly()
        {
            var leaf = new ProbeNode(BTStatus.Success);
            var seq = new Sequence(leaf);
            var tree = new BehaviourTree(seq);

            tree.Tick(0f);                 // completes: leaf enters + exits once
            Assert.AreEqual(1, leaf.Enters);

            tree.Tick(0f);                 // fresh run
            Assert.AreEqual(2, leaf.Enters, "a completed sequence must re-enter its children on the next run");
        }
    }
}
