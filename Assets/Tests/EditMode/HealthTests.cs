using System;
using NUnit.Framework;
using Script.Combat;

namespace Tests.EditMode
{
    public sealed class HealthTests
    {
        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_RejectsNonPositiveMaximum(int maximum)
        {
            Assert.That(() => new Health(maximum), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void ApplyDamage_ClampsAtZeroAndReportsLethalHit()
        {
            var health = new Health(10);

            var result = health.ApplyDamage(15);

            Assert.That(result.RequestedAmount, Is.EqualTo(15));
            Assert.That(result.AppliedAmount, Is.EqualTo(10));
            Assert.That(result.RemainingHealth, Is.Zero);
            Assert.That(result.WasLethal, Is.True);
            Assert.That(result.WasIgnored, Is.False);
            Assert.That(health.Current, Is.Zero);
            Assert.That(health.IsDead, Is.True);
        }

        [Test]
        public void ApplyDamage_WhenAlreadyDead_IsIgnored()
        {
            var health = new Health(5);
            health.ApplyDamage(5);

            var result = health.ApplyDamage(2);

            Assert.That(result.AppliedAmount, Is.Zero);
            Assert.That(result.RemainingHealth, Is.Zero);
            Assert.That(result.WasLethal, Is.False);
            Assert.That(result.WasIgnored, Is.True);
        }

        [Test]
        public void Heal_ClampsAtMaximumAndCannotReviveDeadHealth()
        {
            var health = new Health(10);
            health.ApplyDamage(7);

            Assert.That(health.Heal(20), Is.EqualTo(7));
            Assert.That(health.Current, Is.EqualTo(10));

            health.ApplyDamage(10);
            Assert.That(health.Heal(5), Is.Zero);
            Assert.That(health.IsDead, Is.True);
        }

        [Test]
        public void Reset_ReinitializesMaximumAndCurrentHealth()
        {
            var health = new Health(10);
            health.ApplyDamage(4);

            health.Reset(25);

            Assert.That(health.Maximum, Is.EqualTo(25));
            Assert.That(health.Current, Is.EqualTo(25));
            Assert.That(health.IsDead, Is.False);
        }

        [Test]
        public void DamageAndHeal_RejectNegativeAmounts()
        {
            var health = new Health(10);

            Assert.That(() => health.ApplyDamage(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => health.Heal(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
