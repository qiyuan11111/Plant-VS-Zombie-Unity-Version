using System;
using PvZ.Core.Detection;
using PvZ.Gameplay.Detection;
using PvZ.Gameplay.Entities;
using PvZ.Gameplay.Zombies;
using UnityEngine;

namespace PvZ.Gameplay.Detection.Zombies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class ZombieDetector : ColliderBinding, IDetector
    {
        private IZombieDetectorCallback callback;

        public event Action<ZombieEntity> TargetEntered;
        public event Action<ZombieEntity> TargetStayed;
        public event Action<ZombieEntity> TargetExited;

        public int DetectedContactCount { get; private set; }
        public bool IsDetectingZombie => DetectedContactCount > 0;
        public bool IsLoaded => callback != null;
        public Type CallbackType => typeof(IZombieDetectorCallback);

        private void OnTriggerEnter2D(Collider2D other)
        {
            var zombie = ResolveTarget(other);
            if (zombie != null) OnEnter(zombie);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            var zombie = ResolveTarget(other);
            if (zombie != null) OnStay(zombie);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var zombie = ResolveTarget(other);
            if (zombie != null) OnExit(zombie);
        }

        public void Load(IZombieDetectorCallback detectorCallback)
        {
            EnsureReady();
            callback = detectorCallback ??
                       throw new System.ArgumentNullException(nameof(detectorCallback));
            callback.OnLoad(this);
        }

        void IDetector.Load(IDetectorCallback detectorCallback)
        {
            if (detectorCallback is not IZombieDetectorCallback zombieCallback)
            {
                throw new ArgumentException(
                    $"{nameof(ZombieDetector)} requires an " +
                    $"{nameof(IZombieDetectorCallback)}.",
                    nameof(detectorCallback));
            }

            Load(zombieCallback);
        }

        public void OnEnter(ZombieEntity zombie)
        {
            if (zombie == null) return;

            var detectorCallback = ResolveCallback();
            DetectedContactCount++;
            TargetEntered?.Invoke(zombie);
            detectorCallback.OnZombieEnter(this, zombie);
        }

        public void OnStay(ZombieEntity zombie)
        {
            if (zombie == null) return;

            var detectorCallback = ResolveCallback();
            TargetStayed?.Invoke(zombie);
            detectorCallback.OnZombieStay(this, zombie);
        }

        public void OnExit(ZombieEntity zombie)
        {
            if (zombie == null) return;

            var detectorCallback = ResolveCallback();
            if (DetectedContactCount > 0) DetectedContactCount--;
            TargetExited?.Invoke(zombie);
            detectorCallback.OnZombieExit(this, zombie);
        }

        private static ZombieEntity ResolveTarget(Collider2D other)
        {
            if (other == null || other.gameObject.layer != LayerMask.NameToLayer("Zombie"))
            {
                return null;
            }

            return other.GetComponent<ZombieBodyCollider>()?.Zombie;
        }

        private IZombieDetectorCallback ResolveCallback()
        {
            if (callback == null)
            {
                throw new MissingComponentException(
                    $"{name} must load an {nameof(IZombieDetectorCallback)} before detection starts.");
            }

            return callback;
        }

        private void OnDisable()
        {
            DetectedContactCount = 0;
        }
    }
}
