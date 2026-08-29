using PvZ.Gameplay.Entities;
using PvZ.Gameplay.Detection;
using PvZ.Gameplay.Zombies;
using UnityEngine;

namespace PvZ.Gameplay.Detection.Zombies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class ZombieBodyCollider : BodyColliderBinding
    {
        public ZombieEntity Zombie => Owner as ZombieEntity;

        protected override GameEntity ResolveDetectorOwner(Collider2D other)
        {
            return other != null &&
                   other.gameObject.layer == LayerMask.NameToLayer("DetectZombieRegion")
                ? other.GetComponent<ZombieDetector>()?.Owner
                : null;
        }
    }
}
