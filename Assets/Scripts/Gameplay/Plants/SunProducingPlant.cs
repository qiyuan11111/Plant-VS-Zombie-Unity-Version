using PvZ.Gameplay.Plants.Abilities;
using UnityEngine;

namespace PvZ.Gameplay.Plants
{
    [RequireComponent(typeof(SunProducer), typeof(Blink))]
    public abstract class SunProducingPlant : PlantEntity
    {
        [SerializeField] private SunProducer sunProducer;
        [SerializeField] private Blink blink;

        protected override void OnEnteredBoard()
        {
            base.OnEnteredBoard();
            ResolveSunProducer().StartProducing();
            ResolveBlink().StartBlinking();
        }

        protected override void OnDestroy()
        {
            if (sunProducer != null) sunProducer.StopProducing();
            if (blink != null) blink.StopBlinking();
            base.OnDestroy();
        }

        private SunProducer ResolveSunProducer()
        {
            if (sunProducer == null) sunProducer = GetComponent<SunProducer>();
            if (sunProducer == null)
            {
                throw new MissingComponentException(
                    $"{name} requires a {nameof(SunProducer)} component.");
            }

            return sunProducer;
        }

        private Blink ResolveBlink()
        {
            if (blink == null) blink = GetComponent<Blink>();
            if (blink == null)
            {
                throw new MissingComponentException($"{name} requires a {nameof(Blink)} component.");
            }

            return blink;
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (sunProducer == null) sunProducer = GetComponent<SunProducer>();
            if (blink == null) blink = GetComponent<Blink>();
        }
#endif
    }
}
