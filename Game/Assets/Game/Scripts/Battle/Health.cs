using System;
using UnityEngine;

namespace SwordPrototype.Battle
{
    /// <summary>체력과 사망. 피해 적용·무적·패링 판정 지점만 담당하고 연출은 하지 않는다.</summary>
    public sealed class Health : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        private float invulnerableUntil;

        public float Max => maxHealth;
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;
        /// <summary>선택 슬로모션 중 일반 피격 차단 등 상태 기반 무적.</summary>
        public bool Invulnerable { get; set; }
        /// <summary>대쉬 무적처럼 시간 기반 무적까지 포함한 최종 무적 여부(전투 시계 기준 — 메뉴 동안 멈춤).</summary>
        public bool IsInvulnerable => Invulnerable || BattleClock.Now < invulnerableUntil;
        /// <summary>피해가 들어올 때 패링 성공 여부를 묻는 콜백. true면 피해 무효.</summary>
        public Func<bool> ParryCheck { get; set; }
        /// <summary>실제로 체력이 깎였을 때(깎인 양). 결과 화면 기록용.</summary>
        public event Action<float> Damaged;

        private void Awake() { Current = maxHealth; }

        public void SetMax(float value)
        {
            maxHealth = Mathf.Max(1f, value);
            Current = maxHealth;
        }

        public void GrantInvulnerability(float seconds)
        {
            if (seconds > 0f) invulnerableUntil = Mathf.Max(invulnerableUntil, BattleClock.Now + seconds);
        }

        /// <summary>실제로 깎인 양을 돌려준다. ignoreInvulnerable은 시간 초과 반격 같은 명시적 예외용.</summary>
        public float TakeDamage(float amount, bool ignoreInvulnerable = false)
        {
            if (IsDead || amount <= 0f) return 0f;
            if (!ignoreInvulnerable)
            {
                if (ParryCheck != null && ParryCheck()) return 0f;
                if (IsInvulnerable) return 0f;
            }
            float applied = Mathf.Min(Current, amount);
            Current -= applied;
            Damaged?.Invoke(applied);
            return applied;
        }
    }
}
