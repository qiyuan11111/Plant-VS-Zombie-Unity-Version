using UnityEngine;
using Script.Model;

namespace Prefab.Plant.SunShroom.Script
{
    [RequireComponent(typeof(SunProducer), typeof(SunShroomBlink))]
    public sealed class SunShroom : PlantEntity
    {
        [SerializeField] private SunProducer sunProducer;
        [SerializeField] private SunShroomBlink blink;

        public override Vector3 SpritePosition => new(42.275f, 45.875f, 0f);

        public override string GetChineseName()
        {
            return "阳光菇";
        }

        public override string GetEnglishName()
        {
            return "SunShroom";
        }

        protected override void OnEnteredBoard()
        {
            base.OnEnteredBoard();
            ResolveSunProducer().StartProducing();
            ResolveBlink().StartBlinking();
        }

        protected override void OnDestroy()
        {
            if (sunProducer != null)
            {
                sunProducer.StopProducing();
            }

            if (blink != null)
            {
                blink.StopBlinking();
            }

            base.OnDestroy();
        }

        private SunProducer ResolveSunProducer()
        {
            if (sunProducer == null)
            {
                sunProducer = GetComponent<SunProducer>();
            }

            if (sunProducer == null)
            {
                throw new MissingComponentException($"{name} requires a {nameof(SunProducer)} component.");
            }

            return sunProducer;
        }

        private SunShroomBlink ResolveBlink()
        {
            if (blink == null)
            {
                blink = GetComponent<SunShroomBlink>();
            }

            if (blink == null)
            {
                throw new MissingComponentException(
                    $"{name} requires a {nameof(SunShroomBlink)} component.");
            }

            return blink;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (sunProducer == null)
            {
                sunProducer = GetComponent<SunProducer>();
            }

            if (blink == null)
            {
                blink = GetComponent<SunShroomBlink>();
            }
        }
#endif
    }
}
