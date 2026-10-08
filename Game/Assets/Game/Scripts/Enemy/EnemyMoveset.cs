using System;
using UnityEngine;

namespace SwordPrototype.Enemy
{
    public enum EnemyAttackKind { Sweep, ChargeSlash, Throw, LeapSlam, RetreatShot, SpinSlash, Kick, ComboAssault, Thrust, FanThrow }

    /// <summary>적 공격 1종의 데이터. 값은 S4 설계 1.0(사용자 확인), R8에서 예고·후딜 ×0.7.</summary>
    [Serializable]
    public sealed class EnemyAttackDefinition
    {
        public EnemyAttackKind kind;
        public string displayName;
        public float minDistance;
        public float maxDistance;
        public float telegraphSeconds;
        public float recoverySeconds;
        public float damage;
        public AttackShape shape;
        public float travel;   // 돌진 거리 / 후퇴 거리 / 밀쳐내는 거리
        public bool groundOnly; // S6: 착지 상태에만 맞음(점프로 회피 가능)

        public EnemyAttackDefinition(EnemyAttackKind kind, string name, float min, float max, float telegraph, float recovery, float damage, AttackShape shape, float travel = 0f, bool groundOnly = false)
        {
            this.kind = kind; displayName = name; minDistance = min; maxDistance = max;
            telegraphSeconds = telegraph; recoverySeconds = recovery; this.damage = damage; this.shape = shape; this.travel = travel;
            this.groundOnly = groundOnly;
        }
    }

    /// <summary>거리 구간 하나의 기술 확률(가중치). R8: 거리로 하나만 정하던 방식을 확률표로.</summary>
    [Serializable]
    public sealed class EnemySkillBand
    {
        public float maxDistance;
        public EnemyAttackKind[] kinds;
        public float[] weights;
        public EnemySkillBand(float max, EnemyAttackKind[] kinds, float[] weights) { maxDistance = max; this.kinds = kinds; this.weights = weights; }
    }

    /// <summary>적 패턴 데이터와 선택 규칙. 순수 로직이라 규칙 검사에서 직접 확인한다(난수는 호출자가 0~1로 넘김).</summary>
    [Serializable]
    public sealed class EnemyMoveset
    {
        public const float TimeScale = 0.7f;        // R8: 예고·후딜 ×0.7(사용자 "둘 다 0.7배")

        public float moveSpeed = 6f;
        public float turnDegreesPerSecond = 360f;
        public float arenaRadius = 24f;
        public float attackGap = 0.3f;              // 공격 사이 최소 간격
        public float lockBeforeHit = 0.15f;         // 예고 마지막 고정 구간(0.2 → 예고가 짧아져 0.15)
        public float hitStunSeconds = 0.4f;         // 플레이어 공격 성공 시 경직
        public float parryRecoverySeconds = 0.3f;   // 무방비 해제 후 후딜
        public float minResumeTelegraph = 0.3f;     // 전부 막힌 뒤 남은 예고 최소
        public float retreatTriggerDistance = 3.5f;
        [Range(0f, 1f)] public float retreatChance = 0.4f;   // R8: 큰 공격 뒤 근접이면 이 확률로 후퇴 사격
        public float projectileSpeed = 18f;
        public float projectileRadius = 0.4f;
        public float fanSpreadDegrees = 15f;        // R8 부채 투척 좌우 각도
        public float chargeSeconds = 0.35f;
        public float thrustSeconds = 0.22f;         // R8 찌르기 돌진(더 빠름)
        public float leapHeight = 3f;
        public float turnCounterDelay = 0.15f;      // R8 턴 교대: 플레이어 교환 뒤 반격까지

        private static float T(float seconds) => seconds * TimeScale;

