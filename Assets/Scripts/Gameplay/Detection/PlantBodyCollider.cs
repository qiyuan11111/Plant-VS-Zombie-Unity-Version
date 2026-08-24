using System;
using PvZ.Core.Entities;
using PvZ.Gameplay.Plants;
using UnityEngine;

namespace PvZ.Gameplay.Detection
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlantBodyCollider : ColliderBinding
    {
        public PlantEntity Plant => Owner as PlantEntity;

        public event Action<GameEntity> DetectorEntered;
        public event Action<GameEntity> DetectorStayed;
        public event Action<GameEntity> DetectorExited;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var detector = ResolveDetector(other);
            if (detector != null) OnEnter(detector.Owner);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            var detector = ResolveDetector(other);
            if (detector != null) OnStay(detector.Owner);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var detector = ResolveDetector(other);
            if (detector != null) OnExit(detector.Owner);
        }

        public void OnEnter(GameEntity detector)
        {
            if (detector != null) DetectorEntered?.Invoke(detector);
        }

        public void OnStay(GameEntity detector)
        {
            if (detector != null) DetectorStayed?.Invoke(detector);
        }

        public void OnExit(GameEntity detector)
        {
            if (detector != null) DetectorExited?.Invoke(detector);
        }

        private static PlantDetector ResolveDetector(Collider2D other)
        {
            if (other == null || other.gameObject.layer != LayerMask.NameToLayer("DetectPlantRegion"))
            {
                return null;
            }

            return other.GetComponent<PlantDetector>();
        }
    }
}
