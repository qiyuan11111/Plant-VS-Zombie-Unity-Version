using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PvZ.Bootstrap;
using PvZ.Config;
using PvZ.Gameplay.Zombies;
using PvZ.Presentation;
using UnityEditor;
using UnityEngine;

namespace Tests.EditMode
{
    public sealed class ZombieSpawnerTests
    {
        private readonly List<GameObject> _objects = new();
        private GameConfigObject _config;
        private GameObject _originalZombiePrefab;

        [SetUp]
        public void SetUp()
        {
            var cameraObject = Track(new GameObject("Main Camera", typeof(Camera)));
            cameraObject.tag = "MainCamera";
            var managerObject = Track(new GameObject("MainGameManager"));
            var manager = managerObject.AddComponent<MainGameManager>();
            InvokeSingletonAwake(manager);

            _config = Resources.Load<GameConfigObject>("GameConfigObject");
            _originalZombiePrefab = _config.ZombieNormal;
        }

        [TearDown]
        public void TearDown()
        {
            if (_config != null)
            {
                _config.ZombieNormal = _originalZombiePrefab;
                _config.Init();
            }

            for (var index = _objects.Count - 1; index >= 0; index--)
            {
                if (_objects[index] != null)
                {
                    Object.DestroyImmediate(_objects[index]);
                }
            }

            _objects.Clear();
        }

        [Test]
        public void NormalZombiePrefab_UsesCenteredRootContract()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefab/Zombie/ZombieNormal/ZombieNormal.prefab");
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.transform.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(prefab.transform.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one));

            var collider = prefab.GetComponent<BoxCollider2D>();
            Assert.That(collider, Is.Not.Null);
            Assert.That(collider.offset, Is.EqualTo(new Vector2(-8.125f, 2.475f)));
            Assert.That(collider.size, Is.EqualTo(new Vector2(45f, 105f)));

            foreach (var spriteTransform in prefab.GetComponentsInChildren<SpriteTransform>(true))
            {
                Assert.That(spriteTransform.providesChildSpritePosition, Is.False,
                    spriteTransform.name);
                Assert.That(spriteTransform.providesChildSpriteAffine, Is.False,
                    spriteTransform.name);
            }
        }

        [Test]
        public void SpawnZombieInRow_UsesConfiguredTypeParentAndSpawnPoint()
        {
            var prefab = CreateZombiePrefab();
            _config.ZombieNormal = prefab;
            _config.Init();
            var spawner = CreateSpawner(maxAliveZombies: 3, out var zombieContainer, out var points);

            ZombieEntity raisedZombie = null;
            spawner.ZombieSpawned += zombie => raisedZombie = zombie;

            var spawned = spawner.SpawnZombieInRow(
                GameConfigObject.ZombieType.ZombieNormal,
                2);

            Assert.That(spawned, Is.SameAs(raisedZombie));
            Assert.That(spawned.RowIndex, Is.EqualTo(2));
            Assert.That(spawned.IsOnBoard, Is.True);
            Assert.That(spawned.transform.parent, Is.SameAs(zombieContainer));
            Assert.That(spawned.transform.localPosition,
                Is.EqualTo(new Vector3(points[2].position.x, points[2].position.y, 10f)));
            Assert.That(spawned.name, Is.EqualTo("TestZombie-2"));
            Assert.That(spawner.AliveZombieCount, Is.EqualTo(1));
        }

        [Test]
        public void SpawnZombieInRow_RejectsInvalidRowAndAliveLimit()
        {
            var prefab = CreateZombiePrefab();
            _config.ZombieNormal = prefab;
            _config.Init();
            var spawner = CreateSpawner(maxAliveZombies: 1, out _, out _);

            Assert.That(
                () => spawner.SpawnZombieInRow(-1),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());

            spawner.SpawnZombieInRow(0);

            Assert.That(
                () => spawner.SpawnZombieInRow(1),
                Throws.TypeOf<System.InvalidOperationException>());
        }

        [Test]
        public void SpawnZombieInRow_RejectsPrefabWithoutZombieEntity()
        {
            var invalidPrefab = Track(new GameObject("InvalidZombiePrefab"));
            _config.ZombieNormal = invalidPrefab;
            _config.Init();
            var spawner = CreateSpawner(maxAliveZombies: 1, out _, out _);

            Assert.That(
                () => spawner.SpawnZombieInRow(0),
                Throws.TypeOf<MissingComponentException>());
        }

        private GameObject CreateZombiePrefab()
        {
            var prefab = Track(new GameObject("TestZombiePrefab"));
            prefab.AddComponent<SpriteGroup>();
            prefab.AddComponent<TestZombie>();
            return prefab;
        }

        private ZombieSpawner CreateSpawner(
            int maxAliveZombies,
            out Transform zombieContainer,
            out List<Transform> points)
        {
            zombieContainer = Track(new GameObject("Zombie")).transform;
            var spawnerObject = Track(new GameObject("ZombieSpawner"));
            spawnerObject.SetActive(false);

            points = new List<Transform>();
            for (var row = 0; row < 5; row++)
            {
                var point = Track(new GameObject("ZombieSpawner-" + row)).transform;
                point.SetParent(spawnerObject.transform, false);
                point.position = new Vector3(374f, 183f - row * 99f, 0f);
                points.Add(point);
            }

            var spawner = spawnerObject.AddComponent<ZombieSpawner>();
            SetPrivateField(spawner, "zombieContainer", zombieContainer);
            SetPrivateField(spawner, "spawnPoints", points);
            SetPrivateField(spawner, "maxAliveZombies", maxAliveZombies);
            spawnerObject.SetActive(true);
            return spawner;
        }

        private GameObject Track(GameObject gameObject)
        {
            _objects.Add(gameObject);
            return gameObject;
        }

        private static void InvokeSingletonAwake(MainGameManager manager)
        {
            var method = manager.GetType().BaseType?.GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(manager, null);
        }

        private static void SetPrivateField<T>(ZombieSpawner spawner, string name, T value)
        {
            var field = typeof(ZombieSpawner).GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(spawner, value);
        }
    }

    internal sealed class TestZombie : ZombieEntity
    {
        public override string GetChineseName()
        {
            return "测试僵尸";
        }

        public override string GetEnglishName()
        {
            return "TestZombie";
        }
    }
}
