using NUnit.Framework;

namespace SatyBT.Tests
{
    public class ObserverTests
    {
        [Test]
        public void Self_AbortsSubtreeWhenConditionDrops()
        {
            var reaction = new ProbeNode(BTStatus.Running);
            BehaviourTree tree = null; // captured by the condition; assigned before any tick
            var observer = new ObserverDecorator(
                reaction, "guard",
                () => tree.Blackboard.Get<bool>("guard"),
                ObserverDecorator.AbortMode.Self);

            tree = new BehaviourTree(observer);
            tree.Blackboard.Set("guard", true);

            Assert.AreEqual(BTStatus.Running, tree.Tick(0f)); // guard holds: subtree runs
            Assert.AreEqual(1, reaction.Enters);

            tree.Blackboard.Set("guard", false);               // change fires the observer
            Assert.AreEqual(BTStatus.Failure, tree.Tick(0f));  // subtree aborted
            Assert.AreEqual(1, reaction.Aborts);
        }

        [Test]
        public void LowerPriority_PreemptsRunningLowerPrioritySibling()
        {
            var reaction = new ProbeNode(BTStatus.Running);
            var patrol = new ProbeNode(BTStatus.Running);
            BehaviourTree tree = null;
            var observer = new ObserverDecorator(
                reaction, "alert",
                () => tree.Blackboard.Get<bool>("alert"),
                ObserverDecorator.AbortMode.LowerPriority);

            var root = new Selector(observer, patrol);
            tree = new BehaviourTree(root);

            Assert.AreEqual(BTStatus.Running, tree.Tick(0f)); // alert false: observer fails, patrol runs
            Assert.AreEqual(1, patrol.Enters);
            Assert.AreEqual(0, reaction.Enters);

            tree.Blackboard.Set("alert", true);                // condition becomes true
            Assert.AreEqual(BTStatus.Running, tree.Tick(0f));  // rewinds to observer, aborts patrol
            Assert.AreEqual(1, patrol.Aborts);
            Assert.AreEqual(1, reaction.Enters);
        }

        [Test]
        public void None_ActsAsPlainGuardAndNeverSubscribes()
        {
            var reaction = new ProbeNode(BTStatus.Running);
            BehaviourTree tree = null;
            var observer = new ObserverDecorator(
                reaction, "guard",
                () => tree.Blackboard.Get<bool>("guard"),
                ObserverDecorator.AbortMode.None);

            tree = new BehaviourTree(observer);

            Assert.AreEqual(BTStatus.Failure, tree.Tick(0f)); // guard false → Failure, child not ticked
            Assert.AreEqual(0, reaction.Enters);

            tree.Blackboard.Set("guard", true);
            Assert.AreEqual(BTStatus.Running, tree.Tick(0f)); // guard true → child runs
            Assert.AreEqual(1, reaction.Enters);
        }
    }
}
