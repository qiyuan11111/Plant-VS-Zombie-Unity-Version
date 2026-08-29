using System.Collections;
using PvZ.Bootstrap;
using PvZ.Config;
using PvZ.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PvZ.Gameplay.Sun
{
    /// <summary>Creates sun entities and optionally schedules natural sun drops.</summary>
    public sealed class SunSpawner : SceneSingleton<SunSpawner>
    {
        [SerializeField] private bool enableNaturalSun = true;
        [SerializeField, Min(0.1f)] private float naturalSunInterval = 0.8f;
        [SerializeField, Min(1)] private int maxNaturalSunCount = 30;
        [SerializeField] private Vector2 naturalSunXRange = new(-600f, 600f);
        [SerializeField] private Vector2 naturalSunYRange = new(-500f, 500f);

        private Coroutine _naturalSpawnRoutine;

        public Sun SpawnSun(Vector3 worldPosition, SunType type)
        {
            var sunPrefab = GameBootstrap.Instance.Catalog.GetObjectByType(ObjectType.Sun);
            var sunObject = Instantiate(sunPrefab, transform, true);
            var sun = sunObject.GetComponent<Sun>();
            if (sun == null)
            {
                Destroy(sunObject);
                throw new MissingComponentException(
                    $"{sunPrefab.name} requires a {nameof(Sun)} component.");
            }

            return sun.Initialize(type, transform.InverseTransformPoint(worldPosition));
        }

        protected override bool ValidateDependencies()
        {
            return RequireManager(GameBootstrap.Instance);
        }

        protected override void OnSingletonStart()
        {
            if (enableNaturalSun)
            {
                _naturalSpawnRoutine = StartCoroutine(SpawnNaturalSuns());
            }
        }

        protected override void OnSingletonDestroy()
        {
            if (_naturalSpawnRoutine != null)
            {
                StopCoroutine(_naturalSpawnRoutine);
                _naturalSpawnRoutine = null;
            }
        }

        private IEnumerator SpawnNaturalSuns()
        {
            var wait = new WaitForSeconds(Mathf.Max(0.1f, naturalSunInterval));
            while (enableNaturalSun)
            {
                if (transform.childCount < maxNaturalSunCount)
                {
                    SpawnSun(new Vector3(
                        Random.Range(naturalSunXRange.x, naturalSunXRange.y),
                        Random.Range(naturalSunYRange.x, naturalSunYRange.y),
                        0f), SunType.Normal);
                }

                yield return wait;
            }
        }
    }
}
