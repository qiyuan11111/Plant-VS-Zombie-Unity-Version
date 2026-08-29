using System;

namespace PvZ.Core.Detection
{
    public interface IDetector
    {
        Type CallbackType { get; }
        void Load(IDetectorCallback callback);
    }
}
