using System;
using System.Collections.Generic;

namespace SwordPrototype.Battle
{
    /// <summary>판정이 끝난 타격 1개. 연출은 이 값을 그대로 보여주기만 한다.</summary>
    public struct StrikeOutcome
    {
        public AttackDirection direction;
        public bool knife;
        public bool dashSlash;
        public bool finisher;
        public bool success;
        public bool critical;
        public float chance;
        public float damageToEnemy;
        public float damageToPlayer;
        // 아래는 연출 반응 선택용 기록(S5). 판정에는 쓰지 않는다.
        public bool sameDirectionSecond;
        public bool fromBehind;
        public bool sideBlocked;
        public bool counter;
    }

    /// <summary>예약된 계획을 내부 추첨으로 결과 목록으로 바꾼다. 추첨 과정은 화면에 노출하지 않는다.</summary>
    public static class CombatResolver
    {
        /// <summary>선택 화면 표시용 예상 성공률. 실제 판정과 같은 경로를 쓴다.</summary>
        /// <param name="startStance">R5: 선택 시작 때 검 자세. null이면 자세 보정 없음(규칙 검사용).</param>
        public static float PreviewChance(IReadOnlyList<AttackDirection> reserved, AttackDirection candidate, CharacterStats s, StatTuning t,
            bool inFront, SideBiasTracker bias, float knifePenalty, bool airborne = false, AttackDirection? startStance = null, float adaptation = 0f)
        {
            int repeat = ComboRules.RepeatCount(reserved, candidate);
            if (repeat >= 3) return 1f;   // 특수기 3 마무리는 무조건 성공(9차 답변)
            var context = new StrikeContext
            {
                attackerInFront = inFront,
                sameDirectionSecond = repeat >= 2,
                sideBiasPenalty = bias.FailPenalty(candidate.Side(), t),
                knifePenalty = knifePenalty,
                airDownStrike = airborne && IsDownward(candidate),
                stanceBonus = startStance.HasValue ? StatEffects.StanceBonus(SwingStart(reserved, startStance.Value), candidate, t) : 0f,
                adaptPenalty = adaptation
            };
            return StatEffects.SuccessChance(s, t, context);
        }

        /// <summary>이 타격의 검 시작 자세: 앞 예약이 있으면 그 방향, 없으면 선택 시작 때 자세.</summary>
        public static AttackDirection SwingStart(IReadOnlyList<AttackDirection> reserved, AttackDirection startStance)
            => reserved.Count > 0 ? reserved[reserved.Count - 1] : startStance;

        /// <summary>공중 베기 보너스 대상(↓ ↙ ↘).</summary>
        public static bool IsDownward(AttackDirection d)
            => d == AttackDirection.Down || d == AttackDirection.DownLeft || d == AttackDirection.DownRight;

        /// <param name="guaranteed">패링 반격처럼 모든 타격이 무조건 성공하는 경우.</param>
        /// <param name="airborne">S6: 공중에서 들어간 선택(↓↙↘ 보너스).</param>
        public static List<StrikeOutcome> Resolve(IReadOnlyList<AttackDirection> plan, CharacterStats s, StatTuning t,
            bool inFront, SideBiasTracker bias, float knifePenalty, Func<float> random, bool guaranteed = false, bool airborne = false, AttackDirection? startStance = null, float adaptation = 0f)
        {
            var outcomes = new List<StrikeOutcome>(plan.Count);
            var prefix = new List<AttackDirection>(plan.Count);
            foreach (AttackDirection direction in plan)
            {
                int repeat = ComboRules.RepeatCount(prefix, direction);
                var outcome = new StrikeOutcome
                {
                    direction = direction,
                    finisher = repeat >= 3,
                    chance = guaranteed ? 1f : PreviewChance(prefix, direction, s, t, inFront, bias, knifePenalty, airborne, startStance, adaptation),
                    sameDirectionSecond = repeat == 2,
                    fromBehind = !inFront,
                    sideBlocked = bias.FailPenalty(direction.Side(), t) > 0f,
                    counter = guaranteed
                };
                // 확정 성공(마무리·반격)은 난수와 무관하게 성공. Random.value가 1을 반환하는 경우도 막는다.
                outcome.success = outcome.chance >= 1f || random() < outcome.chance;
                if (outcome.success)
                {
                    outcome.damageToEnemy = outcome.finisher
                        ? StatEffects.SpecialDamage(s, t, t.finisherDamageScale)
                        : StatEffects.HitDamage(s, t, repeat >= 2, random(), out outcome.critical);
                }
                else StatEffects.FailFeedback(s, t, out outcome.damageToPlayer, out outcome.damageToEnemy);
                outcomes.Add(outcome);
                prefix.Add(direction);
            }
            RecordSides(outcomes, bias);
            return outcomes;
        }

        /// <summary>특수기 2 지나가며 베기: 예약 없이 바로, 무조건 성공(9차 답변).</summary>
        public static StrikeOutcome ResolveDashSlash(CharacterStats s, StatTuning t)
            => new StrikeOutcome
            {
                dashSlash = true,
                success = true,
                chance = 1f,
                damageToEnemy = StatEffects.SpecialDamage(s, t, t.dashSlashDamageScale)
            };

        public static StrikeOutcome ResolveKnife(ThrowingKnifeState knife, CharacterStats s, StatTuning t, Func<float> random)
        {
            var outcome = new StrikeOutcome { knife = true, chance = 1f - knife.FailChance };
            outcome.success = knife.Throw(random(), t);
            if (outcome.success) outcome.damageToEnemy = StatEffects.KnifeDamage(s, t);
            return outcome;
        }

        /// <summary>좌우 묶음별로 사용 여부와 성공 여부(그쪽 타격 중 하나라도 성공)를 기록한다.</summary>
        private static void RecordSides(List<StrikeOutcome> outcomes, SideBiasTracker bias)
        {
            foreach (AttackSide side in new[] { AttackSide.Left, AttackSide.Right })
            {
                bool used = false, succeeded = false;
                foreach (StrikeOutcome o in outcomes)
                {
                    if (o.direction.Side() != side) continue;
                    used = true;
                    succeeded |= o.success;
                }
                bias.RecordCombo(side, used, succeeded);
            }
        }
    }
}
