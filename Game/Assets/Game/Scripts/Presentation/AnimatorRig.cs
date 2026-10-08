using UnityEngine;

namespace SwordPrototype.Presentation
{
    /// <summary>
    /// S8-2: 애니메이션 캐릭터(Quaternius 마네킹)의 외형 출력. FighterRig와 같은 호출을 받는다.
    /// - 몸: 애니메이터 상태(이동 섞기 + 동작 신호)
    /// - 검 든 오른팔: Humanoid IK로 8방향 궤적의 손 위치를 따라감(항상 켬, 쉴 때는 기본 자세 방향)
    /// - 검: 손 위치에 두고 검날을 궤적 방향으로 돌림
    /// R6: 실시간 겨눔은 Kamae 표의 실제 검술 자세(손 위치 + 칼 방향)로, 두 팔은 위 층(Arms)의 검 겨눔 동작으로 덮어 팔 흔들기를 없앤다.
    ///     옆·뒤 이동은 다리를 이동 쪽으로 돌리고(최대 60°) 상체를 반대로 비틀어 적을 보게 하며, 뒤로는 걷기를 거꾸로 재생한다.
    ///     SetBody(몸 기울임)와 Wound(베임 반응)는 척추·가슴 뼈에 덧입힌다.
    /// 게임 규칙은 갖지 않는다.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class AnimatorRig : MonoBehaviour, IFighterVisual
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int MoveXId = Animator.StringToHash("MoveX");
        private static readonly int MoveZId = Animator.StringToHash("MoveZ");
        private float moveX, moveZ;
        private bool animDriven;   // R8: 팔·검을 동작이 직접 움직이는 동작 신호 중(IK 끔)
        private Transform indexKnuckle, littleKnuckle, lowerArm;
        private static readonly Vector3 RestBlade = FighterRig.BladeDirection(new Vector2(0.55f, -0.75f), 0.6f);   // 기본 ↘

        [SerializeField] private Transform characterRoot;   // 이동·회전하는 캐릭터 루트(플레이어·적)
        [SerializeField] private Transform sword;           // 검(손 위치를 따라감)
        [SerializeField] private float armReach = 0.5f;
        [SerializeField] private float cueSeconds = 0.6f;    // 동작 신호 후 이동 섞기로 돌아가기까지

        private Animator animator;
        private Transform hand, upperArm, hips, spine, chest, upperChest;
        private Vector3 bladeLocal = RestBlade;
        private float reach = 0.35f;
        private Vector3 lastPosition;
        private float speed;
        private float cueTimer;
        private bool airborne;
        private TrailRenderer trail;
        private float stepTimer;
        private float armsWeight;
        private Vector3 baseModelPosition;

        [SerializeField] private float heightMeters = 1.8f;   // R7: 캐릭터 키(모델이 바뀌어도 Kamae 표 배율이 맞게)

        public void Configure(Transform root, Transform swordTransform, float height = 1.8f) { characterRoot = root; sword = swordTransform; heightMeters = height; }

