using UnityEngine;
using PvZ.Gameplay.Entities;
using PvZ.Gameplay.Board;
using PvZ.Gameplay.Detection;
using PvZ.Gameplay.Detection.Plants;
using PvZ.Gameplay.Presentation.Shadows;

namespace PvZ.Gameplay.Plants
{
    /// <summary>
    /// 所有植物实体的基类，负责植物进入棋盘后的通用生命周期。
    /// <para>
    /// 主要职责包括：记录所在格子、设置行与渲染层、启用根碰撞体、创建阴影，
    /// 以及将根节点上的 <see cref="PlantBodyCollider"/> 事件转发给派生植物。
    /// </para>
    /// <para>
    /// 具体植物的攻击、生产等能力不应放在这里；它们应由派生类通过
    /// <see cref="OnEnteredBoard"/>、<see cref="OnDetectorEntered"/>、
    /// <see cref="OnDetectorStayed"/> 和 <see cref="OnDetectorExited"/> 扩展。
    /// </para>
    /// </summary>
    public abstract class PlantEntity : Character
    {
        // 植物落到棋盘后统一恢复为正常比例，避免预览或拖拽阶段的缩放残留。
        private static readonly Vector3 BoardScale = Vector3.one;

        // 以下阴影配置由各植物预制体在 Inspector 中独立设置。
        [SerializeField] private ShadowSizePreset shadowSize = ShadowSizePreset.Large;
        [SerializeField] private Vector2 shadowLocalPosition = new(0f, -20f);
        [SerializeField] private bool drawsShadow = true;

        // 植物作为“被检测方”使用的根节点碰撞绑定；不负责主动寻找目标。
        [SerializeField] private PlantBodyCollider plantBody;

        // 当前占用的棋盘格，用于实体销毁时释放格子。
        private BoardCell _occupiedCell;
        private Shadow _shadow;
        private bool _isOnBoard;

        // 防止重复订阅 PlantBodyCollider 事件。
        private bool _bodyEventsBound;

        /// <summary>植物是否已经完成入场并处于棋盘中。</summary>
        public bool IsOnBoard => _isOnBoard;

        /// <summary>植物阴影所使用的尺寸预设。</summary>
        public ShadowSizePreset ShadowSize => shadowSize;

        /// <summary>阴影相对于植物根节点的本地坐标。</summary>
        public Vector2 ShadowLocalPosition => shadowLocalPosition;

        /// <summary>该植物是否需要绘制阴影。</summary>
        public bool DrawsShadow => drawsShadow;

        /// <summary>
        /// 植物作为被检测方的碰撞绑定。
        /// 优先使用序列化引用；未配置时兼容性地从植物根节点查找。
        /// </summary>
        public PlantBodyCollider PlantBody =>
            plantBody != null ? plantBody : GetComponent<PlantBodyCollider>();

        /// <summary>
        /// 设置植物作为被检测方使用的碰撞绑定。
        /// </summary>
        /// <param name="bodyCollider">
        /// 必须挂在当前植物根节点上的 <see cref="PlantBodyCollider"/>。
        /// </param>
        /// <returns>当前植物，便于构建器链式配置。</returns>
        public PlantEntity ConfigureBodyCollider(PlantBodyCollider bodyCollider)
        {
            if (bodyCollider == null)
            {
                throw new System.ArgumentNullException(nameof(bodyCollider));
            }

            if (bodyCollider.gameObject != gameObject)
            {
                throw new System.ArgumentException(
                    $"{nameof(PlantBodyCollider)} must be on the plant root.",
                    nameof(bodyCollider));
            }

            plantBody = bodyCollider;
            return this;
        }

