using System.Collections.Generic;
using UnityEngine;

namespace SwordPrototype.Battle
{
    public enum SelectionEnd { None, Completed, TimedOut, KnifeThrown, DashSlash }

    /// <summary>
    /// 슬로모션 선택 1회. 마우스 이동량으로 가상 커서를 움직여 8방향/가운데 원을 고르고,
    /// 좌클릭으로 예약한다. 시간은 호출자가 실제 경과시간으로 넘긴다.
    /// </summary>
    public sealed class SelectionSession
    {
        public const float CenterRadius = 0.3f;
        private const float Sensitivity = 0.08f;

        private readonly List<AttackDirection> reserved = new List<AttackDirection>();
        private readonly CharacterStats stats;
        private readonly bool finisherReady;
        private readonly bool knifeAvailable;
        private readonly bool dashSlashReady;

        /// <param name="counter">패링 후 반격: 속도와 상관없이 1타, 비도·베기·마무리 불가, 무조건 성공.</param>
        public SelectionSession(CharacterStats stats, StatTuning tuning, bool finisherReady, bool dashSlashReady = false, bool counter = false)
        {
            this.stats = stats;
            Counter = counter;
            this.finisherReady = finisherReady && !counter;
            this.dashSlashReady = dashSlashReady && !counter && StatEffects.HasDashSlash(stats);
            knifeAvailable = !counter && StatEffects.HasThrowingKnife(stats);
            Duration = StatEffects.SelectionSeconds(stats, tuning);
            TimeLeft = Duration;
            MaxHits = counter ? 1 : ComboRules.MaxHits(stats.Speed);
        }

        /// <summary>패링 반격 선택인지. 반격은 무조건 성공.</summary>
        public bool Counter { get; }

        public IReadOnlyList<AttackDirection> Reserved => reserved;
        public Vector2 Cursor { get; private set; }
        public float Duration { get; }
        public float TimeLeft { get; private set; }
        public int MaxHits { get; }
        public SelectionEnd End { get; private set; }
        public ReserveResult LastRejection { get; private set; } = ReserveResult.Accepted;
        public bool AtCenter => Cursor.magnitude < CenterRadius;
        public AttackDirection Hovered => FromVector(Cursor);
        /// <summary>비도는 아직 아무것도 예약하지 않았을 때만 던질 수 있다(던지면 선택 종료).</summary>
        public bool KnifeSelectable => knifeAvailable && reserved.Count == 0;

        /// <summary>특수기 2를 쓸 수 있는지(특수기 2 이상 + 쿨타임 준비 + 아직 예약 없음).</summary>
        public bool DashSlashSelectable => dashSlashReady && reserved.Count == 0;
        /// <summary>특수기 2를 가졌지만 예약 때문에 지금은 못 쓰는 경우(HUD 안내용).</summary>
        public bool DashSlashBlockedByReservation => dashSlashReady && reserved.Count > 0;

        /// <summary>우클릭: 예약 없이 바로 특수기 2 지나가며 베기로 선택 종료(9차 답변).</summary>
        public void RequestDashSlash()
        {
            if (End == SelectionEnd.None && DashSlashSelectable) End = SelectionEnd.DashSlash;
        }

        public void MoveCursor(Vector2 mouseDelta) => MoveCursor(mouseDelta, Sensitivity);

        /// <summary>S7: 설정의 선택 커서 감도를 적용.</summary>
        public void MoveCursor(Vector2 mouseDelta, float sensitivity)
        {
            if (End == SelectionEnd.None) Cursor = Vector2.ClampMagnitude(Cursor + mouseDelta * sensitivity, 1f);
        }

        public void SetCursor(Vector2 value) => Cursor = Vector2.ClampMagnitude(value, 1f);

        public void Tick(float realDeltaTime)
        {
            if (End != SelectionEnd.None) return;
            TimeLeft -= realDeltaTime;
            if (TimeLeft <= 0f) { TimeLeft = 0f; End = SelectionEnd.TimedOut; }
        }

        public ReserveResult Evaluate(AttackDirection direction)
            => ComboRules.CanReserve(reserved, direction, stats, finisherReady);

        public void Click()
        {
            if (End != SelectionEnd.None) return;
            if (AtCenter)
            {
                if (KnifeSelectable) End = SelectionEnd.KnifeThrown;
                return;
            }
            ReserveResult result = Evaluate(Hovered);
            LastRejection = result;
            if (!ComboRules.IsAccepted(result)) return;
            reserved.Add(Hovered);
            if (reserved.Count >= MaxHits) End = SelectionEnd.Completed;
        }

        /// <summary>화면 기준 벡터(위 = +y)를 8방향으로. 시계 방향 순서가 enum 순서와 같다.</summary>
        public static AttackDirection FromVector(Vector2 v)
        {
            float angle = Mathf.Atan2(v.x, v.y) * Mathf.Rad2Deg;
            int index = Mathf.RoundToInt(angle / 45f);
            return (AttackDirection)(((index % 8) + 8) % 8);
        }

        public static Vector2 ToVector(AttackDirection direction)
        {
            float rad = (int)direction * 45f * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
        }
    }
}
