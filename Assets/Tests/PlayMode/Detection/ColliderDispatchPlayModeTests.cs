using System.Collections;
using NUnit.Framework;
using PvZ.Gameplay.Entities;
using PvZ.Gameplay.Detection;
using PvZ.Gameplay.Detection.Plants;
using PvZ.Gameplay.Detection.Zombies;
using PvZ.Gameplay.Plants;
using PvZ.Gameplay.Zombies;
using UnityEngine;
using UnityEngine.TestTools;

namespace PvZ.Tests.PlayMode.Detection
{
    public sealed class ColliderDispatchPlayModeTests
    {
        [Test]
        public void PlantEntity_BindsBodyEventsWithoutCallbackClass()
        {
            var plantObject = new GameObject("DetectedPlant");
            var detectorObject = new GameObject("DetectorOwner");

            try
            {
                var plantCollider = plantObject.AddComponent<BoxCollider2D>();
                var plant = plantObject.AddComponent<TestPlant>();
                var plantBody = plantObject.AddComponent<PlantBodyCollider>();
                plantBody.Configure(plant, plantCollider);
                plant.ConfigureBodyCollider(plantBody);
                plant.BindBodyEventsForTest();

                var detectorOwner = detectorObject.AddComponent<TestDetectorOwner>();
                plantBody.OnEnter(detectorOwner);
                plantBody.OnStay(detectorOwner);
                plantBody.OnExit(detectorOwner);

                Assert.That(plant.EnteredDetector, Is.SameAs(detectorOwner));
                Assert.That(plant.StayedDetector, Is.SameAs(detectorOwner));
                Assert.That(plant.ExitedDetector, Is.SameAs(detectorOwner));
            }
            finally
            {
                Object.DestroyImmediate(plantObject);
                Object.DestroyImmediate(detectorObject);
            }
        }

        [UnityTest]
        public IEnumerator IndependentDetectorNodes_DispatchIndependentEventsInBothDirections()
        {
            var shooterObject = new GameObject("DetectorOwner")
            {
                layer = LayerMask.NameToLayer("Plant")
            };
            var zombieObject = new GameObject("DetectedZombie")
            {
                layer = LayerMask.NameToLayer("Zombie")
            };

            try
            {
                var detectorOwner = shooterObject.AddComponent<TestDetectorOwner>();
                var rigidbody = shooterObject.AddComponent<Rigidbody2D>();
                rigidbody.bodyType = RigidbodyType2D.Kinematic;
                rigidbody.gravityScale = 0f;

                var zombieBodyCollider = zombieObject.AddComponent<BoxCollider2D>();
                zombieBodyCollider.isTrigger = true;
                zombieBodyCollider.size = new Vector2(10f, 10f);
                var zombie = zombieObject.AddComponent<TestZombie>();
                var detectedZombie = zombieObject.AddComponent<ZombieBodyCollider>();
                detectedZombie.Configure(zombie, zombieBodyCollider);

                var callbackA = new TestDetectorCallback(detectorOwner);
                var callbackB = new TestDetectorCallback(detectorOwner);
                var detectorA = CreateDetector(detectorOwner, "A", callbackA);
                var detectorB = CreateDetector(detectorOwner, "B", callbackB);
                var detectorEnterCount = 0;
                var detectorExitCount = 0;
                var detectedEnterCount = 0;
                var detectedExitCount = 0;

                detectorA.TargetEntered += target =>
                {
                    Assert.That(target, Is.SameAs(zombie));
                    detectorEnterCount++;
                };
                detectorB.TargetEntered += target =>
                {
                    Assert.That(target, Is.SameAs(zombie));
                    detectorEnterCount++;
                };
                detectorA.TargetExited += target => detectorExitCount++;
                detectorB.TargetExited += target => detectorExitCount++;
                detectedZombie.DetectorEntered += detector =>
                {
                    Assert.That(detector, Is.SameAs(detectorOwner));
                    detectedEnterCount++;
                };
                detectedZombie.DetectorExited += detector =>
                {
                    Assert.That(detector, Is.SameAs(detectorOwner));
                    detectedExitCount++;
                };

                Physics2D.SyncTransforms();
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();

                Assert.That(detectorEnterCount, Is.EqualTo(2));
                Assert.That(detectedEnterCount, Is.EqualTo(2));
                Assert.That(callbackA.LoadCount, Is.EqualTo(1));
                Assert.That(callbackB.LoadCount, Is.EqualTo(1));
                Assert.That(callbackA.EnterCount, Is.EqualTo(1));
                Assert.That(callbackB.EnterCount, Is.EqualTo(1));
                Assert.That(detectorA.DetectedContactCount, Is.EqualTo(1));
                Assert.That(detectorB.DetectedContactCount, Is.EqualTo(1));

                detectorA.transform.localPosition = new Vector3(100f, 0f, 0f);
                Physics2D.SyncTransforms();
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();

                Assert.That(detectorExitCount, Is.EqualTo(1));
                Assert.That(detectedExitCount, Is.EqualTo(1));
                Assert.That(callbackA.ExitCount, Is.EqualTo(1));
                Assert.That(callbackB.ExitCount, Is.Zero);
                Assert.That(detectorA.DetectedContactCount, Is.Zero);
                Assert.That(detectorB.DetectedContactCount, Is.EqualTo(1));
            }
            finally
            {
                Object.Destroy(shooterObject);
                Object.Destroy(zombieObject);
            }
        }

