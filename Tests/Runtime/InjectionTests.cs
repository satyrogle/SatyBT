using NUnit.Framework;

namespace SatyBT.Tests
{
    public class InjectionTests
    {
        private static BehaviourTree BuildTree(out Selector root)
        {
            root = new Selector(new ProbeNode(BTStatus.Failure));
            return new BehaviourTree(root);
        }

        [Test]
        public void Inject_InsertsNodeAndReportsActive()
        {
            var tree = BuildTree(out var root);
            var injected = new ProbeNode(BTStatus.Running);

            var handle = tree.Injector.Inject("a", injected, root, position: 0, durationTicks: 0);

            Assert.IsNotNull(handle);
            Assert.IsTrue(tree.Injector.IsActive("a"));
            Assert.AreEqual(1, tree.Injector.ActiveCount);
            Assert.AreSame(injected, root.GetChild(0), "node must be inserted at the requested position");
            Assert.AreSame(tree.Blackboard, injected.Blackboard, "blackboard must propagate into the injected node");
        }

        [Test]
        public void Inject_DuplicateIdIsRejected()
        {
            var tree = BuildTree(out var root);
            tree.Injector.Inject("a", new ProbeNode(BTStatus.Running), root, 0, 0);

            var second = tree.Injector.Inject("a", new ProbeNode(BTStatus.Running), root, 0, 0);

            Assert.IsNull(second);
            Assert.AreEqual(1, tree.Injector.ActiveCount);
        }

        [Test]
        public void Inject_ExpiresAfterDurationAndAbortsSubtree()
        {
            var tree = BuildTree(out var root);
            var injected = new ProbeNode(BTStatus.Running);
            tree.Injector.Inject("a", injected, root, 0, durationTicks: 3);

            tree.Tick(0f); // TickCount 1
            tree.Tick(0f); // TickCount 2
            Assert.IsTrue(tree.Injector.IsActive("a"));

            tree.Tick(0f); // TickCount 3 -> expires
            Assert.IsFalse(tree.Injector.IsActive("a"));
            Assert.AreEqual(1, injected.Aborts, "expired subtree must be aborted");
        }

        [Test]
        public void Remove_FiresOnRemovedAndDeactivates()
        {
            var tree = BuildTree(out var root);
            var injected = new ProbeNode(BTStatus.Running);
            var handle = tree.Injector.Inject("a", injected, root, 0, 0);

            bool removedFired = false;
            handle.OnRemoved += _ => removedFired = true;

            Assert.IsTrue(tree.Injector.Remove("a"));
            Assert.IsTrue(removedFired);
            Assert.IsFalse(tree.Injector.IsActive("a"));
        }

        [Test]
        public void Remove_UnknownIdReturnsFalse()
        {
            var tree = BuildTree(out _);
            Assert.IsFalse(tree.Injector.Remove("nope"));
        }
    }
}
