using UnityEngine;

namespace PvZ.Presentation.Rendering
{
    /// <summary>Shared 2D affine matrix construction and decomposition.</summary>
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

            var c00 = m00 * m00 + m10 * m10;
            var c01 = m00 * m01 + m10 * m11;
            var c11 = m01 * m01 + m11 * m11;
            var eigenAngle = 0.5f * Mathf.Atan2(2f * c01, c00 - c11);
            var trace = c00 + c11;
            var radius = Mathf.Sqrt(
                Mathf.Max(0f, (c00 - c11) * (c00 - c11) + 4f * c01 * c01));
            var sigma1 = Mathf.Sqrt(Mathf.Max(0f, (trace + radius) * 0.5f));
            var sigma2 = Mathf.Sqrt(Mathf.Max(0f, (trace - radius) * 0.5f));
            if (m00 * m11 - m01 * m10 < 0f) sigma2 = -sigma2;

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
