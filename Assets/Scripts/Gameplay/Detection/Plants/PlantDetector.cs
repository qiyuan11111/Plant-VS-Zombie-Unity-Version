using System;
using PvZ.Core.Detection;
using PvZ.Gameplay.Detection;
using PvZ.Gameplay.Entities;
using PvZ.Gameplay.Plants;
using UnityEngine;

namespace PvZ.Gameplay.Detection.Plants
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlantDetector : ColliderBinding, IDetector
    {
        private IPlantDetectorCallback callback;

        public event Action<PlantEntity> TargetEntered;
        public event Action<PlantEntity> TargetStayed;
        public event Action<PlantEntity> TargetExited;

        public bool IsLoaded => callback != null;
        public Type CallbackType => typeof(IPlantDetectorCallback);

        private void OnTriggerEnter2D(Collider2D other)
        {
            var plant = ResolveTarget(other);
            if (plant != null) OnEnter(plant);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            var plant = ResolveTarget(other);
            if (plant != null) OnStay(plant);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var plant = ResolveTarget(other);
            if (plant != null) OnExit(plant);
        }

        public void Load(IPlantDetectorCallback detectorCallback)
        {
            EnsureReady();
            callback = detectorCallback ??
                       throw new ArgumentNullException(nameof(detectorCallback));
            callback.OnLoad(this);
        }

        void IDetector.Load(IDetectorCallback detectorCallback)
        {
            if (detectorCallback is not IPlantDetectorCallback plantCallback)
            {
                throw new ArgumentException(
                    $"{nameof(PlantDetector)} requires an " +
                    $"{nameof(IPlantDetectorCallback)}.",
                    nameof(detectorCallback));
            }

            Load(plantCallback);
        }

        public void OnEnter(PlantEntity plant)
        {
            if (plant == null) return;

            var detectorCallback = ResolveCallback();
            TargetEntered?.Invoke(plant);
            detectorCallback.OnPlantEnter(this, plant);
        }

        public void OnStay(PlantEntity plant)
        {
            if (plant == null) return;

            var detectorCallback = ResolveCallback();
            TargetStayed?.Invoke(plant);
            detectorCallback.OnPlantStay(this, plant);
        }

        public void OnExit(PlantEntity plant)
        {
            if (plant == null) return;

            var detectorCallback = ResolveCallback();
            TargetExited?.Invoke(plant);
            detectorCallback.OnPlantExit(this, plant);
        }

        private IPlantDetectorCallback ResolveCallback()
        {
            if (callback == null)
            {
                throw new MissingComponentException(
                    $"{name} must load an {nameof(IPlantDetectorCallback)} " +
                    "before detection starts.");
            }

            return callback;
        }

        private static PlantEntity ResolveTarget(Collider2D other)
        {
            if (other == null || other.gameObject.layer != LayerMask.NameToLayer("Plant"))
            {
                return null;
            }

            return other.GetComponent<PlantBodyCollider>()?.Plant;
        }
    }
}
