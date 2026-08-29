using System;
using PvZ.Presentation.Rendering;
using UnityEngine;
using UnityEngine.Serialization;

namespace PvZ.Presentation.Rendering
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class SpriteTransform : MonoBehaviour
    {
        private static readonly int Brightness = Shader.PropertyToID("_Brightness");
        private static readonly int Alpha = Shader.PropertyToID("_Alpha");

        [NonSerialized] public Material material;
        [NonSerialized] public bool hasMaterial;

        [FormerlySerializedAs("Position")]
        public Vector2 position;

        [FormerlySerializedAs("Scale")]
        public Vector2 scale = new(100f, 100f);

        [FormerlySerializedAs("Skew")]
        public Vector2 skew;

        [FormerlySerializedAs("Brightness")]
        [Range(0f, 2f)]
        public float brightness = 1f;

        public bool isBright;

        [FormerlySerializedAs("Alpha")]
        [Range(0f, 1f)]
        public float alpha = 1f;

        [FormerlySerializedAs("AlphaCoef")]
        [Range(0f, 1f)]
        public float alphaCoef = 1f;

        public bool updatePosition;

        [SerializeField, Tooltip("Generated second-rotation node. All position, scale and skew geometry is carried by the Unity hierarchy.")]
        private Transform nativeContent;

        [Tooltip("Marks a source-space reference node for descendants. SpriteTransform values remain absolute in the plant's FLA coordinate space.")]
        public bool providesChildSpritePosition;

        [Tooltip("Use spritePosition/Scale/Skew as this node's static FLA global reference matrix for a separately-authored child animation.")]
        public bool providesChildSpriteAffine;

        [FormerlySerializedAs("childSpritePosition")]
        [Tooltip("Static FLA-global reference position used by separately-authored descendant animation tracks.")]
        public Vector2 spritePosition;

        [Tooltip("Static FLA-global reference scale at the descendant attachment pose.")]
        public Vector2 spriteScale = new(100f, 100f);

        [Tooltip("Static FLA-global reference skew at the descendant attachment pose.")]
        public Vector2 spriteSkew;

        private Transform _cachedTransform;
        private SpriteRenderer _spriteRenderer;
        private MaterialPropertyBlock _materialProperties;
        private SpriteTransform _spritePositionProvider;
        private SpriteTransform _nativeSourceParent;
        private SpriteGroup _scheduler;
        private bool _isDestroying;
        private bool _positionReferenceResolved;
        private bool _nativeSourceParentResolved;

        private bool _hasBrightness;
        private bool _hasAlpha;

        private bool _materialStateApplied;
        private float _appliedBrightness;
        private float _appliedAlpha;

        public const string NativeContentName = "__AffineContent";
        public Transform NativeContent => nativeContent;
        public SpriteRenderer VisualRenderer => _spriteRenderer != null
            ? _spriteRenderer
            : FindVisualRenderer();

        public void ConfigureNativeHierarchy(Transform content)
        {
            nativeContent = content;
            InvalidateInitialization();
            Apply();
        }

        private void Awake()
        {
            EnsureInitialized();
            RegisterWithNearestScheduler();
        }

        private bool InitializeMaterial()
        {
            _spriteRenderer = FindVisualRenderer();
            if (_spriteRenderer == null || _spriteRenderer.sharedMaterial == null) return false;

            var sharedMaterial = _spriteRenderer.sharedMaterial;
            _hasBrightness = sharedMaterial.HasProperty(Brightness);
            _hasAlpha = sharedMaterial.HasProperty(Alpha);

            var hasSupportedProperty = _hasBrightness || _hasAlpha;
            if (!hasSupportedProperty) return false;

            material = sharedMaterial;
            _materialProperties = new MaterialPropertyBlock();
            return true;
        }

        private SpriteRenderer FindVisualRenderer()
        {
            if (nativeContent != null)
            {
                var contentRenderer = nativeContent.GetComponent<SpriteRenderer>();
                if (contentRenderer != null) return contentRenderer;
            }

            return GetComponent<SpriteRenderer>();
        }

        private void ResolvePositionReference()
        {
            if (_positionReferenceResolved) return;

            _positionReferenceResolved = true;
            _spritePositionProvider = null;

            // Start at the parent deliberately: a sub-animation root is positioned
            // in its parent's FLA space, while only its descendants use the static
            // spritePosition defined on that root.
            var parent = _cachedTransform.parent;
            while (parent != null)
            {
                if (parent.TryGetComponent<SpriteTransform>(out var spriteTransform) &&
                    spriteTransform.providesChildSpritePosition)
                {
                    _spritePositionProvider = spriteTransform;
                    return;
                }

                parent = parent.parent;
            }
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                RegisterWithNearestScheduler();
                if (_scheduler != null) return;
            }

            if (!Application.isPlaying)
            {
                Apply();
            }
        }

        private void RestoreSerializedDefaults()
        {
            var hasUninitializedValues = scale == Vector2.zero &&
                                         brightness == 0f &&
                                         alpha == 0f &&
                                         alphaCoef == 0f;
            if (!hasUninitializedValues) return;

            // Older prefabs serialized every field as zero. Geometry no longer
            // falls back to material values, so restore a valid hierarchy pose.
            scale = new Vector2(100f, 100f);
            brightness = 1f;
            alpha = 1f;

            if (hasMaterial && material != null)
            {
                if (_hasBrightness) brightness = material.GetFloat(Brightness);
                if (_hasAlpha) alpha = material.GetFloat(Alpha);
            }

            alphaCoef = 1f;
        }

        private void Update()
        {
            if (_scheduler != null) return;
            Apply();
        }

        private void OnDidApplyAnimationProperties()
        {
            if (_scheduler != null) return;
            Apply();
        }

        public void Apply()
        {
            EnsureInitialized();
            RestoreSerializedDefaults();
            ApplyPositionChange();
            ApplyNativeGeometry();
            ApplyMaterialChanges();
        }

        private void ResolveNativeSourceParent()
        {
            if (_nativeSourceParentResolved) return;

            _nativeSourceParentResolved = true;
            _nativeSourceParent = null;
            var parent = _cachedTransform.parent;
            while (parent != null)
            {
                if (parent.TryGetComponent<SpriteTransform>(out var spriteTransform) &&
                    spriteTransform.nativeContent != null &&
                    (spriteTransform.updatePosition ||
                     spriteTransform.providesChildSpritePosition ||
                     spriteTransform.providesChildSpriteAffine))
                {
                    _nativeSourceParent = spriteTransform;
                    return;
                }

                parent = parent.parent;
            }
        }

        internal void ApplyFromScheduler(SpriteGroup scheduler)
        {
            if (_scheduler != scheduler) return;

            EnsureInitialized();
            RestoreSerializedDefaults();
            ApplyPositionChange();
            ApplyNativeGeometry();
            ApplyMaterialChanges();
        }

        private void ApplyNativeGeometry()
        {
            if (nativeContent == null || nativeContent.parent != _cachedTransform) return;

            NativeAffineDecomposition.BuildSourceMatrix(
                scale,
                skew,
                out var m00,
                out var m01,
                out var m10,
                out var m11);
            if (updatePosition)
            {
                ResolveNativeLocalAffine(ref m00, ref m01, ref m10, ref m11);
            }
            NativeAffineDecomposition.Decompose(
                m00,
                m01,
                m10,
                m11,
                out var outerRotation,
                out var nativeScale,
                out var innerRotation);

            _cachedTransform.localRotation = Quaternion.Euler(0f, 0f, outerRotation);
            _cachedTransform.localScale = new Vector3(nativeScale.x, nativeScale.y, 1f);
            nativeContent.localPosition = Vector3.zero;
            nativeContent.localRotation = Quaternion.Euler(0f, 0f, innerRotation);
            nativeContent.localScale = Vector3.one;

        }

        private void ResolveNativeLocalAffine(
            ref float m00,
            ref float m01,
            ref float m10,
            ref float m11)
        {
            if (!TryGetNativeSourceParentInverse(
                    out var inverse00,
                    out var inverse01,
                    out var inverse10,
                    out var inverse11))
            {
                return;
            }

            var local00 = inverse00 * m00 + inverse01 * m10;
            var local01 = inverse00 * m01 + inverse01 * m11;
            var local10 = inverse10 * m00 + inverse11 * m10;
            var local11 = inverse10 * m01 + inverse11 * m11;
            m00 = local00;
            m01 = local01;
            m10 = local10;
            m11 = local11;
        }

        internal void SetScheduler(SpriteGroup scheduler)
        {
            if (!Application.isPlaying || scheduler == null) return;

            _scheduler = scheduler;
            enabled = false;
        }

        internal void ClearScheduler(SpriteGroup scheduler)
        {
            if (_scheduler != scheduler) return;

            _scheduler = null;
            if (Application.isPlaying && !_isDestroying && gameObject.activeInHierarchy)
            {
                enabled = true;
            }
        }

        private void RegisterWithNearestScheduler()
        {
            if (!Application.isPlaying || _scheduler != null) return;

            var scheduler = GetComponentInParent<SpriteGroup>();
            if (scheduler != null)
            {
                scheduler.RegisterSpriteTransform(this);
            }
        }

        private void EnsureInitialized()
        {
            if (_cachedTransform != null) return;

            _cachedTransform = transform;
            hasMaterial = InitializeMaterial();
            RestoreSerializedDefaults();
            ResolvePositionReference();
        }

        private void InvalidateInitialization()
        {
            _cachedTransform = null;
            _spriteRenderer = null;
            _materialProperties = null;
            _spritePositionProvider = null;
            _nativeSourceParent = null;
            _positionReferenceResolved = false;
            _nativeSourceParentResolved = false;

            _hasBrightness = false;
            _hasAlpha = false;
            _materialStateApplied = false;
        }

        public void ApplyAndDisable()
        {
            Apply();
            enabled = false;
        }

        public void RefreshPositionReference()
        {
            _spritePositionProvider = null;
            _nativeSourceParent = null;
            _positionReferenceResolved = false;
            _nativeSourceParentResolved = false;

            if (_cachedTransform != null)
            {
                ApplyPositionChange();
            }
        }

        public void RefreshDescendantPositionReferences()
        {
            foreach (var spriteTransform in GetComponentsInChildren<SpriteTransform>(true))
            {
                if (spriteTransform == this) continue;
                spriteTransform.RefreshPositionReference();
            }
        }

        private void RefreshDescendantMaterialTransforms()
        {
            foreach (var spriteTransform in GetComponentsInChildren<SpriteTransform>(true))
            {
                if (spriteTransform == this) continue;
                spriteTransform.InvalidateInitialization();
                spriteTransform.Apply();
            }
        }

        private void ApplyMaterialChanges()
        {
            if (!hasMaterial || material == null || _spriteRenderer == null) return;

            // MaterialPropertyBlock is transient Unity state. It can be cleared
            // by an editor validation/domain-reload cycle while the cached
            // renderer and material are still valid, so always create it lazily
            // at the point of use.
            if (_materialProperties == null)
            {
                _materialProperties = new MaterialPropertyBlock();
                _materialStateApplied = false;
            }

            var brightnessChanged = _hasBrightness && (!_materialStateApplied || _appliedBrightness != brightness);
            var combinedAlpha = alpha * alphaCoef;
            var alphaChanged = _hasAlpha && (!_materialStateApplied || _appliedAlpha != combinedAlpha);

            if (!brightnessChanged && !alphaChanged)
            {
                return;
            }

            _spriteRenderer.GetPropertyBlock(_materialProperties);

            if (brightnessChanged)
            {
                _materialProperties.SetFloat(Brightness, brightness);
                _appliedBrightness = brightness;
            }

            if (alphaChanged)
            {
                _materialProperties.SetFloat(Alpha, combinedAlpha);
                _appliedAlpha = combinedAlpha;
            }

            _spriteRenderer.SetPropertyBlock(_materialProperties);
            _materialStateApplied = true;
        }

        private void ApplyPositionChange()
        {
            if (!updatePosition) return;

            ResolvePositionReference();
            if (TryResolveNativeLocalPosition(out var targetLocalPosition))
            {
                if (_cachedTransform.localPosition != targetLocalPosition)
                {
                    // Every SpriteTransform track is an absolute matrix in the
                    // plant's FLA space. Unity needs the corresponding local
                    // translation: inverse(parent FLA matrix) * child FLA point.
                    _cachedTransform.localPosition = targetLocalPosition;
                }

                return;
            }

            var targetPosition = ResolveLocalPosition();

            if (_cachedTransform.localPosition != targetPosition)
            {
                _cachedTransform.localPosition = targetPosition;
            }
        }

        private bool TryResolveNativeLocalPosition(out Vector3 targetLocalPosition)
        {
            targetLocalPosition = default;
            if (!TryGetNativeSourceParentInverse(
                    out var inverse00,
                    out var inverse01,
                    out var inverse10,
                    out var inverse11))
            {
                return false;
            }

            var parentPosition = GetNativeSourceParentPosition();
            var sourceDelta = position - parentPosition;
            var deltaX = sourceDelta.x;
            var deltaY = -sourceDelta.y;
            var referenceLocalPoint = new Vector3(
                inverse00 * deltaX + inverse01 * deltaY,
                inverse10 * deltaX + inverse11 * deltaY,
                0f);

            var providerContent = _nativeSourceParent.nativeContent;
            if (providerContent == null || _cachedTransform.parent == providerContent)
            {
                targetLocalPosition = referenceLocalPoint;
                return true;
            }

            // Generated identity grouping nodes can sit between two source nodes.
            // Convert through the actual Unity hierarchy without changing the
            // source-space local result.
            var targetWorldPoint = providerContent.TransformPoint(referenceLocalPoint);
            targetLocalPosition = _cachedTransform.parent != null
                ? _cachedTransform.parent.InverseTransformPoint(targetWorldPoint)
                : targetWorldPoint;
            return true;
        }

        private bool TryGetNativeSourceParentInverse(
            out float inverse00,
            out float inverse01,
            out float inverse10,
            out float inverse11)
        {
            inverse00 = 1f;
            inverse01 = 0f;
            inverse10 = 0f;
            inverse11 = 1f;
            ResolveNativeSourceParent();
            if (_nativeSourceParent == null) return false;

            GetNativeSourceParentGeometry(out var parentScale, out var parentSkew);
            if (parentScale == Vector2.zero)
            {
                // Backward-compatible default for prefabs serialized before the
                // full source reference affine was introduced.
                parentScale = new Vector2(100f, 100f);
            }

            NativeAffineDecomposition.BuildSourceMatrix(
                parentScale,
                parentSkew,
                out var reference00,
                out var reference01,
                out var reference10,
                out var reference11);
            var determinant = reference00 * reference11 - reference01 * reference10;
            if (Mathf.Abs(determinant) <= 0.0000001f)
            {
                return false;
            }

            var inverseDeterminant = 1f / determinant;
            inverse00 = reference11 * inverseDeterminant;
            inverse01 = -reference01 * inverseDeterminant;
            inverse10 = -reference10 * inverseDeterminant;
            inverse11 = reference00 * inverseDeterminant;
            return true;
        }

        private Vector2 GetNativeSourceParentPosition()
        {
            if (_nativeSourceParent.providesChildSpriteAffine ||
                !_nativeSourceParent.updatePosition)
            {
                return _nativeSourceParent.spritePosition;
            }

            return _nativeSourceParent.position;
        }

        private void GetNativeSourceParentGeometry(
            out Vector2 parentScale,
            out Vector2 parentSkew)
        {
            if (_nativeSourceParent.providesChildSpriteAffine ||
                !_nativeSourceParent.updatePosition)
            {
                parentScale = _nativeSourceParent.spriteScale;
                parentSkew = _nativeSourceParent.spriteSkew;
                return;
            }

            parentScale = _nativeSourceParent.scale;
            parentSkew = _nativeSourceParent.skew;
        }

        private Vector3 ResolveLocalPosition()
        {
            if (_spritePositionProvider != null)
            {
                return ToLocalPosition(position, _spritePositionProvider.spritePosition);
            }

            return new Vector3(position.x, -position.y, 0f);
        }

        private static Vector3 ToLocalPosition(Vector2 sourcePosition, Vector2 sourceOrigin)
        {
            var delta = sourcePosition - sourceOrigin;
            return new Vector3(delta.x, -delta.y, 0f);
        }

        private void OnTransformParentChanged()
        {
            if (Application.isPlaying)
            {
                var previousScheduler = _scheduler;
                if (previousScheduler != null)
                {
                    previousScheduler.UnregisterSpriteTransform(this);
                }

                InvalidateInitialization();
                RegisterWithNearestScheduler();
                return;
            }

#if UNITY_EDITOR
            InvalidateInitialization();
            Apply();
            RefreshDescendantPositionReferences();
            RefreshDescendantMaterialTransforms();
#endif
        }

        private void OnDestroy()
        {
            _isDestroying = true;
            if (_scheduler != null)
            {
                _scheduler.UnregisterSpriteTransform(this);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) return;

            // Preview both per-renderer shader properties and animation-space
            // position changes while editing a prefab. Position remains gated by
            // updatePosition, exactly as it is at runtime.
            InvalidateInitialization();
            Apply();
            RefreshDescendantPositionReferences();
            RefreshDescendantMaterialTransforms();
        }
#endif

    }
}
