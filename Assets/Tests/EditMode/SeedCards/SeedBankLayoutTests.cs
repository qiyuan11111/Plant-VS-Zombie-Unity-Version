using NUnit.Framework;
using PvZ.Gameplay.SeedCards;

namespace PvZ.Tests.EditMode.SeedCards
{
    public sealed class SeedBankLayoutTests
    {
        [TestCase(0, 444f, 369f)]
        [TestCase(5, 444f, 74f)]
        [TestCase(6, 444f, -15f)]
        [TestCase(8, 562f, -15f)]
        public void Calculate_ProducesStableWidthAndOffset(
            int cardCount,
            float expectedWidth,
            float expectedRightOffset)
        {
            var result = SeedBankLayout.Calculate(cardCount);

            Assert.That(result.Width, Is.EqualTo(expectedWidth));
            Assert.That(result.GroupRightOffset, Is.EqualTo(expectedRightOffset));
        }

        [Test]
        public void Calculate_ClampsNegativeCardCount()
        {
            var result = SeedBankLayout.Calculate(-3);

            Assert.That(result.Width, Is.EqualTo(444f));
            Assert.That(result.GroupRightOffset, Is.EqualTo(369f));
        }
    }
}
