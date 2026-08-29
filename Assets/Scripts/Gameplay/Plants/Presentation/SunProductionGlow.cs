using PvZ.Presentation.Rendering;
using PvZ.Gameplay.Presentation.EntityPreviews;
using UnityEngine;

namespace PvZ.Gameplay.Plants.Presentation
{
    /// <summary>Captures, applies, and restores the producer's fallback glow.</summary>
    internal sealed class SunProductionGlow
    {
        private readonly SpriteTransform[] _parts;
        private readonly float[] _brightness;

        private SunProductionGlow(SpriteTransform[] parts)
        {
            _parts = parts;
            _brightness = new float[parts.Length];
            for (var index = 0; index < parts.Length; index++)
            {
                _brightness[index] = parts[index].brightness;
            }
        }

        internal static SunProductionGlow Capture(Transform root)
        {
            return new SunProductionGlow(root.GetComponentsInChildren<SpriteTransform>(true));
        }

        internal void Apply(float multiplier)
        {
            for (var index = 0; index < _parts.Length; index++)
            {
                var part = _parts[index];
                if (part == null) continue;

                part.brightness = _brightness[index] * multiplier;
                part.Apply();
            }
        }

        internal void Restore()
        {
            for (var index = 0; index < _parts.Length; index++)
            {
                var part = _parts[index];
                if (part == null) continue;

                part.brightness = _brightness[index];
                part.Apply();
            }
        }
    }
}
