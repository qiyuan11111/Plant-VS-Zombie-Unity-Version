using UnityEngine;

namespace PvZ.Presentation
{
    /// <summary>
    /// Keeps a leaf sprite's Unity Transform pivot at the center of its
    /// inherited affine-rendered image while preserving the source transform.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteTransform))]
    public sealed class CenterInheritedSpritePivot : MonoBehaviour
    {
    }
}
