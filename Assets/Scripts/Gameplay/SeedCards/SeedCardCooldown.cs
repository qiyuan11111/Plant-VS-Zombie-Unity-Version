using System;

namespace Prefab.Object.SeedCard.Script
{
    public sealed class SeedCardCooldown
    {
        public float Duration { get; private set; }
        public float Remaining { get; private set; }
        public bool IsReady => Remaining <= 0f;
        public float Progress => Duration > 0f ? Remaining / Duration : 0f;

        public void Start(float duration)
        {
            Duration = Math.Max(0f, duration);
            Remaining = Duration;
        }

        public bool Tick(float deltaTime)
        {
            if (IsReady || deltaTime <= 0f) return false;

            Remaining = Math.Max(0f, Remaining - deltaTime);
            return true;
        }
    }
}
