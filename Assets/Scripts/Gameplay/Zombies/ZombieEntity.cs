using System;
using PvZ.Bootstrap;
using PvZ.Config;
using PvZ.Core.Entities;
using PvZ.Gameplay.Board;
using PvZ.Gameplay.World;
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

        private bool _isOnBoard;
        private Shadow _shadow;

        public bool IsOnBoard => _isOnBoard;
        public int RowIndex => Row;
        public bool DrawsShadow => drawsShadow;
        public Vector3 ShadowCenterLocalPosition => new(
            shadowCenterLocalPosition.x,
            shadowCenterLocalPosition.y,
            0f);
        public float ShadowScale => shadowScale;

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
        }

        private void EnsureShadow()
        {
            if (_shadow != null || !drawsShadow) return;
            if (MainGameManager.Instance == null)
            {
                throw new MissingReferenceException(
                    $"{nameof(ZombieEntity)} requires an active {nameof(MainGameManager)} to create its shadow.");
            }

            var shadowPrefab = MainGameManager.Instance.GetObjectByType(
                GameConfigObject.ObjectType.PlanteShadow);
            var shadowObject = Instantiate(shadowPrefab, Transform, false);
            shadowObject.name = "ZombieShadow";
            shadowObject.transform.localPosition = ShadowCenterLocalPosition;
            shadowObject.transform.localRotation = Quaternion.identity;
            _shadow = shadowObject.GetComponent<Shadow>();

            if (_shadow == null)
            {
                Destroy(shadowObject);
                throw new MissingComponentException($"{shadowPrefab.name} requires a Shadow component.");
            }

            var drawNightShadow = GridManager.Instance != null && GridManager.Instance.IsNight;
            _shadow.SetNight(drawNightShadow).Initialize(shadowScale);
        }

        protected virtual void OnDestroy()
        {
            _isOnBoard = false;
        }
    }
}
