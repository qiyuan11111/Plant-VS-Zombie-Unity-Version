using PvZ.Core.Entities;

namespace PvZ.Gameplay.World
{
    public enum ShadowSizePreset
    {
        Large = 0,
        Small = 1
    }

    public class Shadow : WorldObject
    {
        public const float LargeScale = 0.7f;
        public const float SmallScale = 0.5f;

        private float _size;
        
        public override string GetChineseName()
        {
            return "影子";
        }

        public override string GetEnglishName()
        {
            return "Shadow";
        }

        public Shadow SetSize(float size)
        {
            if (_size > 0f)
            {
                Transform.localScale /= _size;
            }

            _size = size;
            Transform.localScale *= size;
            return this;
        }

        public static float GetScale(ShadowSizePreset preset)
        {
            return preset == ShadowSizePreset.Small ? SmallScale : LargeScale;
        }

        public Shadow Initialize(ShadowSizePreset preset)
        {
            return Initialize(GetScale(preset));
        }

        public Shadow Initialize(float size)
        {
            SetSize(size);
            SetSortingLayer("shadow");
            return this;
        }
    }
}
