using NUnit.Framework;
using PvZ.Gameplay.SeedCards;

namespace PvZ.Tests.EditMode.SeedCards
{
    public sealed class SeedCardCooldownTests
    {
        [Test]
        public void Start_BeginsAtFullRemainingProgress()
        {
            var cooldown = new SeedCardCooldown();

            cooldown.Start(4f);

            Assert.That(cooldown.Duration, Is.EqualTo(4f));
            Assert.That(cooldown.Remaining, Is.EqualTo(4f));
            Assert.That(cooldown.Progress, Is.EqualTo(1f));
            Assert.That(cooldown.IsReady, Is.False);
        }

        [Test]
        public void Tick_DecrementsAndClampsAtReady()
        {
            var cooldown = new SeedCardCooldown();
            cooldown.Start(4f);

            Assert.That(cooldown.Tick(1.5f), Is.True);
            Assert.That(cooldown.Remaining, Is.EqualTo(2.5f).Within(0.0001f));
            Assert.That(cooldown.Progress, Is.EqualTo(0.625f).Within(0.0001f));

            Assert.That(cooldown.Tick(10f), Is.True);
            Assert.That(cooldown.Remaining, Is.Zero);
            Assert.That(cooldown.Progress, Is.Zero);
            Assert.That(cooldown.IsReady, Is.True);
            Assert.That(cooldown.Tick(1f), Is.False);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void Tick_WithNonPositiveDelta_DoesNotChangeState(float deltaTime)
        {
            var cooldown = new SeedCardCooldown();
            cooldown.Start(3f);

            Assert.That(cooldown.Tick(deltaTime), Is.False);
            Assert.That(cooldown.Remaining, Is.EqualTo(3f));
        }

        [Test]
        public void Start_ClampsNegativeDurationToReadyState()
        {
            var cooldown = new SeedCardCooldown();

            cooldown.Start(-3f);

            Assert.That(cooldown.Duration, Is.Zero);
            Assert.That(cooldown.Remaining, Is.Zero);
            Assert.That(cooldown.Progress, Is.Zero);
            Assert.That(cooldown.IsReady, Is.True);
        }

        [Test]
        public void Start_WhileCoolingDown_RestartsWithNewDuration()
        {
            var cooldown = new SeedCardCooldown();
            cooldown.Start(10f);
            cooldown.Tick(4f);

            cooldown.Start(2f);

            Assert.That(cooldown.Duration, Is.EqualTo(2f));
            Assert.That(cooldown.Remaining, Is.EqualTo(2f));
            Assert.That(cooldown.Progress, Is.EqualTo(1f));
        }
    }
}
