using UnityEngine;

namespace SwordPrototype.Battle
{
    /// <summary>소유자가 Tick으로 진행시키는 단순 쿨타임 타이머.</summary>
    public sealed class Cooldown
    {
        public float Remaining { get; private set; }
        public bool Ready => Remaining <= 0f;

        public void Start(float seconds) => Remaining = Mathf.Max(0f, seconds);
        public void Tick(float deltaTime) => Remaining = Mathf.Max(0f, Remaining - deltaTime);
        public void Refund(float fraction) => Remaining *= 1f - Mathf.Clamp01(fraction);
        public void Clear() => Remaining = 0f;
    }
}
