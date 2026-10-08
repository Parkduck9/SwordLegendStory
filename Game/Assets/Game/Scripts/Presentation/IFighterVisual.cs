using UnityEngine;

namespace SwordPrototype.Presentation
{
    /// <summary>
    /// S8-2: 연출이 캐릭터 외형에 내리는 의미 있는 동작 신호. 블록 인형(FighterRig)은 무시하고,
    /// 애니메이션 캐릭터(AnimatorRig)는 해당 애니메이션 상태로 넘어간다.
    /// </summary>
    public enum FighterCue
    {
        Idle,       // 기본 겨눔(이동 섞기로 복귀)
        Swing,      // 베기
        Backstep,   // 물러나며 피함
        Roll,       // 옆 구르기 · 비켜 피하기
        Crouch,     // 몸 낮춰 빠지기
        Block,      // 검으로 막기 · 맞받아침 · 가드
        Hit,        // 맞부딪혀 튕김 · 피격
        HitHead,    // 상체 젖힘
        Knockback,  // R6: 깊게 베여 크게 밀림(UAL2 Hit_Knockback)
        // R8 KayKit 동작(팔·검은 동작이 직접 움직임 — IK 끔)
        Slice,      // 큰 검 가로 베기
        Chop,       // 내려 베기
        Stab,       // 찌르기
        SpinAttack, // 몸을 돌려 베기
        Kick,       // 발차기
        JumpChop,   // 뛰어올라 내려베기
        BlockHit,   // 막으며 튕겨남(합 공격 방어)
        Spinning,   // 플레이어 회전 베기(몸 회전은 루트가, 동작은 팔·몸통)
        DodgeBack, DodgeLeft, DodgeRight,   // 회피
        Struggle,   // 힘겨루기(밀기)
        Throw,      // 투척 · 비도
        Stagger,    // 무방비(기지개)
        Death,
        Victory
    }

    /// <summary>
    /// 연출(ExchangeDirector)·적 자세(EnemyPose)가 쓰는 캐릭터 외형 계약. 게임 규칙은 갖지 않는다.
    /// 좌표는 캐릭터 기준: x 오른쪽, y 위, z 앞.
    /// </summary>
    public interface IFighterVisual
    {
        /// <summary>검날 방향(캐릭터 기준)과 손 뻗는 정도.</summary>
        void SetBlade(Vector3 localDirection, float reach = 0.35f);
        /// <summary>R5·R6: 실시간 겨눔 자세. 검을 그 자세(Kamae 표)로 부드럽게 옮겨 들고 있는다(SetBlade·ResetSword가 오면 취소).</summary>
        void HoldStance(Battle.AttackDirection stance);
        /// <summary>R6: 베임 표현. cut은 칼이 지나간 월드 방향, tier 0 스침 · 1 베임 · 2 깊게. 몸을 빼는 반응 + 베인 자국.</summary>
        void Wound(Vector3 cut, int tier);
        /// <summary>몸 전체 기울임·위치 보정(블록 인형용, 애니메이션 캐릭터는 동작이 대신함).</summary>
        void SetBody(Quaternion rotation, Vector3 offset);
        void ResetBody();
        void ResetSword();
        void ResetPose();
        void SetTrail(bool on);
        void Cue(FighterCue cue);
    }
}
