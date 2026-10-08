using UnityEngine;

namespace SwordPrototype.Battle
{
    /// <summary>
    /// 기력 3 비도. 8방향 선택 가운데 작은 원을 좌클릭해 바로 던진다.
    /// 첫 투척은 반드시 성공. 성공하면 실패 확률이 크게 오른 뒤 시간에 따라 하한 30%까지 내려간다.
    /// 던질 때마다 바로 다음 검 공격 선택의 성공률만 낮춘다. 던지면 현재 선택 단계는 종료된다(7차 답변).
    /// </summary>
    public sealed class ThrowingKnifeState
    {
        private bool usedOnce;
        private float failChance;
        private bool penaltyPending;

        public float FailChance => usedOnce ? failChance : 0f;

        public void Tick(float deltaTime, StatTuning t)
        {
            if (!usedOnce || failChance <= t.knifeFailFloor) return;
            float rate = (t.knifeFailAfterSuccess - t.knifeFailFloor) / Mathf.Max(0.01f, t.knifeRecoverSeconds);
            failChance = Mathf.Max(t.knifeFailFloor, failChance - rate * deltaTime);
        }

        /// <summary>roll은 0~1 난수. 명중하면 true.</summary>
        public bool Throw(float roll, StatTuning t)
        {
            bool hit = roll >= FailChance;
            usedOnce = true;
            failChance = hit ? t.knifeFailAfterSuccess : Mathf.Max(failChance, t.knifeFailFloor);
            penaltyPending = true;
            return hit;
        }

        /// <summary>다음 검 공격 선택 시작 시 호출. 다다음 선택은 원래대로.</summary>
        public float ConsumeNextAttackPenalty(StatTuning t)
        {
            if (!penaltyPending) return 0f;
            penaltyPending = false;
            return t.knifeNextAttackPenalty;
        }
    }
}
