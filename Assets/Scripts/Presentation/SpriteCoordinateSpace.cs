using UnityEngine;

namespace Script
{
    /// <summary>
    /// Defines the FLA coordinate origin used by descendant SpriteTransforms.
    /// Source values use X-right and Y-down without manual conversion.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class SpriteCoordinateSpace : MonoBehaviour
    {
        [SerializeField] private Vector2 spritePosition;

        public Vector2 SpritePosition
        {
            get => spritePosition;
            set => spritePosition = value;
        }

        public Vector3 ToLocalPosition(Vector2 sourcePosition)
        {
            var delta = sourcePosition - spritePosition;
            return new Vector3(delta.x, -delta.y, 0f);
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            RefreshDescendants();
        }

        private void OnValidate()
        {
            RefreshDescendants();
        }

        private void RefreshDescendants()
        {
            foreach (var spriteTransform in GetComponentsInChildren<SpriteTransform>(true))
            {
                if (spriteTransform.transform == transform) continue;
                spriteTransform.RefreshCoordinateSpace();
            }
        }
#endif
    }
}