        private static ZombieDetector CreateDetector(
            TestDetectorOwner owner,
            string detectorName,
            IZombieDetectorCallback callback)
        {
            var detectorObject = new GameObject(detectorName)
            {
                layer = LayerMask.NameToLayer("DetectZombieRegion")
            };
            detectorObject.transform.SetParent(owner.transform, false);
            var collider = detectorObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(10f, 10f);
            var detector = detectorObject.AddComponent<ZombieDetector>();
            detector.Configure(owner, collider);
            detector.Load(callback);
            collider.size = new Vector2(10f, 10f);
            collider.offset = Vector2.zero;
            return detector;
        }

        private sealed class TestDetectorCallback : IZombieDetectorCallback
        {
            private readonly GameEntity owner;

            public int LoadCount { get; private set; }
            public int EnterCount { get; private set; }
            public int ExitCount { get; private set; }

            public TestDetectorCallback(GameEntity owner)
            {
                this.owner = owner;
            }

            public void OnLoad(ZombieDetector detector)
            {
                Assert.That(detector.Owner, Is.SameAs(owner));
                LoadCount++;
            }

            public void OnZombieEnter(ZombieDetector detector, ZombieEntity zombie)
            {
                Assert.That(detector.Owner, Is.SameAs(owner));
                Assert.That(zombie, Is.Not.Null);
                EnterCount++;
            }

            public void OnZombieStay(ZombieDetector detector, ZombieEntity zombie)
            {
                Assert.That(detector.Owner, Is.SameAs(owner));
                Assert.That(zombie, Is.Not.Null);
            }

            public void OnZombieExit(ZombieDetector detector, ZombieEntity zombie)
            {
                Assert.That(detector.Owner, Is.SameAs(owner));
                Assert.That(zombie, Is.Not.Null);
                ExitCount++;
            }
        }

        private sealed class TestDetectorOwner : GameEntity
        {
            public override string GetChineseName() => "测试检测方";
            public override string GetEnglishName() => "TestDetectorOwner";
        }

        private sealed class TestZombie : ZombieEntity
        {
            public override string GetChineseName() => "测试僵尸";
            public override string GetEnglishName() => "TestZombie";
        }

        private sealed class TestPlant : PlantEntity
        {
            public GameEntity EnteredDetector { get; private set; }
            public GameEntity StayedDetector { get; private set; }
            public GameEntity ExitedDetector { get; private set; }

            public void BindBodyEventsForTest()
            {
                base.OnEnteredBoard();
            }

            protected override void OnDetectorEntered(GameEntity detector)
            {
                EnteredDetector = detector;
            }

            protected override void OnDetectorStayed(GameEntity detector)
            {
                StayedDetector = detector;
            }

            protected override void OnDetectorExited(GameEntity detector)
            {
                ExitedDetector = detector;
            }

            public override string GetChineseName() => "测试植物";
            public override string GetEnglishName() => "TestPlant";
        }
    }
}
