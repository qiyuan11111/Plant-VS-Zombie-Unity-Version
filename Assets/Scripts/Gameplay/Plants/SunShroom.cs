using UnityEngine;

namespace Prefab.Plant.SunShroom.Script
{
    [RequireComponent(typeof(SunProducer))]
    public sealed class SunShroom : global::Script.Model.Plant
    {
        [SerializeField] private SunProducer sunProducer;

        public override Vector3 SpritePosition => new(42.275f, -45.875f, 0f);

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
        }

        protected override void OnDestroy()
        {
            if (sunProducer != null)
            {
                sunProducer.StopProducing();
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

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (sunProducer == null)
            {
                sunProducer = GetComponent<SunProducer>();
            }
        }
#endif
    }
}
