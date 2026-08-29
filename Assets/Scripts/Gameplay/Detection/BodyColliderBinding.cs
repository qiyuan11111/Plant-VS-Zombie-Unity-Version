using System;
using PvZ.Gameplay.Entities;
using UnityEngine;

namespace PvZ.Gameplay.Detection
{
    public abstract class BodyColliderBinding : ColliderBinding
    {
        public event Action<GameEntity> DetectorEntered;
        public event Action<GameEntity> DetectorStayed;
        public event Action<GameEntity> DetectorExited;

        private void OnTriggerEnter2D(Collider2D other) => OnEnter(ResolveDetectorOwner(other));
        private void OnTriggerStay2D(Collider2D other) => OnStay(ResolveDetectorOwner(other));
        private void OnTriggerExit2D(Collider2D other) => OnExit(ResolveDetectorOwner(other));

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

        protected abstract GameEntity ResolveDetectorOwner(Collider2D other);
    }
}
