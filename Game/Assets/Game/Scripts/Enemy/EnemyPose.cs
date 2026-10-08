using UnityEngine;
using SwordPrototype.Presentation;

namespace SwordPrototype.Enemy
{
    /// <summary>
    /// 적 자세 출력(예고·공격·경직·무방비 '기지개'). 게임 규칙은 갖지 않는다.
    /// S8-2: 애니메이션 캐릭터(AnimatorRig)가 있으면 동작 신호로, 없으면 블록 인형 부품을 직접 돌린다.
    /// </summary>
    public sealed class EnemyPose : MonoBehaviour
    {
        private static readonly Vector3 WindupBlade = FighterRig.BladeDirection(new Vector2(0.2f, 1f), -0.3f);   // 대도를 머리 위로
        private static readonly Vector3 StaggerBlade = FighterRig.BladeDirection(new Vector2(0.4f, 1f), -0.8f);

        private Transform visual, leftArm, rightArm, sword;
        private Quaternion visualRest, leftRest, rightRest, swordRest;
        private AnimatorRig animated;

        public bool Staggered { get; private set; }

        private void Awake()
        {
            animated = GetComponentInChildren<AnimatorRig>();
            visual = transform.Find("Visual_Replaceable");
            if (visual == null) return;
            leftArm = visual.Find("LeftArm");
            rightArm = visual.Find("RightArm");
            sword = visual.Find("SwordSocket");
            visualRest = visual.localRotation;
            if (leftArm != null) leftRest = leftArm.localRotation;
            if (rightArm != null) rightRest = rightArm.localRotation;
            if (sword != null) swordRest = sword.localRotation;
        }

        /// <summary>예고: 검을 들어 올린다(기술마다 준비 자세). 무방비 중에는 무시.</summary>
        public void SetWindup(EnemyAttackKind kind = EnemyAttackKind.Sweep)
        {
            if (Staggered) return;
            if (animated != null)
            {
                // R8: 발차기·투척은 검을 들지 않고 몸을 낮춤, 나머지는 검을 머리 위로
                if (kind == EnemyAttackKind.Kick || kind == EnemyAttackKind.Throw || kind == EnemyAttackKind.FanThrow || kind == EnemyAttackKind.RetreatShot)
                    animated.Cue(FighterCue.Crouch);
                else { animated.Cue(FighterCue.Block); animated.SetBlade(WindupBlade, 0.2f); }
                return;
            }
            if (sword != null) sword.localRotation = swordRest * Quaternion.Euler(-100f, 0f, 0f);
        }

        /// <summary>R8: 기술별 실제 공격 동작(KayKit). 팔·검은 동작이 직접 움직인다(IK 끔).</summary>
        public static FighterCue StrikeCue(EnemyAttackKind kind)
        {
            switch (kind)
            {
                case EnemyAttackKind.SpinSlash: return FighterCue.SpinAttack;
                case EnemyAttackKind.Kick: return FighterCue.Kick;
                case EnemyAttackKind.Thrust: return FighterCue.Stab;
                case EnemyAttackKind.ChargeSlash: return FighterCue.Chop;
                case EnemyAttackKind.LeapSlam: return FighterCue.JumpChop;
                case EnemyAttackKind.Throw:
                case EnemyAttackKind.FanThrow:
                case EnemyAttackKind.RetreatShot: return FighterCue.Throw;
                default: return FighterCue.Slice;
            }
        }

        /// <summary>공격: 기술에 맞는 동작으로 벤다.</summary>
        public void SetStrike(EnemyAttackKind kind = EnemyAttackKind.Sweep)
        {
            if (Staggered) return;
            if (animated != null) { animated.Cue(StrikeCue(kind)); return; }
            if (sword != null) sword.localRotation = swordRest * Quaternion.Euler(60f, 0f, 0f);
        }

        /// <summary>경직: 뒤로 살짝 젖힌다.</summary>
        public void SetHitStun()
        {
            if (Staggered) return;
            if (animated != null) { animated.Cue(FighterCue.Hit); animated.ResetSword(); return; }
            if (visual != null) visual.localRotation = visualRest * Quaternion.Euler(-8f, 0f, 0f);
            if (sword != null) sword.localRotation = swordRest;
        }

        public void ResetPose()
        {
            if (Staggered) return;
            if (animated != null) { animated.Cue(FighterCue.Idle); animated.ResetSword(); return; }
            if (visual != null) visual.localRotation = visualRest;
            if (sword != null) sword.localRotation = swordRest;
        }

        public void SetStagger(bool value)
        {
            Staggered = value;
            if (animated != null)
            {
                if (value) { animated.Cue(FighterCue.Stagger); animated.SetBlade(StaggerBlade, 0.2f); }
                else { animated.Cue(FighterCue.Idle); animated.ResetSword(); }
                return;
            }
            if (visual == null) return;
            visual.localRotation = value ? visualRest * Quaternion.Euler(-18f, 0f, 0f) : visualRest;
            if (leftArm != null) leftArm.localRotation = value ? leftRest * Quaternion.Euler(0f, 0f, -150f) : leftRest;
            if (rightArm != null) rightArm.localRotation = value ? rightRest * Quaternion.Euler(0f, 0f, 150f) : rightRest;
            if (sword != null) sword.localRotation = value ? swordRest * Quaternion.Euler(-150f, 0f, 0f) : swordRest;
        }
    }
}
