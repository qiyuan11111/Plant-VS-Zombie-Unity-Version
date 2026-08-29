namespace PvZ.Gameplay.SeedCards
{
    public readonly struct SeedBankLayoutResult
    {
        public SeedBankLayoutResult(float width, float groupRightOffset)
        {
            Width = width;
            GroupRightOffset = groupRightOffset;
        }

        public float Width { get; }
        public float GroupRightOffset { get; }
    }
}
