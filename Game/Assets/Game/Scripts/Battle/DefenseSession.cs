using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwordPrototype.Battle
{
    /// <summary>
    /// R8 합 공격 방어 1회. 휠 8칸 중 3칸이 순서대로 잠깐 붉게 깜빡인 뒤(보여주기), 같은 순서로 커서+좌클릭(입력).
    /// 칸마다 맞히면 막음, 틀리거나 시간이 끝나면 그 타는 맞음. 속도 스탯이 높을수록 깜빡임·입력 시간이 약간 길다.
    /// 시간은 호출자가 실제 경과시간으로 넘긴다. 난수는 호출자가 0~1로 넘긴다.
    /// </summary>
    public sealed class DefenseSession
    {
        public const int Hits = 3;
        private readonly List<bool> results = new List<bool>(Hits);
        private float inputLeft;

        public DefenseSession(CharacterStats stats, StatTuning t, Func<float> random)
        {
            FlashSeconds = StatTuning.At(t.defenseFlashSeconds, stats.Speed);
            GapSeconds = t.defenseGapSeconds;
            InputSeconds = StatTuning.At(t.defenseInputSeconds, stats.Speed);
            Pattern = MakePattern(random);
            inputLeft = InputSeconds;
        }

        public AttackDirection[] Pattern { get; }
        public float FlashSeconds { get; }
        public float GapSeconds { get; }
        public float InputSeconds { get; }
        public float ShowSeconds => Hits * (FlashSeconds + GapSeconds);
        public float Elapsed { get; private set; }
        public bool InInput => Elapsed >= ShowSeconds;
        public float InputLeft => inputLeft;
        public bool Done { get; private set; }
        public IReadOnlyList<bool> Results => results;
        public bool AllBlocked => results.Count == Hits && results.TrueForAll(r => r);
        public Vector2 Cursor { get; private set; }
        public bool AtCenter => Cursor.magnitude < SelectionSession.CenterRadius;
        public AttackDirection Hovered => SelectionSession.FromVector(Cursor);

        /// <summary>보여주기 중 지금 붉게 켜진 칸(없으면 -1).</summary>
        public int LitIndex
        {
            get
            {
                if (InInput) return -1;
                int i = Mathf.FloorToInt(Elapsed / (FlashSeconds + GapSeconds));
                float within = Elapsed - i * (FlashSeconds + GapSeconds);
                return i < Hits && within < FlashSeconds ? i : -1;
            }
        }

        /// <summary>3칸, 바로 앞과 같은 칸은 나오지 않는다.</summary>
        public static AttackDirection[] MakePattern(Func<float> random)
        {
            var p = new AttackDirection[Hits];
            int prev = Mathf.Clamp((int)(random() * 8f), 0, 7);
            p[0] = (AttackDirection)prev;
            for (int i = 1; i < Hits; i++)
            {
                prev = (prev + 1 + Mathf.Clamp((int)(random() * 7f), 0, 6)) % 8;
                p[i] = (AttackDirection)prev;
            }
            return p;
        }

        public void MoveCursor(Vector2 delta, float sensitivity)
        {
            if (!Done) Cursor = Vector2.ClampMagnitude(Cursor + delta * sensitivity, 1f);
        }

        public void SetCursor(Vector2 value) => Cursor = Vector2.ClampMagnitude(value, 1f);

        public void Tick(float realDelta)
        {
            if (Done) return;
            Elapsed += realDelta;
            if (!InInput) return;
            inputLeft -= realDelta;
            if (inputLeft <= 0f) Finish();
        }

        /// <summary>입력 중 좌클릭: 다음 칸을 맞혔는지 기록. 보여주기 중·가운데 클릭은 무시.</summary>
        public void Click()
        {
            if (Done || !InInput || AtCenter) return;
            results.Add(Hovered == Pattern[results.Count]);
            if (results.Count >= Hits) Done = true;
        }

        // 시간 초과: 남은 타는 모두 맞음
        private void Finish()
        {
            while (results.Count < Hits) results.Add(false);
            Done = true;
        }
    }

    /// <summary>
    /// R8 적 적응: 플레이어 공격이 성공할수록 다음 공격 실패 확률이 오르고, 시간이 지나면 원래대로.
    /// 합 공격 방어 보상 반격은 덜 오른다.
    /// </summary>
    public sealed class EnemyAdaptation
    {
        public float Value { get; private set; }
        public void Add(float amount, StatTuning t) => Value = Mathf.Clamp(Value + amount, 0f, t.adaptMax);
        public void Tick(float realDelta, StatTuning t) => Value = Mathf.Max(0f, Value - t.adaptRecoverPerSecond * realDelta);
        public void Reset() => Value = 0f;
    }
}
