using System.Collections.Generic;
using UnityEngine;

namespace SwordPrototype.Battle
{
    /// <summary>
    /// 플레이어 검 자세(8방향, 캐릭터 기준 화면 평면). 판정(R5 각도 보정)과 연출이 함께 읽는다.
    /// - 공격 방향 D로 베면 끝자세 = D. 다음 공격이 같은 방향이면 몸 전체 회전 베기(R18).
    /// - R5: 실시간에서 일정 시간 계속 움직이면 이동 반대쪽으로 끌린 자세, 공중이면 위로 든 자세, 대쉬하면 즉시 대쉬 반대쪽.
    /// - 멈춘 채 일정 시간 지나면 기본 자세.
    /// 매핑·시간은 모두 StatTuning(Sword stance) 값이라 코드 수정 없이 바꿀 수 있다.
    /// </summary>
    public sealed class SwordStance
    {
        public const AttackDirection Rest = AttackDirection.DownRight;   // StatTuning.restStance 기본값과 같음
        /// <summary>호환용: 기본 복귀 시간(StatTuning.stanceIdleResetSeconds 기본값).</summary>
        public const float IdleResetSeconds = 2f;

        private float idle;
        private float moveHold;
        private AttackDirection? pendingMove;

        public AttackDirection Current { get; private set; } = Rest;

        /// <summary>R8: 같은 방향 또는 같은 쪽 묶음이면 회전 베기.</summary>
        public bool IsSpin(AttackDirection next) => ComboRules.IsSpin(Current, next);

        public void Commit(AttackDirection direction)
        {
            Current = direction;
            idle = 0f;
            moveHold = 0f;
            pendingMove = null;
        }

        /// <summary>실시간 상태에서만 호출(이동 없음, 기본 설정). 기본 자세로 돌아가면 true.</summary>
        public bool TickRealtime(float deltaTime) => TickRealtime(deltaTime, Vector2.zero, false, null);

        /// <summary>
        /// 실시간 상태에서만 호출. localMove는 캐릭터 기준 이동(x 오른쪽, y 앞), 없으면 0.
        /// 자세가 바뀌면 true(연출이 검을 옮긴다).
        /// </summary>
        public bool TickRealtime(float deltaTime, Vector2 localMove, bool airborne, StatTuning t)
        {
            AttackDirection rest = t != null ? t.restStance : Rest;
            float hold = t != null ? t.stanceMoveHoldSeconds : 0.25f;
            float reset = t != null ? t.stanceIdleResetSeconds : IdleResetSeconds;

            if (airborne && t != null)
            {
                idle = 0f; moveHold = 0f; pendingMove = null;
                return Set(t.airStance);
            }
            if (localMove.sqrMagnitude > 0.01f && t != null)
            {
                idle = 0f;
                AttackDirection move = SelectionSession.FromVector(localMove);
                if (pendingMove == move) moveHold += deltaTime;
                else { pendingMove = move; moveHold = deltaTime; }
                return moveHold >= hold && Set(MoveStance(move, t));
            }
            moveHold = 0f; pendingMove = null;
            if (Current == rest) { idle = 0f; return false; }
            idle += deltaTime;
            if (idle < reset) return false;
            idle = 0f;
            return Set(rest);
        }

        /// <summary>대쉬 시작: 대쉬 방향(캐릭터 기준)의 반대쪽 자세로 즉시.</summary>
        public bool ApplyDash(Vector2 localDirection, StatTuning t)
        {
            if (t == null || localDirection.sqrMagnitude < 0.01f) return false;
            idle = 0f; moveHold = 0f; pendingMove = null;
            return Set(MoveStance(SelectionSession.FromVector(localDirection), t));
        }

        /// <summary>이동 방향(앞=Up 기준 8방향) → 검 자세. 표가 비었으면 반대 방향.</summary>
        public static AttackDirection MoveStance(AttackDirection move, StatTuning t)
            => t != null && t.moveStance != null && t.moveStance.Length == 8 ? t.moveStance[(int)move] : (AttackDirection)(((int)move + 4) % 8);

        private bool Set(AttackDirection direction)
        {
            if (Current == direction) return false;
            Current = direction;
            return true;
        }

        /// <summary>선택 화면 표시용: 지금까지 예약한 것을 반영한 끝자세.</summary>
        public AttackDirection Predict(IReadOnlyList<AttackDirection> reserved)
            => reserved.Count > 0 ? reserved[reserved.Count - 1] : Current;
    }
}
