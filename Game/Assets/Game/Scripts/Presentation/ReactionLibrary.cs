using System.Collections.Generic;
using SwordPrototype.Battle;

namespace SwordPrototype.Presentation
{
    /// <summary>성공 반응 5종(성1~성5). 적이 자연스럽게 멀어진다.</summary>
    public enum SuccessReaction { PushBack, SideStagger, KneeBuckle, Tumble, LeanBack }
    /// <summary>실패 반응 5종(실1~실5). 검 충돌·힘겨루기 후 분리.</summary>
    public enum FailReaction { Clash, Struggle, Deflect, BodyBlock, Sidestep }
    /// <summary>힘 3 치명타 추가 모션 3종(치1~치3).</summary>
    public enum CritMotion { DoubleCut, SpinThrust, ShoulderCut }

    /// <summary>S5 설계 1.0의 시간표·반응 선택 규칙(무작위 없음). 순수 로직이라 규칙 검사로 확인한다.</summary>
    public static class ReactionLibrary
    {
        public const float Windup = 0.08f;
        public const float Swing = 0.14f;
        public const float SpinSwing = 0.22f;       // 같은 방향 회전 베기(3타 ≤ 1.5초 유지)
        public const float StopHit = 0.06f;
        public const float StopCrit = 0.1f;         // 치명타·마무리
        public const float StopClash = 0.05f;
        public const float MidReaction = 0.15f;
        public const float MidPush = 0.5f;
        public const float FinalReaction = 0.25f;
        public const float TumbleReaction = 0.35f;
        public const float StruggleExtra = 0.4f;
        public const float CritExtra = 0.2f;
        public const float FailEndDistance = 3f;

        public static SuccessReaction ChooseSuccess(StrikeOutcome o)
        {
            if (o.critical || o.finisher || o.dashSlash || o.counter) return SuccessReaction.Tumble;
            if (o.knife) return SuccessReaction.PushBack;
            switch (o.direction)
            {
                case AttackDirection.Left:
                case AttackDirection.Right: return SuccessReaction.SideStagger;
                case AttackDirection.Down:
                case AttackDirection.DownLeft:
                case AttackDirection.DownRight: return SuccessReaction.KneeBuckle;
                case AttackDirection.Up:
                case AttackDirection.UpLeft:
                case AttackDirection.UpRight: return SuccessReaction.LeanBack;
                default: return SuccessReaction.PushBack;
            }
        }

        /// <summary>우선순위: 같은 방향 둘째 → 좌우 막힘 → 후면 → 체력 낮음(반동) → 기본 충돌.</summary>
        public static FailReaction ChooseFail(StrikeOutcome o)
        {
            if (o.sameDirectionSecond) return FailReaction.Deflect;
            if (o.sideBlocked) return FailReaction.Sidestep;
            if (o.fromBehind) return FailReaction.BodyBlock;
            if (o.damageToPlayer > 0f) return FailReaction.Struggle;
            return FailReaction.Clash;
        }

        public static CritMotion ChooseCrit(AttackDirection d)
        {
            switch (d)
            {
                case AttackDirection.Left:
                case AttackDirection.Right: return CritMotion.DoubleCut;
                case AttackDirection.Up:
                case AttackDirection.UpLeft:
                case AttackDirection.UpRight: return CritMotion.SpinThrust;
                default: return CritMotion.ShoulderCut;
            }
        }

        public static float EndDistance(SuccessReaction r)
            => r == SuccessReaction.Tumble ? 4.5f : r == SuccessReaction.KneeBuckle ? 3f : 3.5f;

        public static float FinalSeconds(StrikeOutcome o)
        {
            if (o.success) return ChooseSuccess(o) == SuccessReaction.Tumble ? TumbleReaction : FinalReaction;
            return FinalReaction + (ChooseFail(o) == FailReaction.Struggle ? StruggleExtra : 0f);
        }

        /// <summary>교환 총 시간 추정(연출과 같은 상수 사용). 마무리의 카메라 회전은 타격 시간과 겹친다.</summary>
        public static float EstimateSeconds(IReadOnlyList<StrikeOutcome> outcomes, AttackDirection startStance)
        {
            float total = 0f;
            AttackDirection stance = startStance;
            for (int i = 0; i < outcomes.Count; i++)
            {
                StrikeOutcome o = outcomes[i];
                total += Windup + (o.direction == stance ? SpinSwing : Swing);
                total += o.success ? (o.critical || o.finisher ? StopCrit : StopHit) : StopClash;
                if (o.success && o.critical) total += CritExtra;
                total += i == outcomes.Count - 1 ? FinalSeconds(o) : MidReaction;
                stance = o.direction;
            }
            return total;
        }
    }
}
