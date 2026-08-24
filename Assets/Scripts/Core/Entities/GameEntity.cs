using System;
using System.Collections.Generic;
using UnityEngine;

namespace PvZ.Core.Entities
{
    public interface IDetectorCallback
    {
    }

    public interface IDetector
    {
        Type CallbackType { get; }
        void Load(IDetectorCallback callback);
    }

    public abstract class GameEntity : EntitySprite
    {
        private const string DetectorRootPath = "detect";

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

            internal void Configure(
                Transform transform,
                IDetectorCallback detectorCallback)
            {
                detectorTransform = transform;
                callback = detectorCallback;
            }
        }

        // private readonly Dictionary<string, Task> _functions = new();

        [Header("Detection")]
        [SerializeField] private List<DetectorSlot> detectorSlots = new();

        protected Animator Animator; //动画

        public abstract string GetChineseName();
        public abstract string GetEnglishName();

        public IReadOnlyList<DetectorSlot> DetectorSlots => detectorSlots;

        protected sealed override void Awake()
        {
            base.Awake();
            Animator = GetComponentInChildren<Animator>();
        }

        public GameEntity ConfigureDetector(
            Transform detectorTransform,
            IDetectorCallback callback)
        {
            if (detectorTransform == null)
            {
                throw new ArgumentNullException(nameof(detectorTransform));
            }

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            var detector = ResolveDetector(detectorTransform);
            ValidateCallback(detector, callback);
            foreach (var slot in detectorSlots)
            {
                if (slot == null || slot.Transform != detectorTransform) continue;

                slot.Configure(detectorTransform, callback);
                return this;
            }

            detectorSlots.Add(new DetectorSlot(detectorTransform, callback));
            return this;
        }

        public void LoadDetectorCallbacks()
        {
            for (var i = 0; i < detectorSlots.Count; i++)
            {
                var slot = detectorSlots[i];
                if (slot == null)
                {
                    throw new MissingReferenceException(
                        $"{name} has an empty detector slot at index {i}.");
                }

                if (slot.Callback == null)
                {
                    throw new MissingReferenceException(
                        $"{name} detector slot {i} has no callback.");
                }

                if (slot.Transform == null)
                {
                    throw new MissingReferenceException(
                        $"{name} detector slot {i} has no Transform.");
                }

                for (var previousIndex = 0; previousIndex < i; previousIndex++)
                {
                    var previous = detectorSlots[previousIndex];
                    if (previous == null) continue;

                    if (previous.Transform == slot.Transform)
                    {
                        throw new InvalidOperationException(
                            $"{name} maps {slot.Transform.name} more than once.");
                    }

                    if (ReferenceEquals(previous.Callback, slot.Callback))
                    {
                        throw new InvalidOperationException(
                            $"{name} reuses one callback instance in multiple detector slots.");
                    }
                }

                var detector = ResolveDetector(slot.Transform);
                ValidateCallback(detector, slot.Callback);
                detector.Load(slot.Callback);
            }
        }

        private IDetector ResolveDetector(Transform detectorTransform)
        {
            if (detectorTransform == null)
            {
                throw new MissingReferenceException(
                    $"{name} has a detector slot without a Transform.");
            }

            var detectorRoot = transform.Find(DetectorRootPath);
            if (detectorRoot == null ||
                detectorTransform == detectorRoot ||
                !detectorTransform.IsChildOf(detectorRoot))
            {
                throw new ArgumentException(
                    $"{detectorTransform.name} must be a child of {name}/{DetectorRootPath}.",
                    nameof(detectorTransform));
            }

            IDetector detector = null;
            foreach (var behaviour in detectorTransform.GetComponents<MonoBehaviour>())
            {
                if (behaviour is not IDetector candidate) continue;
                if (detector != null)
                {
                    throw new InvalidOperationException(
                        $"{detectorTransform.name} must contain exactly one detector component.");
                }

                detector = candidate;
            }

            return detector ?? throw new MissingComponentException(
                $"{detectorTransform.name} requires a detector component.");
        }

        private static void ValidateCallback(
            IDetector detector,
            IDetectorCallback callback)
        {
            if (!detector.CallbackType.IsInstanceOfType(callback))
            {
                throw new ArgumentException(
                    $"{callback.GetType().Name} cannot be loaded by " +
                    $"{detector.GetType().Name}; it requires {detector.CallbackType.Name}.",
                    nameof(callback));
            }
        }
        
        public GameEntity SetParent(Transform parentTransform)
        {
            Transform.SetParent(parentTransform);
            return this;
        }
    
        public GameEntity SetPosition(Vector3 position)
        {
            Transform.position = position;
            return this;
        }
    
        public GameEntity SetLocalPosition(Vector3 position)
        {
            Transform.localPosition = position;
            return this;
        }
    
        public GameEntity SetLocalScale(Vector3 localScale)
        {
            Transform.localScale = localScale;
            return this;
        }

        public GameEntity SetName(string name)
        {
            Transform.name = name;
            return this;
        }
        
    }
}
