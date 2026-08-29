using System.Collections;
using PvZ.Config;
using UnityEngine;

namespace PvZ.Gameplay.Zombies.Spawning
{
    /// <summary>Optional simple spawn schedule; wave logic can replace this component.</summary>
    public sealed class ZombieAutoSpawnScheduler : MonoBehaviour
    {
        [SerializeField] private bool spawnAutomatically;
        [SerializeField] private ZombieType zombieType = ZombieType.ZombieNormal;
        [SerializeField, Min(0.1f)] private float spawnInterval = 5f;

        private Coroutine _routine;

        private void Start()
        {
            if (spawnAutomatically)
            {
                _routine = StartCoroutine(SpawnLoop());
            }
        }

        private void OnDisable()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
        }

        private IEnumerator SpawnLoop()
        {
            var wait = new WaitForSeconds(Mathf.Max(0.1f, spawnInterval));
            while (spawnAutomatically)
            {
                var spawner = ZombieSpawner.Instance;
                if (spawner != null && spawner.HasCapacity)
                {
                    spawner.SpawnZombie(zombieType);
                }

                yield return wait;
            }
        }
    }
}
