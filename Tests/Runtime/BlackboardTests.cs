using NUnit.Framework;

namespace SatyBT.Tests
{
    public class BlackboardTests
    {
        [Test]
        public void TryGet_ReturnsValueForMatchingType()
        {
            var bb = new Blackboard();
            bb.Set("hp", 42);

            Assert.IsTrue(bb.TryGet<int>("hp", out int value));
            Assert.AreEqual(42, value);
        }

        [Test]
        public void TryGet_ReturnsFalseForMissingKey()
        {
            var bb = new Blackboard();
            Assert.IsFalse(bb.TryGet<int>("missing", out int value));
            Assert.AreEqual(0, value);
        }

        [Test]
        public void TryGet_ReturnsFalseForTypeMismatch()
        {
            var bb = new Blackboard();
            bb.Set("hp", 42);

            Assert.IsFalse(bb.TryGet<string>("hp", out string value));
            Assert.IsNull(value);
        }

        [Test]
        public void Get_ReturnsDefaultForMissingKey()
        {
            var bb = new Blackboard();
            Assert.AreEqual(0f, bb.Get<float>("missing"));
        }

        [Test]
        public void Get_ReturnsStoredValue()
        {
            var bb = new Blackboard();
            bb.Set("name", "guard");
            Assert.AreEqual("guard", bb.Get<string>("name"));
        }

        [Test]
        public void Subscribe_FiresCallbackOnSet_UnsubscribeStopsIt()
        {
            var bb = new Blackboard();
            int fires = 0;
            System.Action<string> callback = _ => fires++;

            bb.Subscribe("k", callback);
            bb.Set("k", 1);
            Assert.AreEqual(1, fires);

            bb.Unsubscribe("k", callback);
            bb.Set("k", 2);
            Assert.AreEqual(1, fires);
        }
    }
}
