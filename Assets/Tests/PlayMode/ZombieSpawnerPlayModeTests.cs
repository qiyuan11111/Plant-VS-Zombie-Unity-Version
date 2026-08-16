using System.Collections;
using NUnit.Framework;
using PvZ.Gameplay.Zombies;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public sealed class ZombieSpawnerPlayModeTests
    {
        [UnityTest]
        public IEnumerator SampleScene_AutomaticallySpawnsWalkingZombieFromRight()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            yield return null;

            var timeout = Time.realtimeSinceStartup + 3f;
            var zombie = Object.FindObjectOfType<ZombieNormal>();
            while (zombie == null && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
                zombie = Object.FindObjectOfType<ZombieNormal>();
            }

            Assert.That(ZombieSpawner.Instance, Is.Not.Null, "SampleScene should contain an active ZombieSpawner.");
            Assert.That(zombie, Is.Not.Null, "The automatic spawner should create a normal zombie.");
            Assert.That(zombie.RowIndex, Is.InRange(0, 4));
            Assert.That(zombie.transform.parent.name, Is.EqualTo("Zombie"));

            var startX = zombie.transform.localPosition.x;
            Assert.That(startX, Is.GreaterThan(300f), "Zombie should start near the right edge of the board.");

            var animator = zombie.GetComponent<Animator>();
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("walk"), Is.True);

            yield return new WaitForSeconds(0.75f);

            Assert.That(zombie.transform.localPosition.x, Is.LessThan(startX - 5f),
                "The walk animation root motion should move the spawned zombie left.");
        }
    }
}