        /// <summary>
        /// 将植物放入指定棋盘格，并完成所有通用入场初始化。
        /// 每个植物实例只能成功调用一次。
        /// </summary>
        /// <param name="cell">已经分配给该植物的有效棋盘格。</param>
        /// <returns>当前植物，便于链式调用。</returns>
        public PlantEntity EnterBoard(BoardCell cell)
        {
            if (_isOnBoard)
            {
                throw new System.InvalidOperationException($"{name} is already on the board.");
            }

            if (cell == null || cell == BoardCell.None)
            {
                throw new System.ArgumentException("A plant requires a valid board cell.", nameof(cell));
            }

            _occupiedCell = cell;
            SetRow(cell.Point.y).SetHeight(0f);

            GetComponentRoot()
                .SetSortingLayer("plant-" + cell.Point.y)
                .SetColliderState(true);

            SetLocalScale(BoardScale);
            SetLocalPosition(new Vector3(cell.Position.x, cell.Position.y, 10f));
            SetName(GetEnglishName() + "-" + cell.Point.x + "-" + cell.Point.y);

            EnsureShadow();
            _isOnBoard = true;
            enabled = true;
            OnEnteredBoard();
            return this;
        }

        /// <summary>
        /// 植物完成通用入场设置后调用。
        /// 默认绑定被检测碰撞事件并载入主动检测器的回调配置；
        /// 派生类重写时应调用 <c>base.OnEnteredBoard()</c>。
        /// </summary>
        protected virtual void OnEnteredBoard()
        {
            BindBodyEvents();
            LoadDetectorCallbacks();
        }

        /// <summary>
        /// 某个主动检测实体首次进入本植物的被检测碰撞箱时调用。
        /// 参数只保证是检测方实体，不保证它是僵尸或其他特定类型。
        /// </summary>
        /// <param name="detector">拥有主动检测碰撞箱的实体。</param>
        protected virtual void OnDetectorEntered(GameEntity detector)
        {
        }

        /// <summary>
        /// 某个主动检测实体持续位于本植物的被检测碰撞箱内时调用。
        /// </summary>
        /// <param name="detector">拥有主动检测碰撞箱的实体。</param>
        protected virtual void OnDetectorStayed(GameEntity detector)
        {
        }

        /// <summary>
        /// 某个主动检测实体离开本植物的被检测碰撞箱时调用。
        /// </summary>
        /// <param name="detector">拥有主动检测碰撞箱的实体。</param>
        protected virtual void OnDetectorExited(GameEntity detector)
        {
        }

        // PlantBodyCollider 只发布通用的被检测事件；PlantEntity 决定如何响应。
        private void BindBodyEvents()
        {
            if (_bodyEventsBound) return;

            plantBody = PlantBody;
            if (plantBody == null) return;
            if (plantBody.Plant != this)
            {
                throw new MissingComponentException(
                    $"{name} requires a configured {nameof(PlantBodyCollider)} on its root.");
            }

            plantBody.DetectorEntered += OnDetectorEntered;
            plantBody.DetectorStayed += OnDetectorStayed;
            plantBody.DetectorExited += OnDetectorExited;
            _bodyEventsBound = true;
        }

        // 在销毁前解除订阅，避免碰撞组件继续持有实体的回调引用。
        private void UnbindBodyEvents()
        {
            if (!_bodyEventsBound || plantBody == null) return;

            plantBody.DetectorEntered -= OnDetectorEntered;
            plantBody.DetectorStayed -= OnDetectorStayed;
            plantBody.DetectorExited -= OnDetectorExited;
            _bodyEventsBound = false;
        }

        // 阴影在植物实际进入棋盘时延迟创建，预览阶段不生成。
        private void EnsureShadow()
        {
            if (_shadow != null || !drawsShadow) return;
            var drawNightShadow = BoardGrid.Instance != null && BoardGrid.Instance.IsNight;
            _shadow = ShadowFactory.Create(
                Transform,
                shadowLocalPosition,
                Shadow.GetScale(shadowSize),
                drawNightShadow);
        }

        /// <summary>
        /// 销毁植物时解除碰撞事件并释放其占用的棋盘格。
        /// 派生类重写时应调用 <c>base.OnDestroy()</c>。
        /// </summary>
        protected virtual void OnDestroy()
        {
            UnbindBodyEvents();
            _isOnBoard = false;
            _occupiedCell?.TryRelease(this);
            _occupiedCell = null;
        }

        /// <summary>将植物恢复到其所在行的常规渲染层。</summary>
        public void SetNormalMode()
        {
            SetSortingLayer("plant-"+Row);
        }
    }
}
