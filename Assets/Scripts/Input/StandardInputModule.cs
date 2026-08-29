using PvZ.Gameplay.Planting;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PvZ.Input
{
    [AddComponentMenu("Event/Standard Input Module")]
    public sealed class StandardInputModule : StandaloneInputModule
    {
        private readonly MouseState _mouseState = new MouseState();

        protected override MouseState GetMousePointerEventData(int id)
        {
            var created = GetPointerData(kMouseLeftId, out var leftData, true);
            leftData.Reset();

            if (created)
            {
                leftData.position = input.mousePosition;
            }

            var mousePosition = input.mousePosition;
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                leftData.position = new Vector2(-1f, -1f);
                leftData.delta = Vector2.zero;
            }
            else
            {
                leftData.delta = mousePosition - leftData.position;
                leftData.position = mousePosition;
            }

            leftData.scrollDelta = input.mouseScrollDelta;
            leftData.button = PointerEventData.InputButton.Left;
            eventSystem.RaycastAll(leftData, m_RaycastResultCache);

            var isPlanting = PlantingController.Instance != null && PlantingController.Instance.IsPlanting;
            leftData.pointerCurrentRaycast = PointerPriorityPolicy.Select(m_RaycastResultCache, isPlanting);
            m_RaycastResultCache.Clear();

            GetPointerData(kMouseRightId, out var rightData, true);
            rightData.Reset();
            CopyFromTo(leftData, rightData);
            rightData.button = PointerEventData.InputButton.Right;

            GetPointerData(kMouseMiddleId, out var middleData, true);
            middleData.Reset();
            CopyFromTo(leftData, middleData);
            middleData.button = PointerEventData.InputButton.Middle;

            _mouseState.SetButtonState(
                PointerEventData.InputButton.Left,
                StateForMouseButton(0),
                leftData);
            _mouseState.SetButtonState(
                PointerEventData.InputButton.Right,
                StateForMouseButton(1),
                rightData);
            _mouseState.SetButtonState(
                PointerEventData.InputButton.Middle,
                StateForMouseButton(2),
                middleData);

            return _mouseState;
        }
    }
}