        public EnemyAttackDefinition[] attacks =
        {
            new EnemyAttackDefinition(EnemyAttackKind.Sweep, "큰 검 휘두르기", 0f, 3.5f, T(0.5f), T(0.6f), 15f, AttackShape.Fan(3.5f, 140f), 0f, true),
            new EnemyAttackDefinition(EnemyAttackKind.ChargeSlash, "돌진 베기", 3.5f, 8f, T(0.6f), T(0.8f), 18f, AttackShape.Line(1.5f, 8f), 8f),
            new EnemyAttackDefinition(EnemyAttackKind.Throw, "투척", 8f, 15f, T(0.5f), T(0.5f), 8f, AttackShape.Line(0.8f, 15f)),
            new EnemyAttackDefinition(EnemyAttackKind.LeapSlam, "도약 내려찍기", 15f, float.MaxValue, T(0.9f), T(1.0f), 25f, AttackShape.Circle(4f), 0f, true),
            new EnemyAttackDefinition(EnemyAttackKind.RetreatShot, "후퇴 사격", 0f, 0f, T(0.4f), T(0.4f), 8f, AttackShape.Line(0.8f, 12f), 4f),
            // R8 새 기술
            new EnemyAttackDefinition(EnemyAttackKind.SpinSlash, "회전 베기", 0f, 3.5f, T(0.6f), T(0.7f), 14f, AttackShape.Circle(3.2f), 0f, true),
            new EnemyAttackDefinition(EnemyAttackKind.Kick, "밀쳐 차기", 0f, 2.5f, T(0.4f), T(0.5f), 6f, AttackShape.Fan(2.4f, 100f), 2f),
            new EnemyAttackDefinition(EnemyAttackKind.ComboAssault, "합 공격", 0f, 6.5f, T(0.5f), T(0.7f), 8f, AttackShape.Fan(6.5f, 60f)),
            new EnemyAttackDefinition(EnemyAttackKind.Thrust, "찌르기 돌진", 3f, 8f, T(0.45f), T(0.7f), 16f, AttackShape.Line(1.0f, 7f), 7f),
            new EnemyAttackDefinition(EnemyAttackKind.FanThrow, "부채 투척", 6f, float.MaxValue, T(0.55f), T(0.55f), 7f, AttackShape.Fan(15f, 34f)),
        };

        /// <summary>R8 거리별 확률표(사용자 확인). 직전 기술은 확률 절반, 같은 기술 3연속 금지.</summary>
        public EnemySkillBand[] bands =
        {
            new EnemySkillBand(3f, new[] { EnemyAttackKind.Sweep, EnemyAttackKind.SpinSlash, EnemyAttackKind.Kick, EnemyAttackKind.LeapSlam, EnemyAttackKind.ComboAssault }, new[] { 35f, 20f, 15f, 15f, 15f }),
            new EnemySkillBand(6f, new[] { EnemyAttackKind.ComboAssault, EnemyAttackKind.ChargeSlash, EnemyAttackKind.Thrust, EnemyAttackKind.LeapSlam }, new[] { 40f, 20f, 20f, 20f }),
            new EnemySkillBand(15f, new[] { EnemyAttackKind.Throw, EnemyAttackKind.FanThrow, EnemyAttackKind.ChargeSlash, EnemyAttackKind.LeapSlam }, new[] { 30f, 20f, 20f, 30f }),
            new EnemySkillBand(float.MaxValue, new[] { EnemyAttackKind.LeapSlam, EnemyAttackKind.FanThrow }, new[] { 60f, 40f }),
        };

        public EnemyAttackDefinition Get(EnemyAttackKind kind)
        {
            foreach (var a in attacks) if (a.kind == kind) return a;
            return attacks[0];
        }

        public EnemySkillBand Band(float distance)
        {
            foreach (var b in bands) if (distance < b.maxDistance) return b;
            return bands[bands.Length - 1];
        }

        /// <summary>
        /// 1) 직전이 큰 근접 공격이고 플레이어가 3.5m 안이면 retreatChance 확률로 후퇴 사격
        /// 2) 거리 구간 확률표에서 고름 — 직전 기술 가중치 절반, 2연속이면 0(3연속 금지)
        /// roll·roll2는 0~1 난수(검사에서 고정값을 넣는다).
        /// </summary>
        public EnemyAttackKind Choose(float distance, bool hasLast, EnemyAttackKind last, int lastStreak, float roll, float roll2 = 1f)
        {
            bool afterBig = hasLast && (last == EnemyAttackKind.Sweep || last == EnemyAttackKind.ChargeSlash || last == EnemyAttackKind.LeapSlam || last == EnemyAttackKind.SpinSlash);
            if (afterBig && distance <= retreatTriggerDistance && roll2 < retreatChance) return EnemyAttackKind.RetreatShot;
            EnemySkillBand band = Band(distance);
            float total = 0f;
            var w = new float[band.kinds.Length];
            for (int i = 0; i < w.Length; i++)
            {
                w[i] = band.weights[i];
                if (hasLast && band.kinds[i] == last) w[i] = lastStreak >= 2 ? 0f : w[i] * 0.5f;
                total += w[i];
            }
            if (total <= 0f) return band.kinds[0];
            float pick = Mathf.Clamp01(roll) * total;
            for (int i = 0; i < w.Length; i++)
            {
                if (pick < w[i] || i == w.Length - 1 && w[i] > 0f) return band.kinds[i];
                pick -= w[i];
            }
            for (int i = w.Length - 1; i >= 0; i--) if (w[i] > 0f) return band.kinds[i];
            return band.kinds[0];
        }
    }
}
