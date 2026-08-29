using NUnit.Framework;
using PvZ.Presentation.Rendering;
using PvZ.Gameplay.Presentation.EntityPreviews;
using UnityEditor;
using UnityEngine;

namespace PvZ.Tests.EditMode.Presentation
{
    public sealed partial class SpriteTransformChildPositionTests
    {
    [TestCase("Custom/LightnessSkewShader")]
            [TestCase("Custom/Particle")]
            public void PresentationShaders_ExposeNoGeometryProperties(string shaderName)
            {
                var shader = Shader.Find(shaderName);
                Assert.That(shader, Is.Not.Null);
                var material = new Material(shader);
                try
                {
                    Assert.That(material.HasProperty("_Brightness"), Is.True);
                    Assert.That(material.HasProperty("_Alpha"), Is.True);
                    Assert.That(material.HasProperty("_SkewX"), Is.False);
                    Assert.That(material.HasProperty("_ScaleX"), Is.False);
                    Assert.That(material.HasProperty("_AffineRow0"), Is.False);
                }
                finally
                {
                    Object.DestroyImmediate(material);
                }
            }
    }
}
