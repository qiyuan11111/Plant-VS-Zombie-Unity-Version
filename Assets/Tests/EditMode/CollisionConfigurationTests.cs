using System.Linq;
using NUnit.Framework;
using PvZ.Core.Entities;
using PvZ.Gameplay.Detection;
using PvZ.Gameplay.Plants.Types;
using PvZ.Gameplay.Zombies;
using UnityEditor;
using UnityEngine;

namespace Tests.EditMode
{
    public sealed class CollisionConfigurationTests
    {
        private const string PeaShooterPrefabPath =
            "Assets/Prefab/Plant/PeaShooterSingle/PeaShooterSingle.prefab";
        private const string ZombiePrefabPath =
            "Assets/Prefab/Zombie/ZombieNormal/ZombieNormal.prefab";

        [Test]
        public void PeaShooterPrefab_HasBodyAndSiblingDetectHierarchy()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PeaShooterPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var shooter = prefab.GetComponent<PeaShooterSingle>();
            var bodyCollider = prefab.GetComponent<BoxCollider2D>();
            var plantBody = prefab.GetComponent<PlantBodyCollider>();
            var rigidbody = prefab.GetComponent<Rigidbody2D>();
            var component = prefab.transform.Find("component");
            var detect = prefab.transform.Find("detect");
            var zombieNode = prefab.transform.Find("detect/zombie");
            var detectorCollider = zombieNode?.GetComponent<BoxCollider2D>();
            var detector = zombieNode?.GetComponent<ZombieDetector>();

            Assert.That(shooter, Is.Not.Null);
            Assert.That(bodyCollider, Is.Not.Null);
            Assert.That(plantBody, Is.Not.Null);
            Assert.That(plantBody.Collider, Is.SameAs(bodyCollider));
            Assert.That(plantBody.Plant, Is.SameAs(shooter));
            Assert.That(shooter.PlantBody, Is.SameAs(plantBody));
            Assert.That(rigidbody, Is.Not.Null);
            Assert.That(rigidbody.bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
            Assert.That(component, Is.Not.Null);
            Assert.That(detect, Is.Not.Null);
            Assert.That(component.parent, Is.SameAs(prefab.transform));
            Assert.That(detect.parent, Is.SameAs(prefab.transform));
            Assert.That(zombieNode.parent, Is.SameAs(detect));
            Assert.That(detectorCollider, Is.Not.Null);
            Assert.That(detectorCollider.isTrigger, Is.True);
            Assert.That(zombieNode.gameObject.layer,
                Is.EqualTo(LayerMask.NameToLayer("DetectZombieRegion")));
            Assert.That(detector, Is.Not.Null);
            Assert.That(detector.Collider, Is.SameAs(detectorCollider));
            Assert.That(detector.Owner, Is.SameAs(shooter));
            Assert.That(detector.IsLoaded, Is.False);
            Assert.That(shooter.DetectorSlots.Count, Is.EqualTo(1));
            Assert.That(shooter.DetectorSlots[0].Transform, Is.SameAs(zombieNode));
            Assert.That(shooter.DetectorSlots[0].Callback,
                Is.InstanceOf<IZombieDetectorCallback>());
            Assert.That(shooter.DetectorSlots[0].Callback.GetType().Name,
                Is.EqualTo("ShootingDetection"));
            Assert.That(prefab.GetComponents<ColliderBinding>().Length, Is.EqualTo(1));
            Assert.That(zombieNode.GetComponents<ColliderBinding>().Length, Is.EqualTo(1));
        }

        [Test]
        public void PeaShooterDetector_RefreshesForwardGeometryFromBoardPosition()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PeaShooterPrefabPath);
            var instance = Object.Instantiate(prefab);
            try
            {
                instance.transform.localPosition = new Vector3(-400f, 170f, 10f);
                var shooter = instance.GetComponent<PeaShooterSingle>();
                var detector = instance.GetComponentInChildren<ZombieDetector>(true);
                var box = detector.GetComponent<BoxCollider2D>();

                shooter.LoadDetectorCallbacks();

                var expectedWidth = 304.3585f - (-400f + 40f);
                Assert.That(detector.IsLoaded, Is.True);
                Assert.That(box.size.x, Is.EqualTo(expectedWidth).Within(0.0001f));
                Assert.That(box.size.y, Is.EqualTo(34.5f).Within(0.0001f));
                Assert.That(box.offset.x,
                    Is.EqualTo(40f + expectedWidth * 0.5f).Within(0.0001f));
                Assert.That(box.offset.y, Is.EqualTo(-10f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void PeaShooterCallback_IsDiscoverableByInspectorTypeMenu()
        {
            var callbackType = TypeCache.GetTypesDerivedFrom<IDetectorCallback>()
                .SingleOrDefault(type =>
                    type.Name == "ShootingDetection" &&
                    type.DeclaringType == typeof(PeaShooterSingle));

            Assert.That(callbackType, Is.Not.Null);
            Assert.That(callbackType.IsSerializable, Is.True);
            Assert.That(typeof(IZombieDetectorCallback).IsAssignableFrom(callbackType), Is.True);
        }

        [Test]
        public void ZombiePrefab_HasOneToOneDetectedColliderBinding()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZombiePrefabPath);
            var zombie = prefab.GetComponent<ZombieEntity>();
            var bodyCollider = prefab.GetComponent<BoxCollider2D>();
            var zombieBody = prefab.GetComponent<ZombieBodyCollider>();

            Assert.That(zombie, Is.Not.Null);
            Assert.That(bodyCollider, Is.Not.Null);
            Assert.That(zombieBody, Is.Not.Null);
            Assert.That(zombieBody.Collider, Is.SameAs(bodyCollider));
            Assert.That(zombieBody.Zombie, Is.SameAs(zombie));
            Assert.That(zombie.ZombieBody, Is.SameAs(zombieBody));
            Assert.That(prefab.GetComponents<ColliderBinding>().Length, Is.EqualTo(1));
        }

        [Test]
        public void DetectionLayers_EnableOnlyTheirMatchingBodyLayers()
        {
            var plant = LayerMask.NameToLayer("Plant");
            var zombie = LayerMask.NameToLayer("Zombie");
            var detectPlant = LayerMask.NameToLayer("DetectPlantRegion");
            var detectZombie = LayerMask.NameToLayer("DetectZombieRegion");

            Assert.That(Physics2D.GetIgnoreLayerCollision(detectPlant, plant), Is.False);
            Assert.That(Physics2D.GetIgnoreLayerCollision(detectZombie, zombie), Is.False);
            Assert.That(Physics2D.GetIgnoreLayerCollision(detectPlant, zombie), Is.True);
            Assert.That(Physics2D.GetIgnoreLayerCollision(detectZombie, plant), Is.True);
        }
    }
}
