using System.Collections.Generic;
using UnityEngine;

namespace SwordPrototype.Battle
{
    public enum ReserveResult { Accepted, AcceptedAsFinisher, ComboFull, SameDirectionLimit, FinisherUnavailable, VerticalRepeat }

    /// <summary>선택 1회의 예약 규칙: 속도별 타수와 같은 방향 제한.</summary>
    public static class ComboRules
    {
        public static readonly Color FirstColor = new Color(1f, 0.85f, 0.2f);   // 1회: 노랑
        public static readonly Color SecondColor = new Color(0.3f, 0.6f, 1f);   // 2회: 파랑
        public static readonly Color ThirdColor = new Color(1f, 0.25f, 0.2f);   // 3회: 빨강

        /// <summary>속도 0~1: 1타, 2: 2타, 3: 3타.</summary>
        public static int MaxHits(int speed) => speed <= 1 ? 1 : Mathf.Min(speed, 3);

        /// <summary>candidate를 다음에 예약하면 같은 방향이 몇 번 연속되는지.</summary>
        public static int RepeatCount(IReadOnlyList<AttackDirection> reserved, AttackDirection candidate)
        {
            int count = 1;
            for (int i = reserved.Count - 1; i >= 0 && reserved[i] == candidate; i--) count++;
            return count;
        }

        public static Color RepeatColor(int repeatCount)
            => repeatCount >= 3 ? ThirdColor : repeatCount == 2 ? SecondColor : FirstColor;

        /// <summary>
        /// 같은 방향 3번째는 특수기 3 마무리이며 쿨타임이 준비됐을 때만 허용한다.
        /// 그 외에는 UI에 불가를 표시한다.
        /// </summary>
        public static ReserveResult CanReserve(IReadOnlyList<AttackDirection> reserved, AttackDirection candidate, CharacterStats stats, bool finisherReady)
        {
            if (reserved.Count >= MaxHits(stats.Speed)) return ReserveResult.ComboFull;
            // R8: ↑↑ · ↓↓ 연속 예약은 막는다(위아래는 서로 가로지르는 크게 베기만)
            if (reserved.Count > 0 && reserved[reserved.Count - 1] == candidate && IsVertical(candidate)) return ReserveResult.VerticalRepeat;
            int repeat = RepeatCount(reserved, candidate);
            if (repeat <= 2) return ReserveResult.Accepted;
            if (repeat == 3 && StatEffects.HasFinisher(stats))
                return finisherReady ? ReserveResult.AcceptedAsFinisher : ReserveResult.FinisherUnavailable;
            return repeat == 3 ? ReserveResult.FinisherUnavailable : ReserveResult.SameDirectionLimit;
        }

        public static bool IsVertical(AttackDirection d) => d == AttackDirection.Up || d == AttackDirection.Down;

        /// <summary>
        /// R8: 회전 베기 조건 — 같은 방향이거나, 같은 쪽 묶음(↖←↙ / ↗→↘)끼리 이어 벨 때.
        /// 반대쪽·위아래는 칼이 몸을 가로지르는 베기.
        /// </summary>
        public static bool IsSpin(AttackDirection from, AttackDirection to)
            => from == to || (to.Side() != AttackSide.None && to.Side() == from.Side());

        public static bool IsAccepted(ReserveResult result)
            => result == ReserveResult.Accepted || result == ReserveResult.AcceptedAsFinisher;
    }
}
