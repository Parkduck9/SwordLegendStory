using UnityEngine;

namespace SwordPrototype.Battle
{
    /// <summary>한 타격의 성공률 계산 입력. 패널티는 실패 확률 가산값(0~1).</summary>
    public struct StrikeContext
    {
        public bool attackerInFront;      // 플레이어가 적 정면 180° 안에 있음
        public bool sameDirectionSecond;  // 같은 방향 연속 둘째 타. 특수기 3 마무리(셋째 타)도 같은 실패 확률을 쓰도록 true로 넘긴다.
        public float sideBiasPenalty;     // SideBiasTracker 값
        public float knifePenalty;        // ThrowingKnifeState 값, 다음 선택 1회만
        public bool airDownStrike;        // S6: 공중에서 ↓↙↘ 내려 베기
        public float stanceBonus;         // R5: 검 자세(시작→목표) 보정, StatEffects.StanceBonus 값
        public float adaptPenalty;        // R8: 적 적응(EnemyAdaptation 값, 선택 시작 때 고정)
    }

    /// <summary>스탯 레벨을 게임 수치로 바꾼다. 난수 없는 순수 함수.</summary>
    public static class StatEffects
    {
        public static float MaxHealth(CharacterStats s, StatTuning t) => StatTuning.At(t.maxHealth, s.Health);
        public static float MoveSpeed(CharacterStats s, StatTuning t) => StatTuning.At(t.moveSpeed, s.Speed);
        public static float DashCooldown(CharacterStats s, StatTuning t) => StatTuning.At(t.dashCooldown, s.Speed);
        public static float SelectionSeconds(CharacterStats s, StatTuning t) => StatTuning.At(t.selectionSeconds, s.Speed);
        public static float SkillCooldown(CharacterStats s, StatTuning t, float baseSeconds) => baseSeconds * StatTuning.At(t.skillCooldownScale, s.Stamina);

        // 특수기 단계는 누적된다: 3은 1·2 효과도 가진다.
        public static float DodgeInvulnerability(CharacterStats s, StatTuning t) => s.Special >= 1 ? t.dodgeInvulnerability : 0f;
        public static bool HasDashSlash(CharacterStats s) => s.Special >= 2;
        public static bool HasFinisher(CharacterStats s) => s.Special >= 3;
        public static bool HasThrowingKnife(CharacterStats s) => s.Stamina >= 3;
        public static bool HasGuardParry(CharacterStats s) => s.Health >= 3;
        public static bool HasCritical(CharacterStats s) => s.Strength >= 3;
        public static bool IsLowHealthStat(CharacterStats s) => s.Health <= 1;

        /// <summary>정면/후면은 적이 바라보는 방향 기준 플레이어 위치로 판정한다.</summary>
        public static bool IsInFront(Vector3 enemyPosition, Vector3 enemyForward, Vector3 playerPosition)
        {
            Vector3 toPlayer = Vector3.ProjectOnPlane(playerPosition - enemyPosition, Vector3.up);
            return Vector3.Dot(Vector3.ProjectOnPlane(enemyForward, Vector3.up), toPlayer) >= 0f;
        }

        public static float SuccessChance(CharacterStats s, StatTuning t, StrikeContext c)
        {
            float chance = t.baseSuccess;
            if (s.Speed == 0) chance -= t.speed0SuccessPenalty;
            if (c.attackerInFront)
            {
                if (s.Health == 0) chance += t.frontSuccessHealth0;
                else if (s.Health >= 2) chance += t.frontSuccessHealthHigh;
            }
            else chance += StatTuning.At(t.backSuccessBySpeed, s.Speed);
            if (c.sameDirectionSecond) chance -= t.repeatFailBonus;
            if (c.airDownStrike) chance += t.airStrikeDownBonus;
            chance -= c.sideBiasPenalty + c.knifePenalty;
            chance += c.stanceBonus - c.adaptPenalty;
            return Mathf.Clamp(chance, t.minSuccess, t.maxSuccess);
        }

        /// <summary>시작 자세에서 목표 방향까지 8방향 칸 수(0~4). 4 = 반대편으로 크게.</summary>
        public static int SwingSteps(AttackDirection start, AttackDirection target)
        {
            int d = Mathf.Abs((int)start - (int)target) % 8;
            return Mathf.Min(d, 8 - d);
        }

        /// <summary>
        /// R5 검 자세 보정 — 바꾸는 지점은 여기 하나. 상성표(64칸)가 채워져 있으면 그것을, 아니면 칸 수 표를 쓴다.
        /// </summary>
        public static float StanceBonus(AttackDirection start, AttackDirection target, StatTuning t)
        {
            if (t.stanceMatrix != null && t.stanceMatrix.Length == 64) return t.stanceMatrix[(int)start * 8 + (int)target];
            return t.stanceSwingBonus != null && t.stanceSwingBonus.Length > 0 ? StatTuning.At(t.stanceSwingBonus, SwingSteps(start, target)) : 0f;
        }

        /// <summary>일반 타격 성공 피해. critRoll은 판정기가 넘기는 0~1 난수.</summary>
        public static float HitDamage(CharacterStats s, StatTuning t, bool sameDirectionSecond, float critRoll, out bool critical)
        {
            float damage = t.baseDamage + (sameDirectionSecond ? t.repeatDamageBonus : 0f);
            damage *= StatTuning.At(t.strengthDamageScale, s.Strength);
            if (HasCritical(s)) damage += t.strength3FlatBonus;
            critical = HasCritical(s) && critRoll < t.critChance;
            if (critical) damage *= t.critMultiplier;
            return damage * HealthDamageScale(s, t);
        }

        /// <summary>기술(지나가며 베기·마무리·비도) 피해. scale은 baseDamage 대비 배율.</summary>
        public static float SpecialDamage(CharacterStats s, StatTuning t, float scale)
            => t.baseDamage * scale * StatTuning.At(t.strengthSpecialScale, s.Strength) * HealthDamageScale(s, t);

        public static float KnifeDamage(CharacterStats s, StatTuning t) => SpecialDamage(s, t, t.knifeDamage / t.baseDamage);

        /// <summary>타격 실패(적 방어/패링) 시 발생하는 작은 피해.</summary>
        public static void FailFeedback(CharacterStats s, StatTuning t, out float damageToPlayer, out float damageToEnemy)
        {
            bool low = IsLowHealthStat(s);
            damageToPlayer = low ? t.failSelfDamage : 0f;
            damageToEnemy = low ? 0f : t.failEnemyDamage;
        }

        private static float HealthDamageScale(CharacterStats s, StatTuning t) => s.Health == 0 ? t.health0DamageScale : 1f;
    }
}
