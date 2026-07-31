using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace Script.InputModule
{
    internal static class PointerPriorityPolicy
    {
        private const string SunTag = "Sun";

        public static RaycastResult Select(IReadOnlyList<RaycastResult> results, bool isPlanting)
        {
            var firstNormalResult = default(RaycastResult);
            var firstClickableResult = default(RaycastResult);

            for (var i = 0; i < results.Count; i++)
            {
                var result = results[i];
                if (result.gameObject == null) continue;

                if (result.gameObject.CompareTag(SunTag))
                {
                    if (!isPlanting) return result;
                    continue;
                }

                if (firstNormalResult.gameObject == null)
                {
                    firstNormalResult = result;
                }

                if (isPlanting &&
                    firstClickableResult.gameObject == null &&
                    ExecuteEvents.GetEventHandler<IPointerClickHandler>(result.gameObject) != null)
                {
                    firstClickableResult = result;
                }
            }

            return isPlanting && firstClickableResult.gameObject != null
                ? firstClickableResult
                : firstNormalResult;
        }
    }
}
