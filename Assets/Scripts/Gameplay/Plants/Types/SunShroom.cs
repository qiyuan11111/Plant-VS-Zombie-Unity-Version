using UnityEngine;
using PvZ.Gameplay.Plants;
using PvZ.Gameplay.Plants.Abilities;

namespace PvZ.Gameplay.Plants.Types
{
    [RequireComponent(typeof(SunProducer), typeof(Blink))]
    public sealed class SunShroom : PlantEntity
    {
        [SerializeField] private SunProducer sunProducer;
        [SerializeField] private Blink blink;

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

        private Blink ResolveBlink()
        {
            if (blink == null)
            {
                blink = GetComponent<Blink>();
            }

            if (blink == null)
            {
                throw new MissingComponentException(
                    $"{name} requires a {nameof(Blink)} component.");
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
                blink = GetComponent<Blink>();
            }
        }
#endif
    }
}
