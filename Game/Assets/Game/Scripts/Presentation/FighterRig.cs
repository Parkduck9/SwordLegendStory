using UnityEngine;

namespace SwordPrototype.Presentation
{
    /// <summary>
    /// 로우폴리 캐릭터 자세 출력(코드로 부품 회전). 게임 규칙은 없다.
    /// 나중에 Animator 기반 모델로 교체할 때 이 컴포넌트만 바꾸면 된다.
    /// 좌표는 Visual_Replaceable 로컬 기준: x 오른쪽, y 위, z 앞.
    /// </summary>
    public sealed class FighterRig : MonoBehaviour, IFighterVisual
    {
        private static readonly Vector3 Shoulder = new Vector3(0.3f, 1.35f, 0.1f);
        private Transform visual, sword, rightArm, blade;
        private Quaternion visualRest, swordRest, armRest;
        private Vector3 visualPosRest, swordPosRest;
        private TrailRenderer trail;

        private void Awake()
        {
            visual = transform.Find("Visual_Replaceable");
            if (visual == null) return;
            sword = visual.Find("SwordSocket");
            rightArm = visual.Find("RightArm");
            blade = sword != null ? sword.Find("Blade") : null;
            visualRest = visual.localRotation;
            visualPosRest = visual.localPosition;
            if (sword != null) { swordRest = sword.localRotation; swordPosRest = sword.localPosition; }
            if (rightArm != null) armRest = rightArm.localRotation;
        }

        /// <summary>화면 방향(x 오른쪽, y 위)과 앞쪽 성분으로 검날 방향을 만든다.</summary>
        public static Vector3 BladeDirection(Vector2 screen, float forward) => new Vector3(screen.x, screen.y, forward).normalized;

        /// <summary>궤적: from → to. 중간에 앞쪽으로 크게 내밀어 몸 앞을 가르는 베기가 된다.</summary>
        public static Vector3 SwingDirection(Vector2 from, Vector2 to, float t)
        {
            Vector2 s = Vector2.Lerp(from, to, t);
            return BladeDirection(s, 0.5f + Mathf.Sin(t * Mathf.PI));
        }

        // 블록 인형은 보간 없이 바로 든다.
        public void HoldStance(Battle.AttackDirection stance) => SetBlade(Kamae.Of(stance).blade);

        // 블록 인형: 몸 반응은 기존 SetBody 연출이 맡고, 베인 자국만 남긴다.
        public void Wound(Vector3 cut, int tier) => SlashMark.Spawn(transform, transform.position + Vector3.up * 1.2f + transform.forward * 0.25f, cut, tier, 1f);

        public void SetBlade(Vector3 localDirection, float reach = 0.35f)
        {
            if (sword == null) return;
            sword.localRotation = Quaternion.FromToRotation(Vector3.up, localDirection);
            sword.localPosition = Shoulder + localDirection * reach;
            if (rightArm != null)
                rightArm.localRotation = armRest * Quaternion.FromToRotation(Vector3.down, Vector3.Lerp(Vector3.down, localDirection, 0.7f).normalized);
        }

        public void SetBody(Quaternion rotation, Vector3 offset)
        {
            if (visual == null) return;
            visual.localRotation = visualRest * rotation;
            visual.localPosition = visualPosRest + offset;
        }

        public void ResetBody() => SetBody(Quaternion.identity, Vector3.zero);

        public void ResetSword()
        {
            if (sword != null) { sword.localRotation = swordRest; sword.localPosition = swordPosRest; }
            if (rightArm != null) rightArm.localRotation = armRest;
        }

        public void ResetPose() { ResetBody(); ResetSword(); SetTrail(false); }

        /// <summary>블록 인형은 동작 신호가 없다(몸 기울임·검 방향으로만 표현).</summary>
        public void Cue(FighterCue cue) { }

        /// <summary>검 궤적 잔상.</summary>
        public void SetTrail(bool on)
        {
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
