using System;
using UnityEngine;

namespace SwordPrototype.Battle
{
    /// <summary>
    /// 스탯 수치를 한곳에서 편집한다. 모든 값은 시험값이다(6차 답변: 제안 수치로 우선 시험).
    /// 배열은 스탯 레벨 0~3 순서.
    /// </summary>
    [Serializable]
    public sealed class StatTuning
    {
        [Header("Health")]
        public float[] maxHealth = { 60f, 80f, 100f, 120f };
        public float frontSuccessHealth0 = -0.20f;
        public float frontSuccessHealthHigh = 0.05f;   // 체력 2~3
        public float health0DamageScale = 0.8f;
        public float failSelfDamage = 3f;              // 체력 0~1, 공격 실패 시 플레이어 피해
        public float failEnemyDamage = 2f;             // 체력 2~3, 공격 실패 시 적 피해
        public float parryWindow = 0.3f;               // 체력 3
        public float parryCooldown = 8f;
        [Range(0f, 1f)] public float parryRefund = 0.5f;

        [Header("Speed")]
        public float[] moveSpeed = { 4f, 4.5f, 5f, 5.5f };
        public float[] dashCooldown = { 2.5f, 1.8f, 1.2f, 0.6f };
        public float[] backSuccessBySpeed = { -0.10f, -0.05f, 0f, 0.10f };
        public float speed0SuccessPenalty = 0.10f;
        public float[] selectionSeconds = { 1f, 1.3f, 1.5f, 2f };

        [Header("Stamina")]
        public float[] skillCooldownScale = { 1.8f, 1.3f, 1f, 0.5f };
        public float knifeDamage = 8f;
        public float knifeFailAfterSuccess = 0.9f;
        public float knifeFailFloor = 0.3f;
        public float knifeRecoverSeconds = 8f;
        public float knifeNextAttackPenalty = 0.15f;

        [Header("Strength")]
        public float baseDamage = 10f;
        public float[] strengthDamageScale = { 0.4f, 0.8f, 1f, 1.2f };
        public float[] strengthSpecialScale = { 0.6f, 0.85f, 1f, 1.15f };
        public float strength3FlatBonus = 2f;
        [Range(0f, 1f)] public float critChance = 0.15f;
        public float critMultiplier = 1.5f;

        [Header("Special")]
        public float dodgeInvulnerability = 0.3f;      // 특수기 1 이상
        public float dashSlashDamageScale = 4.5f;      // 특수기 2 이상, 무조건 성공. 일반 3타 합보다 강하게(9차 답변)
        public float dashSlashCooldown = 10f;
        public float finisherDamageScale = 4f;         // 특수기 3, 무조건 성공(9차 답변)
        public float finisherCooldown = 20f;

        [Header("Attack judgement")]
        public float baseSuccess = 0.6f;
        public float minSuccess = 0.05f;
        public float maxSuccess = 0.95f;
        public float repeatFailBonus = 0.15f;          // 같은 방향 둘째 타 F추가
        public float repeatDamageBonus = 3f;           // 같은 방향 둘째 타 D추가
        public float airStrikeDownBonus = 0.10f;       // S6: 공중 베기 ↓↙↘ 성공률 보너스
        public float sideBiasStep = 0.10f;
        public float sideBiasMax = 0.30f;

        // R5: 검 자세. 나중에 바꿀 때는 이 값들만 고친다(판정·선택 화면·연출이 함께 따라감).
        [Header("Sword stance (R5)")]
        [Tooltip("시작 자세→목표 방향 칸 수(0~4)별 성공률 보정. 0칸=회전 베기(기존 규칙만), 1칸=45° 짧게, 2칸=90°, 3칸=135° 넓게, 4칸=180° 크게")]
        public float[] stanceSwingBonus = { 0f, -0.10f, 0f, 0.05f, 0.10f };
        [Tooltip("상성표(선택). 64칸 = 시작 자세 8 × 목표 방향 8 (AttackDirection 순서, [시작*8+목표]). 비어 있으면 위 칸 수 표를 쓴다.")]
        public float[] stanceMatrix = new float[0];
        [Tooltip("캐릭터 기준 이동 방향(AttackDirection 순서: 앞, 앞오른, 오른, 뒤오른, 뒤, 뒤왼, 왼, 앞왼) → 검 자세. 기본: 움직이는 반대쪽으로 끌림")]
        public AttackDirection[] moveStance =
        {
            AttackDirection.Down, AttackDirection.DownLeft, AttackDirection.Left, AttackDirection.UpLeft,
            AttackDirection.Up, AttackDirection.UpRight, AttackDirection.Right, AttackDirection.DownRight
        };
        public AttackDirection airStance = AttackDirection.Up;          // 점프 중
        public AttackDirection restStance = AttackDirection.DownRight;  // 기본 자세
        public float stanceMoveHoldSeconds = 0.25f;                     // 이만큼 계속 움직여야 자세가 바뀜
        public float stanceIdleResetSeconds = 2f;                       // 멈춘 채 이만큼 지나면 기본 자세

        // R8: 쉬움 보정(사용자 확인) — 공격 재진입 대기, 적 적응(성공할수록 실패 확률↑, 시간 지나면 회복)
        [Header("Pacing (R8)")]
        public float attackReentrySeconds = 1.2f;
        public float adaptPerHit = 0.05f;          // 성공 1타마다 실패 확률 +5%
        public float adaptCounterHit = 0.025f;     // 합 공격 방어 보상 반격은 절반
        public float adaptMax = 0.25f;
        public float adaptRecoverPerSecond = 0.05f / 1.5f;   // 1.5초마다 5%씩 회복

        // R8: 합 공격 방어(속도 스탯 0~3에 따라 약간 길어짐)
        [Header("Defense (R8)")]
        // 2026-10-08 Play 의견 "붉은 칸이 너무 빠름" → 처음 값과 2배 느린 제안의 중간으로
        public float[] defenseFlashSeconds = { 0.35f, 0.38f, 0.42f, 0.46f };
        public float[] defenseInputSeconds = { 1.7f, 1.9f, 2.05f, 2.2f };
        public float defenseGapSeconds = 0.1f;

        public static float At(float[] table, int level) => table[Mathf.Clamp(level, 0, table.Length - 1)];
    }
}
