using System.Collections.Generic;
using PvZ.Presentation.Rendering;
using PvZ.Gameplay.Presentation.EntityPreviews;
using UnityEngine;
using ComponentUtility = UnityEditorInternal.ComponentUtility;

namespace PvZ.Editor.PrefabPipeline.Common
{
    internal static class PrefabHierarchyUtility
    {
        public static Transform GetOrCreatePath(Transform root, string path)
        {
            var current = root;
            foreach (var segment in path.Split('/'))
            {
                current = GetOrCreateChild(current, segment, root.gameObject.layer);
            }

            return current;
        }

        public static Transform GetOrCreateChild(Transform parent, string name)
        {
            return GetOrCreateChild(parent, name, parent.gameObject.layer);
        }

        public static Transform GetOrCreateChild(Transform parent, string name, int layer)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                child = new GameObject(name) { layer = layer }.transform;
                child.SetParent(parent, false);
            }
            else
            {
                child.gameObject.layer = layer;
            }

            return child;
        }

        public static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            foreach (Transform child in target.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        public static int GetDepth(Transform target)
        {
            var depth = 0;
            while (target.parent != null)
            {
                depth++;
                target = target.parent;
            }

            return depth;
        }

        public static bool EnsureNativeHierarchy(SpriteTransform spriteTransform)
        {
            var pivot = spriteTransform.transform;
            var content = spriteTransform.NativeContent;
            var changed = false;

            if (content == null || content.parent != pivot)
            {
                content = pivot.Find(SpriteTransform.NativeContentName);
            }

            if (content == null)
            {
                content = new GameObject(SpriteTransform.NativeContentName)
                {
                    layer = pivot.gameObject.layer
                }.transform;
                content.SetParent(pivot, false);
                changed = true;
            }
            else if (content.gameObject.layer != pivot.gameObject.layer)
            {
                content.gameObject.layer = pivot.gameObject.layer;
                changed = true;
            }

            var directChildren = new List<Transform>();
            foreach (Transform child in pivot)
            {
                if (child != content) directChildren.Add(child);
            }

            foreach (var child in directChildren)
            {
                child.SetParent(content, false);
                changed = true;
            }

            var pivotRenderer = pivot.GetComponent<SpriteRenderer>();
            var contentRenderer = content.GetComponent<SpriteRenderer>();
            if (pivotRenderer != null)
            {
                ComponentUtility.CopyComponent(pivotRenderer);
                if (contentRenderer == null)
                {
                    ComponentUtility.PasteComponentAsNew(content.gameObject);
                }
                else
                {
                    ComponentUtility.PasteComponentValues(contentRenderer);
                }

                Object.DestroyImmediate(pivotRenderer);
                changed = true;
            }

            if (spriteTransform.NativeContent != content)
            {
                spriteTransform.ConfigureNativeHierarchy(content);
                changed = true;
            }

            return changed;
        }
    }
}
