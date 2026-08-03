using Prefab.Plant.SunShroom.Script;
using Script.Model;
using UnityEngine;

namespace Prefab.Plant.SunFlower.Script
{
    [RequireComponent(typeof(SunProducer), typeof(SunFlowerBlink))]
    public sealed class SunFlower : PlantEntity
    {
        [SerializeField] private SunProducer sunProducer;
        [SerializeField] private SunFlowerBlink blink;

        public override Vector3 SpritePosition => new(0f, 0f, 0f);

        public override string GetChineseName()
        {
            return "向日葵";
        }

        public override string GetEnglishName()
        {
            return "SunFlower";
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

        private SunFlowerBlink ResolveBlink()
        {
            if (blink == null)
            {
                blink = GetComponent<SunFlowerBlink>();
            }

            if (blink == null)
            {
                throw new MissingComponentException(
                    $"{name} requires a {nameof(SunFlowerBlink)} component.");
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
                blink = GetComponent<SunFlowerBlink>();
            }
        }
#endif
    }
}
