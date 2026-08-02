using System.Collections.Generic;
using NUnit.Framework;
using Script.InputModule;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tests.EditMode
{
    public sealed class PointerPriorityPolicyTests
    {
        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var gameObject in _objects)
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }

            _objects.Clear();
        }

        [Test]
        public void Select_WhenNotPlanting_PrioritizesSunOverEarlierResult()
        {
            var normal = CreateObject("Normal");
            var sun = CreateObject("Sun");
            sun.tag = "Sun";
            var results = new[] { Hit(normal), Hit(sun) };

            var selected = PointerPriorityPolicy.Select(results, false);

            Assert.That(selected.gameObject, Is.SameAs(sun));
        }

        [Test]
        public void Select_WhenPlanting_SkipsSunAndPrefersClickableResult()
        {
            var sun = CreateObject("Sun");
            sun.tag = "Sun";
            var normal = CreateObject("Normal");
            var clickableParent = CreateObject("ClickableParent");
            clickableParent.AddComponent<TestPointerClickHandler>();
            var clickableChild = CreateObject("ClickableChild");
            clickableChild.transform.SetParent(clickableParent.transform);
            var results = new[] { Hit(sun), Hit(normal), Hit(clickableChild) };

            var selected = PointerPriorityPolicy.Select(results, true);

            Assert.That(selected.gameObject, Is.SameAs(clickableChild));
        }

        [Test]
        public void Select_WhenPlantingWithoutClickableResult_ReturnsFirstNonSunResult()
        {
            var sun = CreateObject("Sun");
            sun.tag = "Sun";
            var first = CreateObject("First");
            var second = CreateObject("Second");
            var results = new[] { Hit(sun), Hit(first), Hit(second) };

            var selected = PointerPriorityPolicy.Select(results, true);

            Assert.That(selected.gameObject, Is.SameAs(first));
        }

        [Test]
        public void Select_WithNoValidResults_ReturnsDefaultResult()
        {
            var results = new[] { default(RaycastResult) };

            var selected = PointerPriorityPolicy.Select(results, false);

            Assert.That(selected.gameObject, Is.Null);
        }

        private GameObject CreateObject(string name)
        {
            var gameObject = new GameObject(name);
            _objects.Add(gameObject);
            return gameObject;
        }

        private static RaycastResult Hit(GameObject gameObject)
        {
            return new RaycastResult { gameObject = gameObject };
        }
    }

    internal sealed class TestPointerClickHandler : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
        }
    }
}
