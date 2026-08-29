using UnityEngine;

namespace PvZ.Input
{
    public static class PointerWorldPosition
    {
        public static Vector3 Get(float depth, Camera camera = null)
        {
            camera ??= Camera.main;
            if (camera == null)
            {
                throw new MissingReferenceException("A Camera tagged MainCamera is required.");
            }

            var position = camera.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
            position.z = depth;
            return position;
        }
    }
}
