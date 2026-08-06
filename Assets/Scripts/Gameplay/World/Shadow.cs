using PvZ.Core.Entities;
using UnityEngine;

namespace PvZ.Gameplay.World
{
    public enum ShadowSizePreset
    {
        Large = 0,
        Small = 1
    }

    public class Shadow : WorldObject
    {
        public static readonly Vector2 SourceImageSize = new(86f, 36f);
        public const float LargeScale = 1f;
        public const float SmallScale = 0.5f;

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite daySprite;
        [SerializeField] private Sprite nightSprite;
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

        /// <summary>
        /// TodDrawImageCelCenterScaledF receives the unscaled image's top-left
        /// and always scales around its full 86x36 center. Source Y points down;
        /// Unity Y points up.
        /// </summary>
        public static Vector3 SourceTopLeftToUnityCenter(Vector2 sourceTopLeft)
        {
            var center = sourceTopLeft + SourceImageSize * 0.5f;
            return new Vector3(center.x, -center.y, 0f);
        }

        public Shadow Initialize(ShadowSizePreset preset)
        {
            return Initialize(GetScale(preset));
        }

        public Shadow Initialize(ShadowSizePreset preset, bool useNightSprite)
        {
            SetNight(useNightSprite);
            return Initialize(GetScale(preset));
        }

        public Shadow Initialize(float size)
        {
            SetSize(size);
            SetSortingLayer("shadow");
            return this;
        }

        public Shadow SetNight(bool useNightSprite)
        {
            ResolveSpriteRenderer();
            var sprite = useNightSprite && nightSprite != null ? nightSprite : daySprite;
            if (sprite != null) spriteRenderer.sprite = sprite;
            return this;
        }

        private void ResolveSpriteRenderer()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (spriteRenderer == null)
            {
                throw new MissingComponentException($"{name} requires a SpriteRenderer.");
            }

            if (daySprite == null) daySprite = spriteRenderer.sprite;
        }
    }
}
