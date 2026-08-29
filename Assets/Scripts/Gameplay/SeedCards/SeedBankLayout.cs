using UnityEngine;

namespace PvZ.Gameplay.SeedCards
{
    public static class SeedBankLayout
    {
        private const float BaseWidth = 444f;
        private const float CardStride = 59f;

        public static SeedBankLayoutResult Calculate(int cardCount)
        {
            cardCount = Mathf.Max(0, cardCount);
            return cardCount < 6
                ? new SeedBankLayoutResult(BaseWidth, (6 - cardCount) * CardStride + 15f)
                : new SeedBankLayoutResult(BaseWidth + (cardCount - 6) * CardStride, -15f);
        }
    }
}
