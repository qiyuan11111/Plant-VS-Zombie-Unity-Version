using System;
using System.Collections.Generic;
using PvZ.Bootstrap;
using PvZ.Config;
using PvZ.Core;
using PvZ.Gameplay.Zombies;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PvZ.Gameplay.Zombies.Spawning
{
    /// <summary>
    /// Scene-level entry point for creating zombies. Wave scheduling deliberately
    /// lives outside this class so later wave code can request an exact type/row.
    /// </summary>
    public sealed class ZombieSpawner : SceneSingleton<ZombieSpawner>
    {
        [Header("Spawn Layout")]
        [SerializeField] private Transform zombieContainer;
        [SerializeField] private List<Transform> spawnPoints = new();
        [SerializeField] private float spawnDepth = 10f;

        [SerializeField] private ZombieType defaultZombieType = ZombieType.ZombieNormal;
        [SerializeField, Min(1)] private int maxAliveZombies = 30;

        private readonly List<ZombieEntity> _aliveZombies = new();

        public int SpawnPointCount => spawnPoints.Count;
        public int AliveZombieCount
        {
            get
            {
                RemoveDestroyedZombies();
                return _aliveZombies.Count;
            }
        }
        public bool HasCapacity => AliveZombieCount < maxAliveZombies;

        public event Action<ZombieEntity> ZombieSpawned;

        public ZombieEntity SpawnZombie()
        {
            return SpawnZombie(defaultZombieType);
        }

        public ZombieEntity SpawnZombie(ZombieType zombieType)
        {
            EnsureCanSpawn();
            return SpawnZombieInRow(zombieType, Random.Range(0, spawnPoints.Count));
        }

        public ZombieEntity SpawnZombieInRow(int row)
        {
            return SpawnZombieInRow(defaultZombieType, row);
        }

        public ZombieEntity SpawnZombieInRow(
            ZombieType zombieType,
            int row)
        {
            EnsureCanSpawn();

            if (row < 0 || row >= spawnPoints.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(row),
                    row,
                    $"Row must be between 0 and {spawnPoints.Count - 1}.");
            }

            var prefab = GameBootstrap.Instance.Catalog.GetZombieByType(zombieType);
            if (prefab.GetComponent<ZombieEntity>() == null)
            {
                throw new MissingComponentException(
                    $"{prefab.name} requires a {nameof(ZombieEntity)} component.");
            }

            var zombieObject = Instantiate(prefab, zombieContainer, false);
            var zombie = zombieObject.GetComponent<ZombieEntity>();
            var spawnPoint = spawnPoints[row];
            var localPosition = zombieContainer.InverseTransformPoint(spawnPoint.position);
            localPosition.z = spawnDepth;

            try
            {
                zombie.ResetRuntimeState();
                zombie.EnterBoard(row, localPosition);
            }
            catch
            {
                Destroy(zombieObject);
                throw;
            }

            _aliveZombies.Add(zombie);
            zombie.Died += _ => _aliveZombies.Remove(zombie);
            ZombieSpawned?.Invoke(zombie);
            return zombie;
        }

        protected override bool ValidateReferences()
        {
            var isValid = true;
            isValid &= RequireReference(zombieContainer, nameof(zombieContainer));

            if (spawnPoints.Count == 0)
            {
                Debug.LogError($"{nameof(ZombieSpawner)} requires at least one spawn point.", this);
                isValid = false;
            }

            for (var row = 0; row < spawnPoints.Count; row++)
            {
                isValid &= RequireReference(spawnPoints[row], $"spawn point for row {row}");
            }

            return isValid;
        }

        protected override bool ValidateDependencies()
        {
            return RequireManager(GameBootstrap.Instance);
        }

        protected override void OnSingletonDestroy()
        {
            _aliveZombies.Clear();
        }

        private void EnsureCanSpawn()
        {
            if (GameBootstrap.Instance == null)
            {
                throw new MissingReferenceException(
                    $"{nameof(ZombieSpawner)} requires an active {nameof(GameBootstrap)} in the scene.");
            }

            if (zombieContainer == null || spawnPoints.Count == 0)
            {
                throw new InvalidOperationException($"{nameof(ZombieSpawner)} has an invalid spawn layout.");
            }

            RemoveDestroyedZombies();
            if (_aliveZombies.Count >= maxAliveZombies)
            {
                throw new InvalidOperationException(
                    $"{nameof(ZombieSpawner)} reached its alive-zombie limit ({maxAliveZombies}).");
            }
        }

        private void RemoveDestroyedZombies()
        {
            _aliveZombies.RemoveAll(zombie => zombie == null);
        }
    }
}
