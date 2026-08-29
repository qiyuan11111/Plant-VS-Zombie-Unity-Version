using System;
using PvZ.Gameplay.Entities;
using PvZ.Gameplay.Board;
using PvZ.Gameplay.Detection;
using PvZ.Gameplay.Detection.Zombies;
using PvZ.Gameplay.Presentation.Shadows;
using UnityEngine;

namespace PvZ.Gameplay.Zombies
{
    public abstract class ZombieEntity : Character
    {
        private static readonly Vector3 BoardScale = Vector3.one;

        [Header("Shadow")]
        [SerializeField] private bool drawsShadow = true;
        [SerializeField] private Vector2 shadowCenterLocalPosition = new(-20.125f, -24.525f);
        [SerializeField, Min(0.01f)] private float shadowScale = Shadow.LargeScale;
        [SerializeField] private ZombieBodyCollider zombieBody;

        private bool _isOnBoard;
        private Shadow _shadow;
        private bool _bodyEventsBound;

        public bool IsOnBoard => _isOnBoard;
        public int RowIndex => Row;
        public bool DrawsShadow => drawsShadow;
        public Vector3 ShadowCenterLocalPosition => new(
            shadowCenterLocalPosition.x,
            shadowCenterLocalPosition.y,
            0f);
        public float ShadowScale => shadowScale;
        public ZombieBodyCollider ZombieBody =>
            zombieBody != null ? zombieBody : GetComponent<ZombieBodyCollider>();

        public ZombieEntity ConfigureBodyCollider(ZombieBodyCollider bodyCollider)
        {
            if (bodyCollider == null)
            {
                throw new ArgumentNullException(nameof(bodyCollider));
            }

            if (bodyCollider.gameObject != gameObject)
            {
                throw new ArgumentException(
                    $"{nameof(ZombieBodyCollider)} must be on the zombie root.",
                    nameof(bodyCollider));
            }

            zombieBody = bodyCollider;
            return this;
        }

        public ZombieEntity EnterBoard(int row, Vector3 localPosition)
        {
            if (_isOnBoard)
            {
                throw new InvalidOperationException($"{name} is already on the board.");
            }

            if (row < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(row), row, "A zombie row cannot be negative.");
            }

            SetRow(row).SetHeight(0f);
            ResetHealth();

            GetComponentRoot()
                .SetSortingLayer("zombie-" + row)
                .SetColliderState(true);

            SetLocalScale(BoardScale);
            SetLocalPosition(localPosition);
            SetName(GetEnglishName() + "-" + row);

            EnsureShadow();
            _isOnBoard = true;
            enabled = true;
            OnEnteredBoard();
            return this;
        }

        protected virtual void OnEnteredBoard()
        {
            BindBodyEvents();
            LoadDetectorCallbacks();
        }

        protected virtual void OnDetectorEntered(GameEntity detector)
        {
        }

        protected virtual void OnDetectorStayed(GameEntity detector)
        {
        }

        protected virtual void OnDetectorExited(GameEntity detector)
        {
        }

        private void BindBodyEvents()
        {
            if (_bodyEventsBound) return;

            zombieBody = ZombieBody;
            if (zombieBody == null) return;
            if (zombieBody.Zombie != this)
            {
                throw new MissingComponentException(
                    $"{name} requires a configured {nameof(ZombieBodyCollider)} on its root.");
            }

            zombieBody.DetectorEntered += OnDetectorEntered;
            zombieBody.DetectorStayed += OnDetectorStayed;
            zombieBody.DetectorExited += OnDetectorExited;
            _bodyEventsBound = true;
        }

        private void UnbindBodyEvents()
        {
            if (!_bodyEventsBound || zombieBody == null) return;

            zombieBody.DetectorEntered -= OnDetectorEntered;
            zombieBody.DetectorStayed -= OnDetectorStayed;
            zombieBody.DetectorExited -= OnDetectorExited;
            _bodyEventsBound = false;
        }

        private void EnsureShadow()
        {
            if (_shadow != null || !drawsShadow) return;
            var drawNightShadow = BoardGrid.Instance != null && BoardGrid.Instance.IsNight;
            _shadow = ShadowFactory.Create(
                Transform,
                ShadowCenterLocalPosition,
                shadowScale,
                drawNightShadow,
                "ZombieShadow");
        }

        protected virtual void OnDestroy()
        {
            UnbindBodyEvents();
            _isOnBoard = false;
        }
    }
}
