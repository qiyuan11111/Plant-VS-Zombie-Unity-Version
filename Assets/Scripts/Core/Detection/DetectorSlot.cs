using System;
using UnityEngine;

namespace PvZ.Core.Detection
{
    [Serializable]
    public sealed class DetectorSlot
    {
        [SerializeField] private Transform detectorTransform;
        [SerializeReference] private IDetectorCallback callback;

        public Transform Transform => detectorTransform;
        public IDetectorCallback Callback => callback;

        public DetectorSlot(Transform transform, IDetectorCallback detectorCallback)
        {
            detectorTransform = transform;
            callback = detectorCallback;
        }

        internal void Configure(Transform transform, IDetectorCallback detectorCallback)
        {
            detectorTransform = transform;
            callback = detectorCallback;
        }
    }
}
