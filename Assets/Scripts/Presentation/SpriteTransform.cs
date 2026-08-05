using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace PvZ.Presentation
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class SpriteTransform : MonoBehaviour
    {
        private static readonly int SkewX = Shader.PropertyToID("_SkewX");
        private static readonly int SkewY = Shader.PropertyToID("_SkewY");
        private static readonly int ScaleX = Shader.PropertyToID("_ScaleX");
        private static readonly int ScaleY = Shader.PropertyToID("_ScaleY");
        private static readonly int Brightness = Shader.PropertyToID("_Brightness");
        private static readonly int Alpha = Shader.PropertyToID("_Alpha");
        private static readonly int AffineRow0 = Shader.PropertyToID("_AffineRow0");
        private static readonly int AffineRow1 = Shader.PropertyToID("_AffineRow1");

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
        public float brightness;

        public bool isBright;

        [FormerlySerializedAs("Alpha")]
        [Range(0f, 1f)]
        public float alpha;

        [FormerlySerializedAs("AlphaCoef")]
        [Range(0f, 1f)]
        public float alphaCoef;

        public bool updatePosition;

        [Tooltip("Use this transform's static FLA position as the origin for descendant SpriteTransforms.")]
        public bool providesChildSpritePosition;

        [FormerlySerializedAs("childSpritePosition")]
        [Tooltip("FLA coordinate-space origin used by descendant SpriteTransforms.")]
        public Vector2 spritePosition;

        private Transform _cachedTransform;
        private SpriteRenderer _spriteRenderer;
        private MaterialPropertyBlock _materialProperties;
        private SpriteTransform _spritePositionProvider;
        private SpriteGroup _scheduler;
        private bool _isDestroying;
        private bool _positionReferenceResolved;

        private bool _hasSkewX;
        private bool _hasSkewY;
        private bool _hasScaleX;
        private bool _hasScaleY;
        private bool _hasBrightness;
        private bool _hasAlpha;
        private bool _hasAffineRow0;
        private bool _hasAffineRow1;

        private bool _materialStateApplied;
        private Vector2 _appliedSkew;
        private Vector2 _appliedScale;
        private float _appliedBrightness;
        private float _appliedAlpha;
        private Vector4 _appliedAffineRow0;
        private Vector4 _appliedAffineRow1;

        private SpriteTransform _hierarchyParent;
        private RelativeTransformState[] _relativeTransformStates = Array.Empty<RelativeTransformState>();
        private bool _hierarchyReferenceResolved;
        private bool _relativeTransformInitialized;
        private bool _hierarchyStateInitialized;
        private Vector2 _cachedHierarchySkew;
        private Vector2 _cachedHierarchyScale;
        private uint _observedParentHierarchyVersion;
        private uint _hierarchyVersion;
        private Affine2D _relativeToHierarchyParent = Affine2D.Identity;
        private Affine2D _localAffine = Affine2D.Identity;
        private Affine2D _rendererAffine = Affine2D.Identity;
        private Affine2D _descendantAffine = Affine2D.Identity;
        private bool _hasDescendantTransform;

        private void Awake()
        {
            EnsureInitialized();
            RegisterWithNearestScheduler();
        }

        private bool InitializeMaterial()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null || _spriteRenderer.sharedMaterial == null) return false;

            var sharedMaterial = _spriteRenderer.sharedMaterial;
            _hasSkewX = sharedMaterial.HasProperty(SkewX);
            _hasSkewY = sharedMaterial.HasProperty(SkewY);
            _hasScaleX = sharedMaterial.HasProperty(ScaleX);
            _hasScaleY = sharedMaterial.HasProperty(ScaleY);
            _hasBrightness = sharedMaterial.HasProperty(Brightness);
            _hasAlpha = sharedMaterial.HasProperty(Alpha);
            _hasAffineRow0 = sharedMaterial.HasProperty(AffineRow0);
            _hasAffineRow1 = sharedMaterial.HasProperty(AffineRow1);

            var hasSupportedProperty = _hasSkewX || _hasSkewY ||
                                       _hasScaleX || _hasScaleY ||
                                       _hasBrightness || _hasAlpha ||
                                       _hasAffineRow0 || _hasAffineRow1;
            if (!hasSupportedProperty) return false;

            material = sharedMaterial;
            _materialProperties = new MaterialPropertyBlock();
            return true;
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

        private void RestoreLegacyMaterialDefaults()
        {
            var hasUninitializedValues = scale == Vector2.zero &&
                                         brightness == 0f &&
                                         alpha == 0f &&
                                         alphaCoef == 0f;
            if (!hasUninitializedValues) return;

            // Older prefabs serialized every field as zero. Empty attachment
            // nodes have no renderer/material, but they still participate in
            // inherited affine propagation now, so their missing scale must be
            // restored to the identity instead of collapsing every descendant.
            scale = new Vector2(100f, 100f);
            brightness = 1f;
            alpha = 1f;

            if (hasMaterial && material != null)
            {
                if (_hasScaleX) scale.x = material.GetFloat(ScaleX);
                if (_hasScaleY) scale.y = material.GetFloat(ScaleY);
                if (_hasSkewX) skew.x = material.GetFloat(SkewX);
                if (_hasSkewY) skew.y = material.GetFloat(SkewY);
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
            ApplyPositionChange();
            EnsureHierarchyState(true);
            ApplyMaterialChanges();
        }

        internal void ApplyFromScheduler(SpriteGroup scheduler)
        {
            if (_scheduler != scheduler) return;

            EnsureInitialized();
            ApplyPositionChange();
            EnsureHierarchyState(false);
            ApplyMaterialChanges();
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
            RestoreLegacyMaterialDefaults();
            ResolvePositionReference();
        }

        private void InvalidateInitialization()
        {
            _cachedTransform = null;
            _spriteRenderer = null;
            _materialProperties = null;
            _spritePositionProvider = null;
            _positionReferenceResolved = false;

            _hasSkewX = false;
            _hasSkewY = false;
            _hasScaleX = false;
            _hasScaleY = false;
            _hasBrightness = false;
            _hasAlpha = false;
            _hasAffineRow0 = false;
            _hasAffineRow1 = false;
            _materialStateApplied = false;

            _hierarchyParent = null;
            _relativeTransformStates = Array.Empty<RelativeTransformState>();
            _hierarchyReferenceResolved = false;
            _relativeTransformInitialized = false;
            _hierarchyStateInitialized = false;
            _rendererAffine = Affine2D.Identity;
            _descendantAffine = Affine2D.Identity;
            _hasDescendantTransform = false;
        }

        public void ApplyAndDisable()
        {
            Apply();
            enabled = false;
        }

        public void RefreshPositionReference()
        {
            _spritePositionProvider = null;
            _positionReferenceResolved = false;

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

            var skewXChanged = _hasSkewX && (!_materialStateApplied || _appliedSkew.x != skew.x);
            var skewYChanged = _hasSkewY && (!_materialStateApplied || _appliedSkew.y != skew.y);
            var scaleXChanged = _hasScaleX && (!_materialStateApplied || _appliedScale.x != scale.x);
            var scaleYChanged = _hasScaleY && (!_materialStateApplied || _appliedScale.y != scale.y);
            var brightnessChanged = _hasBrightness && (!_materialStateApplied || _appliedBrightness != brightness);
            var combinedAlpha = alpha * alphaCoef;
            var alphaChanged = _hasAlpha && (!_materialStateApplied || _appliedAlpha != combinedAlpha);
            var affineRow0 = _rendererAffine.Row0;
            var affineRow1 = _rendererAffine.Row1;
            var affineRow0Changed = _hasAffineRow0 &&
                                    (!_materialStateApplied || _appliedAffineRow0 != affineRow0);
            var affineRow1Changed = _hasAffineRow1 &&
                                    (!_materialStateApplied || _appliedAffineRow1 != affineRow1);

            if (!skewXChanged && !skewYChanged &&
                !scaleXChanged && !scaleYChanged &&
                !brightnessChanged && !alphaChanged &&
                !affineRow0Changed && !affineRow1Changed)
            {
                return;
            }

            _spriteRenderer.GetPropertyBlock(_materialProperties);

            if (skewXChanged)
            {
                _materialProperties.SetFloat(SkewX, skew.x);
                _appliedSkew.x = skew.x;
            }

            if (skewYChanged)
            {
                _materialProperties.SetFloat(SkewY, skew.y);
                _appliedSkew.y = skew.y;
            }

            if (scaleXChanged)
            {
                _materialProperties.SetFloat(ScaleX, scale.x);
                _appliedScale.x = scale.x;
            }

            if (scaleYChanged)
            {
                _materialProperties.SetFloat(ScaleY, scale.y);
                _appliedScale.y = scale.y;
            }

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

            if (affineRow0Changed)
            {
                _materialProperties.SetVector(AffineRow0, affineRow0);
                _appliedAffineRow0 = affineRow0;
            }

            if (affineRow1Changed)
            {
                _materialProperties.SetVector(AffineRow1, affineRow1);
                _appliedAffineRow1 = affineRow1;
            }

            _spriteRenderer.SetPropertyBlock(_materialProperties);
            _materialStateApplied = true;
        }

        private void ApplyPositionChange()
        {
            if (!updatePosition) return;

            ResolvePositionReference();
            var targetPosition = ResolveLocalPosition();

            if (_cachedTransform.localPosition != targetPosition)
            {
                _cachedTransform.localPosition = targetPosition;
            }
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

        private void EnsureHierarchyState(bool ensureParentState)
        {
            EnsureInitialized();
            ResolveHierarchyReference();

            if (_hierarchyParent != null &&
                (ensureParentState || _hierarchyParent._scheduler != _scheduler))
            {
                _hierarchyParent.EnsureInitialized();
                _hierarchyParent.ApplyPositionChange();
                _hierarchyParent.EnsureHierarchyState(true);
            }

            var parentVersion = _hierarchyParent == null
                ? 0u
                : _hierarchyParent._hierarchyVersion;
            var parentHasTransform = _hierarchyParent != null &&
                                     _hierarchyParent._hasDescendantTransform;
            var parentChanged = !_hierarchyStateInitialized ||
                                _observedParentHierarchyVersion != parentVersion;
            var relativeChanged = parentHasTransform && RefreshRelativeTransform();
            var localChanged = !_hierarchyStateInitialized ||
                               _cachedHierarchySkew != skew ||
                               _cachedHierarchyScale != scale;

            if (!parentChanged && !relativeChanged && !localChanged) return;

            if (localChanged)
            {
                _localAffine = Affine2D.CreateSkew(skew) * Affine2D.CreateScale(scale);
                _cachedHierarchySkew = skew;
                _cachedHierarchyScale = scale;
            }

            var newRendererAffine = Affine2D.Identity;
            var hasInheritedTransform = parentHasTransform;
            if (hasInheritedTransform &&
                _relativeToHierarchyParent.TryInverse(out var inverseRelative))
            {
                newRendererAffine = inverseRelative *
                                    _hierarchyParent._descendantAffine *
                                    _relativeToHierarchyParent;
            }
            else if (hasInheritedTransform)
            {
                hasInheritedTransform = false;
            }

            var newHasDescendantTransform = hasInheritedTransform ||
                                            _localAffine != Affine2D.Identity;
            var newDescendantAffine = newRendererAffine * _localAffine;
            var descendantStateChanged = !_hierarchyStateInitialized ||
                                         _hasDescendantTransform != newHasDescendantTransform ||
                                         _descendantAffine != newDescendantAffine;

            _rendererAffine = newRendererAffine;
            _descendantAffine = newDescendantAffine;
            _hasDescendantTransform = newHasDescendantTransform;
            _observedParentHierarchyVersion = parentVersion;
            _hierarchyStateInitialized = true;

            if (descendantStateChanged)
            {
                unchecked
                {
                    _hierarchyVersion++;
                    if (_hierarchyVersion == 0u) _hierarchyVersion = 1u;
                }
            }
        }

        private void ResolveHierarchyReference()
        {
            if (_hierarchyReferenceResolved) return;

            _hierarchyReferenceResolved = true;
            _hierarchyParent = null;
            var pathLength = 1;
            var parent = _cachedTransform.parent;

            while (parent != null)
            {
                if (parent.TryGetComponent(out _hierarchyParent)) break;
                pathLength++;
                parent = parent.parent;
            }

            if (_hierarchyParent == null) return;

            _relativeTransformStates = new RelativeTransformState[pathLength];
            var current = _cachedTransform;
            for (var index = 0; index < pathLength; index++)
            {
                _relativeTransformStates[index].Transform = current;
                current = current.parent;
            }
        }

        private bool RefreshRelativeTransform()
        {
            if (_hierarchyParent == null) return false;

            var changed = !_relativeTransformInitialized;
            for (var index = 0; index < _relativeTransformStates.Length; index++)
            {
                var state = _relativeTransformStates[index];
                var localPosition = state.Transform.localPosition;
                if (_relativeTransformInitialized && state.LocalPosition == localPosition)
                {
                    continue;
                }

                state.LocalPosition = localPosition;
                state.LocalAffine = Affine2D.CreateTranslation(localPosition.x, localPosition.y);
                _relativeTransformStates[index] = state;
                changed = true;
            }

            if (!changed) return false;

            var relative = Affine2D.Identity;
            for (var index = 0; index < _relativeTransformStates.Length; index++)
            {
                relative = _relativeTransformStates[index].LocalAffine * relative;
            }

            _relativeToHierarchyParent = relative;
            _relativeTransformInitialized = true;
            return true;
        }

        private struct RelativeTransformState
        {
            public Transform Transform;
            public Vector3 LocalPosition;
            public Affine2D LocalAffine;
        }

        private readonly struct Affine2D : IEquatable<Affine2D>
        {
            private const float InvertibleEpsilon = 0.0000001f;

            public static readonly Affine2D Identity = new(
                1f, 0f, 0f,
                0f, 1f, 0f);

            private readonly float _m00;
            private readonly float _m01;
            private readonly float _m02;
            private readonly float _m10;
            private readonly float _m11;
            private readonly float _m12;

            public Vector4 Row0 => new(_m00, _m01, _m02, 0f);
            public Vector4 Row1 => new(_m10, _m11, _m12, 0f);

            private Affine2D(
                float m00, float m01, float m02,
                float m10, float m11, float m12)
            {
                _m00 = m00;
                _m01 = m01;
                _m02 = m02;
                _m10 = m10;
                _m11 = m11;
                _m12 = m12;
            }

            public static Affine2D CreateTranslation(float x, float y)
            {
                return new Affine2D(
                    1f, 0f, x,
                    0f, 1f, y);
            }

            public static Affine2D CreateScale(Vector2 value)
            {
                return new Affine2D(
                    value.x / 100f, 0f, 0f,
                    0f, value.y / 100f, 0f);
            }

            public static Affine2D CreateSkew(Vector2 angles)
            {
                var skewX = -angles.x * Mathf.Deg2Rad;
                var skewY = -angles.y * Mathf.Deg2Rad;
                return new Affine2D(
                    Mathf.Cos(skewY), -Mathf.Sin(skewX), 0f,
                    Mathf.Sin(skewY), Mathf.Cos(skewX), 0f);
            }

            public bool TryInverse(out Affine2D inverse)
            {
                var determinant = _m00 * _m11 - _m01 * _m10;
                if (Mathf.Abs(determinant) <= InvertibleEpsilon)
                {
                    inverse = Identity;
                    return false;
                }

                var inverseDeterminant = 1f / determinant;
                var m00 = _m11 * inverseDeterminant;
                var m01 = -_m01 * inverseDeterminant;
                var m10 = -_m10 * inverseDeterminant;
                var m11 = _m00 * inverseDeterminant;
                inverse = new Affine2D(
                    m00,
                    m01,
                    -(m00 * _m02 + m01 * _m12),
                    m10,
                    m11,
                    -(m10 * _m02 + m11 * _m12));
                return true;
            }

            public static Affine2D operator *(Affine2D left, Affine2D right)
            {
                return new Affine2D(
                    left._m00 * right._m00 + left._m01 * right._m10,
                    left._m00 * right._m01 + left._m01 * right._m11,
                    left._m00 * right._m02 + left._m01 * right._m12 + left._m02,
                    left._m10 * right._m00 + left._m11 * right._m10,
                    left._m10 * right._m01 + left._m11 * right._m11,
                    left._m10 * right._m02 + left._m11 * right._m12 + left._m12);
            }

            public bool Equals(Affine2D other)
            {
                return _m00.Equals(other._m00) &&
                       _m01.Equals(other._m01) &&
                       _m02.Equals(other._m02) &&
                       _m10.Equals(other._m10) &&
                       _m11.Equals(other._m11) &&
                       _m12.Equals(other._m12);
            }

            public override bool Equals(object obj)
            {
                return obj is Affine2D other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = _m00.GetHashCode();
                    hashCode = (hashCode * 397) ^ _m01.GetHashCode();
                    hashCode = (hashCode * 397) ^ _m02.GetHashCode();
                    hashCode = (hashCode * 397) ^ _m10.GetHashCode();
                    hashCode = (hashCode * 397) ^ _m11.GetHashCode();
                    hashCode = (hashCode * 397) ^ _m12.GetHashCode();
                    return hashCode;
                }
            }

            public static bool operator ==(Affine2D left, Affine2D right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Affine2D left, Affine2D right)
            {
                return !left.Equals(right);
            }
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
