using UnityEngine;

namespace PvZ.Presentation
{
    /// <summary>
    /// Represents a 2D scale/skew matrix with the pivot Transform plus one
    /// ordinary Unity child Transform. The component never writes the pivot's
    /// position, while its rotation/scale and Content's rotation together form
    /// the requested affine matrix for every object under Content.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class NativeSkewTransform : MonoBehaviour
    {
        [SerializeField] private Vector2 scalePercent = new(100f, 100f);
        [SerializeField] private Vector2 skewDegrees;
        [SerializeField] private Transform content;

        private Vector2 appliedScalePercent = new(float.NaN, float.NaN);
        private Vector2 appliedSkewDegrees = new(float.NaN, float.NaN);

        public Vector2 ScalePercent
        {
            get => scalePercent;
            set
            {
                scalePercent = value;
                Apply();
            }
        }

        public Vector2 SkewDegrees
        {
            get => skewDegrees;
            set
            {
                skewDegrees = value;
                Apply();
            }
        }

        public Transform Content => content;

        private void OnEnable()
        {
            Apply();
        }

        private void OnValidate()
        {
            Apply();
        }

        private void Update()
        {
            if (appliedScalePercent != scalePercent || appliedSkewDegrees != skewDegrees)
            {
                Apply();
            }
        }

        public void Configure(Vector2 newScalePercent, Vector2 newSkewDegrees)
        {
            scalePercent = newScalePercent;
            skewDegrees = newSkewDegrees;
            Apply();
        }

        public Transform EnsureHierarchy()
        {
            content = EnsureDirectChild(content, "Content__SecondRotation_PutChildrenHere");
            Apply();
            return content;
        }

        public void Apply()
        {
            if (content == null)
            {
                return;
            }

            NativeAffineDecomposition.BuildSourceMatrix(
                scalePercent,
                skewDegrees,
                out var m00,
                out var m01,
                out var m10,
                out var m11);
            NativeAffineDecomposition.Decompose(
                m00,
                m01,
                m10,
                m11,
                out var outerRotation,
                out var nativeScale,
                out var innerRotation);

            // Position remains user-owned. This Transform supplies R(a) * S,
            // and the only generated child supplies R(b).
            transform.localRotation = Quaternion.Euler(0f, 0f, outerRotation);
            transform.localScale = new Vector3(nativeScale.x, nativeScale.y, 1f);
            SetLocalTransform(content, innerRotation, Vector2.one);

            appliedScalePercent = scalePercent;
            appliedSkewDegrees = skewDegrees;
        }

        public Matrix4x4 GetExpectedLocalMatrix()
        {
            NativeAffineDecomposition.BuildSourceMatrix(
                scalePercent,
                skewDegrees,
                out var m00,
                out var m01,
                out var m10,
                out var m11);
            var matrix = Matrix4x4.identity;
            matrix.m00 = m00;
            matrix.m01 = m01;
            matrix.m10 = m10;
            matrix.m11 = m11;
            return matrix;
        }

        public Matrix4x4 GetNativeLocalMatrix()
        {
            if (content == null) return Matrix4x4.identity;
            return Matrix4x4.TRS(Vector3.zero, transform.localRotation, transform.localScale) *
                   Matrix4x4.TRS(Vector3.zero, content.localRotation, content.localScale);
        }

        private Transform EnsureDirectChild(Transform current, string childName)
        {
            if (current == null)
            {
                current = transform.Find(childName);
            }

            if (current == null)
            {
                current = new GameObject(childName).transform;
            }

            if (current.parent != transform)
            {
                current.SetParent(transform, false);
            }

            current.name = childName;
            return current;
        }

        private static void SetLocalTransform(Transform target, float rotationZ, Vector2 nativeScale)
        {
            target.localPosition = Vector3.zero;
            target.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            target.localScale = new Vector3(nativeScale.x, nativeScale.y, 1f);
        }

        private void OnDrawGizmos()
        {
            var pivot = transform.position;
            const float radius = 8f;
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(pivot - Vector3.right * radius, pivot + Vector3.right * radius);
            Gizmos.DrawLine(pivot - Vector3.up * radius, pivot + Vector3.up * radius);

            if (content == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(pivot, content.TransformPoint(Vector3.right * 28f));
            Gizmos.color = Color.green;
            Gizmos.DrawLine(pivot, content.TransformPoint(Vector3.up * 28f));
        }
    }

    /// <summary>
    /// Shared 2D affine decomposition used by both the standalone examples and
    /// SpriteTransform's native hierarchy backend.
    /// </summary>
    internal static class NativeAffineDecomposition
    {
        internal static void BuildSourceMatrix(
            Vector2 percent,
            Vector2 angles,
            out float m00,
            out float m01,
            out float m10,
            out float m11)
        {
            var skewX = angles.x * Mathf.Deg2Rad;
            var skewY = angles.y * Mathf.Deg2Rad;
            var scaleX = percent.x / 100f;
            var scaleY = percent.y / 100f;

            // Same convention as SpriteTransform.CreateSkew * CreateScale.
            m00 = Mathf.Cos(skewY) * scaleX;
            m01 = Mathf.Sin(skewX) * scaleY;
            m10 = -Mathf.Sin(skewY) * scaleX;
            m11 = Mathf.Cos(skewX) * scaleY;
        }

        internal static void Decompose(
            float m00,
            float m01,
            float m10,
            float m11,
            out float outerRotation,
            out Vector2 nativeScale,
            out float innerRotation)
        {
            const float epsilon = 0.000001f;
            if (Mathf.Abs(m01) <= epsilon && Mathf.Abs(m10) <= epsilon)
            {
                outerRotation = 0f;
                nativeScale = new Vector2(m00, m11);
                innerRotation = 0f;
                return;
            }

            // 2x2 singular-value decomposition: M = R(a) * S * R(b).
            var c00 = m00 * m00 + m10 * m10;
            var c01 = m00 * m01 + m10 * m11;
            var c11 = m01 * m01 + m11 * m11;
            var eigenAngle = 0.5f * Mathf.Atan2(2f * c01, c00 - c11);
            var trace = c00 + c11;
            var radius = Mathf.Sqrt(
                Mathf.Max(0f, (c00 - c11) * (c00 - c11) + 4f * c01 * c01));
            var sigma1 = Mathf.Sqrt(Mathf.Max(0f, (trace + radius) * 0.5f));
            var sigma2 = Mathf.Sqrt(Mathf.Max(0f, (trace - radius) * 0.5f));
            var determinant = m00 * m11 - m01 * m10;
            if (determinant < 0f) sigma2 = -sigma2;

            var cosV = Mathf.Cos(eigenAngle);
            var sinV = Mathf.Sin(eigenAngle);
            float u00;
            float u10;
            if (Mathf.Abs(sigma1) > epsilon)
            {
                u00 = (m00 * cosV + m01 * sinV) / sigma1;
                u10 = (m10 * cosV + m11 * sinV) / sigma1;
            }
            else
            {
                u00 = 1f;
                u10 = 0f;
            }

            outerRotation = Mathf.Atan2(u10, u00) * Mathf.Rad2Deg;
            innerRotation = -eigenAngle * Mathf.Rad2Deg;
            nativeScale = new Vector2(sigma1, sigma2);
        }

    }
}
