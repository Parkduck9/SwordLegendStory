using UnityEngine;

namespace SwordPrototype.Battle
{
    /// <summary>
    /// 한쪽(좌/우 묶음)을 공격하면 다음 콤보에서 그쪽 막힘 확률이 오른다.
    /// 다음 콤보에서도 같은 쪽을 써서 성공하면 다다음 콤보에서 더 오른다.
    /// 해당 쪽을 쓰지 않은 콤보가 지나면 초기화. ↑↓는 무시.
    /// </summary>
    public sealed class SideBiasTracker
    {
        private int leftLevel;
        private int rightLevel;

        public int Level(AttackSide side) => side == AttackSide.Left ? leftLevel : side == AttackSide.Right ? rightLevel : 0;

        public float FailPenalty(AttackSide side, StatTuning t) => Mathf.Min(Level(side) * t.sideBiasStep, t.sideBiasMax);

        /// <summary>콤보가 끝날 때마다 좌우 각각 한 번 호출.</summary>
        public void RecordCombo(AttackSide side, bool used, bool succeeded)
        {
            if (side == AttackSide.None) return;
            int level = Level(side);
            int next = !used ? 0 : level > 0 && succeeded ? level + 1 : 1;
            if (side == AttackSide.Left) leftLevel = next; else rightLevel = next;
        }

        public void Reset() { leftLevel = 0; rightLevel = 0; }
    }
}
