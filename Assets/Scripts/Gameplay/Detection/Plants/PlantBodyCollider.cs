using PvZ.Gameplay.Entities;
using PvZ.Gameplay.Detection;
using PvZ.Gameplay.Plants;
using UnityEngine;

namespace PvZ.Gameplay.Detection.Plants
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlantBodyCollider : BodyColliderBinding
    {
        public PlantEntity Plant => Owner as PlantEntity;

        protected override GameEntity ResolveDetectorOwner(Collider2D other)
        {
            return other != null &&
                   other.gameObject.layer == LayerMask.NameToLayer("DetectPlantRegion")
                ? other.GetComponent<PlantDetector>()?.Owner
                : null;
        }
    }
}
