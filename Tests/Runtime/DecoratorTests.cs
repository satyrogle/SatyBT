using NUnit.Framework;

namespace SatyBT.Tests
{
    public class DecoratorTests
    {
        [Test]
        public void Inverter_FlipsSuccessAndFailure()
        {
            Assert.AreEqual(BTStatus.Failure, new Inverter(new ProbeNode(BTStatus.Success)).Update(0f));
            Assert.AreEqual(BTStatus.Success, new Inverter(new ProbeNode(BTStatus.Failure)).Update(0f));
        }

        [Test]
        public void Inverter_PassesRunningThrough()
        {
            Assert.AreEqual(BTStatus.Running, new Inverter(new ProbeNode(BTStatus.Running)).Update(0f));
        }

        [Test]
        public void Succeeder_AlwaysSucceedsOnCompletion()
        {
            Assert.AreEqual(BTStatus.Success, new Succeeder(new ProbeNode(BTStatus.Failure)).Update(0f));
            Assert.AreEqual(BTStatus.Success, new Succeeder(new ProbeNode(BTStatus.Success)).Update(0f));
        }

        [Test]
        public void Succeeder_PassesRunningThrough()
        {
            Assert.AreEqual(BTStatus.Running, new Succeeder(new ProbeNode(BTStatus.Running)).Update(0f));
        }

        [Test]
        public void Repeater_RepeatsFixedCountThenSucceeds()
        {
            var child = new ProbeNode(BTStatus.Success);
            var repeater = new Repeater(child, 3);

            Assert.AreEqual(BTStatus.Running, repeater.Update(0f));
            Assert.AreEqual(BTStatus.Running, repeater.Update(0f));
            Assert.AreEqual(BTStatus.Success, repeater.Update(0f));
            Assert.AreEqual(3, child.Ticks);
        }

        [Test]
        public void Repeater_FailsImmediatelyOnChildFailure()
        {
            var child = new ProbeNode(BTStatus.Failure);
            Assert.AreEqual(BTStatus.Failure, new Repeater(child, 3).Update(0f));
            Assert.AreEqual(1, child.Ticks);
        }

        [Test]
        public void Cooldown_GatesChildAfterSuccess()
        {
            var child = new ProbeNode(BTStatus.Success);
            var cooldown = new CooldownDecorator(child, cooldownSeconds: 1f, onSuccessOnly: true);

            Assert.AreEqual(BTStatus.Success, cooldown.Update(0.6f));  // runs, starts cooldown
            Assert.AreEqual(BTStatus.Failure, cooldown.Update(0.6f));  // cooling down, child not ticked
            Assert.AreEqual(BTStatus.Success, cooldown.Update(0.6f));  // cooldown elapsed, runs again

            Assert.AreEqual(2, child.Ticks, "child must be skipped while cooling down");
        }

        [Test]
        public void Cooldown_DoesNotGateOnFailureByDefault()
        {
            var child = new ProbeNode(BTStatus.Failure);
            var cooldown = new CooldownDecorator(child, cooldownSeconds: 1f, onSuccessOnly: true);

            Assert.AreEqual(BTStatus.Failure, cooldown.Update(0.6f));
            Assert.AreEqual(BTStatus.Failure, cooldown.Update(0.6f));
            Assert.AreEqual(2, child.Ticks, "no cooldown should start after a failure when onSuccessOnly");
        }
    }
}
