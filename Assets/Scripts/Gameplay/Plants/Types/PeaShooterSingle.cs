using System;
using System.Collections;
using System.Collections.Generic;
using PvZ.Presentation;
using PvZ.Gameplay.Detection;
using PvZ.Gameplay.Plants;
using PvZ.Gameplay.Plants.Abilities;
using PvZ.Gameplay.Zombies;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PvZ.Gameplay.Plants.Types
{
    /// <summary>
    /// 单发豌豆射手。
    /// 负责管理自身表现、子弹生成点，以及“前方是否存在僵尸”的检测能力；
    /// 实际生成子弹和攻击结算由后续的射击能力负责。
    /// </summary>
    [RequireComponent(typeof(Blink))]
    public class PeaShooterSingle : PlantEntity, IPointerClickHandler
    {
        // 射击检测框从植物前方开始，并一直延伸到棋盘右边界。
        // 这些值使用预制体本地坐标，与当前美术资源和棋盘尺寸对应。
        private const float DetectorBoardRightX = 304.3585f;
        private const float DetectorForwardStart = 40f;
        private const float DetectorVerticalOffset = -10f;
        private const float DetectorHeight = 34.5f;

        // 按当前、历史和旧版预制体层级依次兼容豌豆生成点。
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

        /// <summary>
        /// 在嘴部锚点位置发射一颗豌豆。
        /// 当前仅保留生成点解析，子弹实例化会在子弹系统接入后启用。
        /// </summary>
        public void ShootProjectilePea()
        {
            Vector3 position = ResolveProjectileSpawnAnchor().position;
            // BulletManager.instance.InstantiateBullet(this, position + new Vector3(30, 6, 0),
            //     GameConfigObject.BulletType.ProjectilePea);
        }

        /// <summary>
        /// 获取子弹生成点。优先使用 Inspector 引用，未配置时兼容查找不同版本的嘴部节点。
        /// </summary>
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

        /// <summary>
        /// 植物正式进入棋盘后加载检测器，并启动眨眼表现。
        /// </summary>
        protected override void OnEnteredBoard()
        {
            base.OnEnteredBoard();
            ResolveBlink().StartBlinking();
        }

        /// <summary>
        /// 销毁前停止眨眼协程，并交由植物基类清理碰撞事件和格子占用。
        /// </summary>
        protected override void OnDestroy()
        {
            if (blink != null)
            {
                blink.StopBlinking();
            }

            base.OnDestroy();
        }

        /// <summary>
        /// 获取眨眼组件。允许 Inspector 配置，也支持从当前节点自动补取。
        /// </summary>
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

        /// <summary>
        /// 将指定的检测节点绑定到该植物的射击检测回调。
        /// 通常由预制体构建器调用；运行时由基类统一加载该映射。
        /// </summary>
        /// <param name="detectorTransform">
        /// 位于当前实体 <c>detect</c> 节点下、且挂有 <see cref="ZombieDetector"/> 的节点。
        /// </param>
        public void ConfigureShootingDetectorBinding(Transform detectorTransform)
        {
            ConfigureDetector(detectorTransform, new ShootingDetection());
        }

        /// <summary>
        /// 根据植物在棋盘中的横坐标，配置向右覆盖至棋盘边界的矩形检测区域。
        /// </summary>
        private void ConfigureShootingDetectorGeometry(ZombieDetector detector)
        {
            if (detector.Owner != this)
            {
                throw new System.ArgumentException(
                    $"{detector.name} does not belong to {name}.",
                    nameof(detector));
            }

            if (detector.Collider is not BoxCollider2D box)
            {
                throw new MissingComponentException(
                    $"{detector.name} requires a {nameof(BoxCollider2D)}.");
            }

            var left = transform.localPosition.x + DetectorForwardStart;
            var width = Mathf.Max(0.01f, DetectorBoardRightX - left);
            box.isTrigger = true;
            box.size = new Vector2(width, DetectorHeight);
            box.offset = new Vector2(
                DetectorForwardStart + width * 0.5f,
                DetectorVerticalOffset);
        }

        /// <summary>
        /// 单发豌豆射手专属的僵尸检测回调。
        /// 每个 Detector Slot 持有独立实例，使同一实体可以配置多种检测能力。
        /// </summary>
        [Serializable]
        private sealed class ShootingDetection : IZombieDetectorCallback
        {
            // SerializeReference 只保存回调配置；运行时所属植物由 Detector 反向解析。
            [NonSerialized] private PeaShooterSingle owner;

            /// <summary>
            /// Detector 被植物基类加载时校验所属实体，并初始化检测框尺寸。
            /// </summary>
            public void OnLoad(ZombieDetector detector)
            {
                owner = detector.Owner as PeaShooterSingle;
                if (owner == null)
                {
                    throw new ArgumentException(
                        $"{detector.name} must belong to a {nameof(PeaShooterSingle)}.",
                        nameof(detector));
                }

                owner.ConfigureShootingDetectorGeometry(detector);
            }

            /// <summary>
            /// 僵尸进入射击范围。后续在此启动或唤醒射击能力。
            /// </summary>
            public void OnZombieEnter(ZombieDetector detector, ZombieEntity zombie)
            {
                Debug.Log("ZombieEnter");
            }

            /// <summary>
            /// 僵尸持续位于射击范围。预留给需要持续更新目标的能力。
            /// </summary>
            public void OnZombieStay(ZombieDetector detector, ZombieEntity zombie)
            {
                Debug.Log("ZombieStay");
            }

            /// <summary>
            /// 僵尸离开射击范围。后续可根据 Detector 的剩余接触数决定是否停止射击。
            /// </summary>
            public void OnZombieExit(ZombieDetector detector, ZombieEntity zombie)
            {
                Debug.Log("ZombieExit");
            }
        }


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


        /// <summary>
        /// 响应植物上的指针点击；当前尚未绑定交互行为。
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
        }

#if UNITY_EDITOR
        /// <summary>
        /// 在编辑器中自动补全可直接获取的组件引用。
        /// </summary>
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
