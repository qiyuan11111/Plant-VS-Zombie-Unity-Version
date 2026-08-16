using System;
using PvZ.Core.Entities;
using UnityEngine;

namespace PvZ.Gameplay.Zombies
{
    public abstract class ZombieEntity : Character
    {
        private static readonly Vector3 BoardScale = Vector3.one;

        private bool _isOnBoard;

        public bool IsOnBoard => _isOnBoard;
        public int RowIndex => Row;

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

            _isOnBoard = true;
            enabled = true;
            OnEnteredBoard();
            return this;
        }

        protected virtual void OnEnteredBoard()
        {
        }

        protected virtual void OnDestroy()
        {
            _isOnBoard = false;
        }
    }
}
