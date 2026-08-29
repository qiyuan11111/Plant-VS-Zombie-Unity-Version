namespace PvZ.Gameplay.Sun
{
    public static class SunTypeCatalog
    {
        public static float GetScale(SunType type) => type == SunType.Small ? 0.5f : 1f;

        public static int GetValue(SunType type) => type == SunType.Small ? 15 : 25;

        public static SunType FromAnimationEvent(int typeNumber) =>
            typeNumber == 0 ? SunType.Small : SunType.Normal;
    }
}
