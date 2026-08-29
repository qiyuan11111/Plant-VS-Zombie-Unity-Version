using UnityEngine;

namespace PvZ.Presentation.Rendering
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

}