        private float BodyScale => heightMeters / 1.8f;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            upperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            upperChest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
            indexKnuckle = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
            littleKnuckle = animator.GetBoneTransform(HumanBodyBones.RightLittleProximal);
            lowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            if (characterRoot == null) characterRoot = transform.parent != null ? transform.parent : transform;
            lastPosition = characterRoot.position;
            baseModelPosition = transform.localPosition;
        }

        private void Update()
        {
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            float real = Battle.BattleClock.RealDelta;
            Vector3 delta = Vector3.ProjectOnPlane(characterRoot.position - lastPosition, Vector3.up);
            lastPosition = characterRoot.position;
            speed = Mathf.Lerp(speed, delta.magnitude / dt, 1f - Mathf.Exp(-10f * dt));
            animator.SetFloat(SpeedId, speed);

            // R8 하체: KayKit 옆으로 달리기·뒷걸음 동작이 생겨 2D 방향 섞기로(다리 돌리기·거꾸로 걷기 흉내는 제거)
            Vector3 local = characterRoot.InverseTransformDirection(delta / dt);   // 속도(m/s) — 한 프레임 이동량은 너무 작아 방향 판정에 못 씀
            float blend = 1f - Mathf.Exp(-10f * dt);
            moveX = Mathf.Lerp(moveX, local.x, blend);
            moveZ = Mathf.Lerp(moveZ, local.z, blend);
            animator.SetFloat(MoveXId, moveX);
            animator.SetFloat(MoveZId, moveZ);

            // R6 겨눔: 자세 표의 손 위치·칼 방향으로 약 0.2초에 걸쳐 옮김
            if (holding)
            {
                float k = 1f - Mathf.Exp(-14f * real);
                holdHand = Vector3.Lerp(holdHand, holdPose.hand, k);
                bladeLocal = Vector3.Slerp(bladeLocal, holdPose.blade, k).normalized;
            }
            // 두 팔 위 층: 동작 신호(베기·피격 등) 중에는 끄고, 평소에는 켜서 팔 흔들기 대신 검을 겨눔
            float armsTarget = cueTimer > 0f ? 0f : 1f;
            armsWeight = Mathf.MoveTowards(armsWeight, armsTarget, 6f * Mathf.Max(real, dt));
            if (animator.layerCount > 1) animator.SetLayerWeight(1, armsWeight);

            // S8-4 발소리: 걸음이 빠를수록 간격이 짧고, 몸집이 클수록 낮게
            if (characterRoot.position.y < 0.35f && speed > 0.8f)
            {
                stepTimer -= dt;
                if (stepTimer <= 0f)
                {
                    stepTimer = Mathf.Lerp(0.55f, 0.27f, Mathf.InverseLerp(1f, 7f, speed));
                    Audio.Sound.Play(Audio.Sfx.Footstep, characterRoot.position, Mathf.Clamp(0.93f / Mathf.Max(0.1f, transform.localScale.y), 0.7f, 1.15f));
                }
            }

            if (flinchTime >= 0f) flinchTime += real;

            if (cueTimer > 0f) { cueTimer -= real; if (cueTimer <= 0f) { animDriven = false; CrossFade("Locomotion", 0.15f); } return; }
            bool air = characterRoot.position.y > 0.35f;
            if (air && !airborne) CrossFade("Air", 0.1f);
            else if (!air && airborne) { CrossFade("Land", 0.05f); cueTimer = 0.25f; }
            airborne = air;
        }

        // 애니메이션·IK가 끝난 뒤: 상체 비틀기·기울임·움찔을 덧입히고, 검을 손 위치에 맞춘다.
        private void LateUpdate()
        {
            Quaternion add = UpperBodyOffset();
            if (spine != null && add != Quaternion.identity)
            {
                // 척추·가슴에 나눠 적용(자식이므로 누적되어 합계가 add)
                Quaternion half = Quaternion.Slerp(Quaternion.identity, add, 0.5f);
                spine.rotation = half * spine.rotation;
                Transform second = upperChest != null ? upperChest : chest;
                if (second != null) second.rotation = half * second.rotation;
            }
            transform.localPosition = baseModelPosition + bodyOffset;
            if (sword == null || hand == null) return;
            sword.position = hand.position;
            if (animDriven)
            {
                // R8: 동작이 팔을 움직이는 중에는 쥔 손 방향(새끼손가락 → 검지 마디)이 칼날 방향
                Vector3 grip = indexKnuckle != null && littleKnuckle != null ? indexKnuckle.position - littleKnuckle.position
                    : lowerArm != null ? hand.position - lowerArm.position : characterRoot.up;
                if (grip.sqrMagnitude > 1e-6f) sword.rotation = Quaternion.FromToRotation(Vector3.up, grip.normalized);
                return;
            }
            sword.rotation = Quaternion.FromToRotation(Vector3.up, characterRoot.TransformDirection(bladeLocal));
        }

        // 상체에 덧입힐 월드 회전: SetBody 기울임 + 베임 움찔
        private Quaternion UpperBodyOffset()
        {
            Quaternion lean = characterRoot.rotation * bodyRotation * Quaternion.Inverse(characterRoot.rotation);
            return lean * FlinchRotation();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (upperArm == null) return;
            if (animDriven) { animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f); animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 0f); return; }
            Vector3 target;
            if (holding)
                target = characterRoot.TransformPoint(holdHand * BodyScale);
            else
            {
                Vector3 worldDir = characterRoot.TransformDirection(bladeLocal);
                // 어깨에서 검날 방향으로 팔을 뻗은 위치(앞쪽으로 조금 빼서 몸통을 파고들지 않게)
                target = upperArm.position + characterRoot.forward * 0.15f + worldDir * (armReach + reach * 0.4f);
            }
            // 기울임·움찔은 손 목표에 반영하지 않아 팔과 검이 몸과 함께 움직인다.
            Vector3 pivot = hips != null ? hips.position : characterRoot.position;
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
            animator.SetIKPosition(AvatarIKGoal.RightHand, target);
            // 팔꿈치는 오른쪽 뒤로(겨눔 자세에서 팔이 몸을 파고들지 않게)
            animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, holding ? 0.6f : 0f);
            animator.SetIKHintPosition(AvatarIKHint.RightElbow, characterRoot.TransformPoint(new Vector3(0.45f, 1.1f, -0.25f) * BodyScale));
        }

        public void SetBlade(Vector3 localDirection, float reachAmount = 0.35f)
        {
            holding = false;
            if (localDirection.sqrMagnitude > 0.0001f) bladeLocal = localDirection.normalized;
            reach = reachAmount;
        }

        // R5·R6 겨눔 자세: Kamae 표의 손 위치·칼 방향으로 부드럽게 옮긴다.
        private bool holding;
        private Kamae.Pose holdPose;
        private Vector3 holdHand;
        public void HoldStance(Battle.AttackDirection stance)
        {
            if (!holding)
            {
                // 지금 손 위치에서 출발(갑자기 튀지 않게)
                holdHand = hand != null ? characterRoot.InverseTransformPoint(hand.position) / BodyScale : Kamae.Of(stance).hand;
            }
            holdPose = Kamae.Of(stance);
            reach = 0.35f;
            holding = true;
        }

        // 몸 기울임(연출이 주는 값): 척추에 덧입힘. 크게 도는 값(구르기·회전)은 애니메이션이 대신하므로 40°까지만.
        private Quaternion bodyRotation = Quaternion.identity;
        private Vector3 bodyOffset;
        public void SetBody(Quaternion rotation, Vector3 offset)
        {
            bodyRotation = Quaternion.Angle(Quaternion.identity, rotation) <= 90f
                ? Quaternion.RotateTowards(Quaternion.identity, rotation, 40f) : Quaternion.identity;
            bodyOffset = Vector3.ClampMagnitude(offset, 0.3f) * 0.5f;
        }
        public void ResetBody() { bodyRotation = Quaternion.identity; bodyOffset = Vector3.zero; }
        public void ResetSword() { holding = false; bladeLocal = RestBlade; reach = 0.35f; }
        public void ResetPose() { ResetSword(); ResetBody(); SetTrail(false); }

        // R6 베임 반응: 칼이 지나간 반대쪽으로 상체를 빼며 움찔(0.06초 만에 최대, 이후 감쇠) + 베인 자국
        private static readonly float[] FlinchDegrees = { 22f, 34f, 44f };
        private float flinchTime = -1f, flinchDegrees;
        private Vector3 flinchAxis = Vector3.right;
        public void Wound(Vector3 cut, int tier)
        {
            tier = Mathf.Clamp(tier, 0, 2);
            Vector3 toward = Vector3.ProjectOnPlane(characterRoot.forward, Vector3.up).normalized;   // 상대 쪽(정면)
            if (cut.sqrMagnitude < 0.0001f) cut = characterRoot.right;
            // 뒤로 젖히는 축(정면 기준) + 칼이 지나가는 방향으로 휩쓸리는 축을 섞는다.
            Vector3 back = Vector3.Cross(toward, Vector3.up);           // 이 축으로 +회전 = 상체가 뒤로
            Vector3 sweep = Vector3.Cross(toward, cut.normalized);      // 칼 진행 방향으로 몸이 밀리는 축(오른쪽으로 베면 오른쪽으로 돌아감)
            flinchAxis = (back * 0.7f + sweep * 0.5f).normalized;
            flinchDegrees = FlinchDegrees[tier];
            flinchTime = 0f;
            Transform anchor = upperChest != null ? upperChest : chest != null ? chest : transform;
            float s = BodyScale;
            Vector3 center = anchor.position + toward * 0.17f * s + Vector3.down * 0.05f * s;
            SlashMark.Spawn(anchor, center, cut, tier, s);
        }

        private Quaternion FlinchRotation()
        {
            if (flinchTime < 0f) return Quaternion.identity;
            float t = flinchTime;
            float amount = t < 0.06f ? Mathf.Sin(t / 0.06f * Mathf.PI * 0.5f) : Mathf.Exp(-6f * (t - 0.06f)) * Mathf.Cos((t - 0.06f) * 9f);
            if (t > 1.2f) { flinchTime = -1f; return Quaternion.identity; }
            return Quaternion.AngleAxis(flinchDegrees * amount, flinchAxis);
        }

        public void Cue(FighterCue cue)
        {
            switch (cue)
            {
                case FighterCue.Idle: cueTimer = 0f; animDriven = false; CrossFade("Locomotion", 0.12f); return;
                case FighterCue.Swing: Play("Swing", 0.25f, 0.05f); return;
                case FighterCue.Backstep:
                case FighterCue.DodgeBack: Play("DodgeBack", 0.4f); return;   // R8: 피격 대신 실제 뒤로 회피(KayKit)
                case FighterCue.DodgeLeft: Play("DodgeLeft", 0.4f); return;
                case FighterCue.DodgeRight: Play("DodgeRight", 0.4f); return;
                case FighterCue.BlockHit: Play("BlockHit", 0.45f); return;
                case FighterCue.Spinning: Play("Spinning", 0.4f); return;
                // R8 적 공격 동작: 팔·검을 동작이 직접 움직인다
                case FighterCue.Slice: Act("Slice", 0.55f); return;
                case FighterCue.Chop: Act("Chop", 0.6f); return;
                case FighterCue.Stab: Act("Stab", 0.55f); return;
                case FighterCue.SpinAttack: Act("SpinAttack", 0.8f); return;
                case FighterCue.Kick: Act("Kick", 0.5f); return;
                case FighterCue.JumpChop: Act("JumpChop", 0.7f); return;
                case FighterCue.Roll: Play("Roll", 0.6f); return;
                case FighterCue.Crouch: Play("Crouch", 0.45f); return;
                case FighterCue.Block: Play("Guard", cueSeconds, 0.15f); SetBlade(FighterRig.BladeDirection(new Vector2(-0.2f, 0.8f), 1f)); return;
                case FighterCue.Hit: Play("Hit", 0.35f); return;
                case FighterCue.HitHead: Play("HitHead", 0.4f); return;
                case FighterCue.Knockback: Play("Knockback", 0.6f); return;
                case FighterCue.Struggle: Play("Struggle", 0.5f); return;
                case FighterCue.Throw: Act("Throw", 0.5f); return;
                case FighterCue.Stagger: Play("Stagger", 1.6f); return;
                case FighterCue.Death: Play("Death", float.MaxValue); return;
                case FighterCue.Victory: Play("Victory", float.MaxValue); return;
            }
        }

        // 동작이 팔·검을 직접 움직이는 신호(IK 끔). 다른 신호가 오면 해제.
        private void Act(string state, float seconds)
        {
            Play(state, seconds);
            animDriven = true;
        }

        private void Play(string state, float seconds, float normalizedStart = 0f)
        {
            animDriven = false;
            cueTimer = seconds;
            animator.CrossFadeInFixedTime(state, 0.06f, 0, normalizedStart);
        }

        private void CrossFade(string state, float blend) => animator.CrossFadeInFixedTime(state, blend, 0);

        public void SetTrail(bool on)
        {
            Transform blade = sword != null ? sword.Find("Blade") : null;
            if (blade == null) return;
            if (trail == null)
            {
                if (!on) return;
                var tip = new GameObject("TrailTip").transform;
                tip.SetParent(blade, false);
                tip.localPosition = new Vector3(0f, 0.5f, 0f);
                trail = tip.gameObject.AddComponent<TrailRenderer>();
                trail.time = 0.12f;
                trail.minVertexDistance = 0.02f;
                trail.widthMultiplier = 0.08f;
                trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
                trail.material = new Material(Shader.Find("Sprites/Default"));
                trail.startColor = new Color(1f, 1f, 1f, 0.8f);
                trail.endColor = new Color(1f, 1f, 1f, 0f);
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            if (on) trail.Clear();
            trail.emitting = on;
        }
    }
}
