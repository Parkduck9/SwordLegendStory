using UnityEngine;

namespace SwordPrototype.Battle
{
    /// <summary>플레이어 스탯과 시험 수치를 보관하고 시작 시 체력·이동속도에 적용한다.</summary>
    public sealed class PlayerStats : MonoBehaviour
    {
        [SerializeField] private CharacterStats stats = new CharacterStats(2, 2, 2, 2, 2);
        [SerializeField] private StatTuning tuning = new StatTuning();

        public CharacterStats Stats => stats;
        public StatTuning Tuning => tuning;

        private void Awake()
        {
            // S7: 타이틀의 스탯 배분 화면을 거쳐 왔으면 그 값을, 전투장 단독 실행이면 Inspector 값을 쓴다.
            if (Flow.RunSetup.HasSelection) stats = Flow.RunSetup.Stats;
            if (!stats.IsValid(out string reason))
                Debug.LogWarning("스탯 설정 오류: " + reason);
            if (TryGetComponent(out Health health)) health.SetMax(StatEffects.MaxHealth(stats, tuning));
            if (TryGetComponent(out PlayerMovement movement)) movement.WalkSpeed = StatEffects.MoveSpeed(stats, tuning);
        }
    }
}
