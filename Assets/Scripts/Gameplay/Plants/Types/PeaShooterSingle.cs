using System.Collections;
using System.Collections.Generic;
using PvZ.Presentation;
using PvZ.Gameplay.Plants;
using PvZ.Gameplay.Plants.Abilities;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PvZ.Gameplay.Plants.Types
{

    [RequireComponent(typeof(Blink))]
    public class PeaShooterSingle : PlantEntity, IPointerClickHandler
    {
        private const string NativeMouthPath =
            "component/basic/__AffineContent/head/__AffineContent/mouth";
        private const string PreviousAllNativeMouthPath =
            "component/basic/__AffineContent/head/__AffineContent/pod/__AffineContent/mouth";
        private const string PreviousNativeMouthPath =
            "component/basic/head/__AffineContent/pod/mouth";
        private const string LegacyMouthPath = "component/basic/head/pod/mouth";
        private static readonly int Shoot = Animator.StringToHash("shoot");

        [SerializeField] private Blink blink;
        [SerializeField] private Transform projectileSpawnAnchor;
        // public override int sunlight => 100;
        // public override float cdTime => 0.5f;

        // public Vector3 shadowPosition => new(0, -17f, 0);
        // public Vector3 shadowScale => new(1, 1, 1);

        // public class DetectZombieCallback : DetectRegion.DetectCallback
        // {
        //     public override void OnEnterDetect(Collider2D col)
        //     {
        //         var peaShooterSingle = (PeaShooterSingle)sprite;
        //         peaShooterSingle.attack = true;
        //     }
        //
        //     public override void OnExitDetect(Collider2D col)
        //     {
        //         var peaShooterSingle = (PeaShooterSingle)sprite;
        //         if(sprite.detectRegions[name].IsAnyDetect())    return;
        //         peaShooterSingle.attack = false;
        //     }
        //
        //     public override bool IsGoal(GameEntity sprite)
        //     {
        //         if (sprite.row != this.sprite.row) return false;
        //         return true;
        //     }
        //
        //     public DetectZombieCallback(GameEntity sprite) : base(sprite)
        //     {
        //     }
        // }

        // public class HandDetectZombieRegion : DetectRegion.HandDetectRegion
        // {
        //     public override void hand(DetectRegion region)
        //     {
        //         GameObject detectRegionGameObject = region.gameObject;
        //
        //         var right = 304.3585f;
        //         var left = sprite._transform.localPosition.x + 40;
        //
        //         BoxCollider2D collider2D = detectRegionGameObject.AddComponent<BoxCollider2D>();
        //         collider2D.size = new Vector2(right - left, 34.5f);
        //         collider2D.offset = new Vector2((right - left) / 2 + 40, 27.6f);
        //         collider2D.isTrigger = true;
        //     }
        //
        //     public HandDetectZombieRegion(GameEntity sprite, string name, string layer) : base(sprite, name, layer)
        //     {
        //     }
        // }

        public void ShootProjectilePea()
        {
            Vector3 position = ResolveProjectileSpawnAnchor().position;
            // BulletManager.instance.InstantiateBullet(this, position + new Vector3(30, 6, 0),
            //     GameConfigObject.BulletType.ProjectilePea);
        }

        private Transform ResolveProjectileSpawnAnchor()
        {
            if (projectileSpawnAnchor == null)
            {
                projectileSpawnAnchor = Transform.Find(NativeMouthPath) ??
                                          Transform.Find(PreviousAllNativeMouthPath) ??
                                          Transform.Find(PreviousNativeMouthPath) ??
                                          Transform.Find(LegacyMouthPath);
            }

            if (projectileSpawnAnchor == null)
            {
                throw new MissingReferenceException(
                    $"{name} has no projectile spawn anchor assigned.");
            }

            return projectileSpawnAnchor;
        }

        protected override void OnEnteredBoard()
        {
            base.OnEnteredBoard();
            ResolveBlink().StartBlinking();
        }

        protected override void OnDestroy()
        {
            if (blink != null)
            {
                blink.StopBlinking();
            }

            base.OnDestroy();
        }

        private Blink ResolveBlink()
        {
            if (blink == null)
            {
                blink = GetComponent<Blink>();
            }

            if (blink == null)
            {
                throw new MissingComponentException(
                    $"{name} requires a {nameof(Blink)} component.");
            }

            return blink;
        }

        // public override void SetDetectRegions()
        // {
        //     SetDetectRegion<ZombieEntity>(new DetectZombieCallback(this), new HandDetectZombieRegion(this, "DetectZombieRegion", "DetectZombieRegion"));
        // }
        //
        // public override List<Task> SetFunction()
        // {
        //     List<Task> tasks = new List<Task>();
        //     tasks.Add(coroutine.StartCoroutineTask(StartShootColdDown()));
        //     return tasks;
        // }


        // IEnumerator StartShootColdDown()
        // {
        //     while (true)
        //     {
        //         if (attack)
        //         {
        //             animator.SetTrigger(Shoot);
        //             yield return new TimeWait(Random.Range(1.45f, 1.55f));
        //         }
        //         else
        //         {
        //             yield return new TimeWait();
        //         }
        //
        //     }
        // }

        public override string GetChineseName()
        {
            return "豌豆射手";
        }

        public override string GetEnglishName()
        {
            return "PeaShooterSingle";
        }

        // public override void SetNormalModeAnimatioSpeed()
        // {
        //     SetAnimationSpeed(1f);
        // }


        public void OnPointerClick(PointerEventData eventData)
        {
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (blink == null)
            {
                blink = GetComponent<Blink>();
            }
        }
#endif

    }

}
