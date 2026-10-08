using UnityEngine;

namespace SwordPrototype.Battle
{
    /// <summary>
    /// Shift 대쉬. 충전 시간은 속도 스탯(0: 2.5초 ~ 3: 0.6초), 무적은 특수기 1 이상일 때 0.3초.
    /// 전투 선택·교환 중(CombatLocked)에는 쓸 수 없다.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(PlayerMovement), typeof(PlayerStats))]
    public sealed class PlayerDash : MonoBehaviour
    {
        [Header("시험값")]
        [SerializeField] private float dashDistance = 4f;
        [SerializeField] private float dashSeconds = 0.18f;

        private CharacterController body;
        private PlayerMovement movement;
        private PlayerStats stats;
        private Health health;
        private readonly Cooldown cooldown = new Cooldown();
        private Vector3 dashDirection;
        private float dashRemaining;

        public Cooldown Cooldown => cooldown;
        /// <summary>대쉬 시작(월드 수평 방향). R5 검 자세 전환용.</summary>
        public event System.Action<Vector3> Dashed;
        public bool Dashing => dashRemaining > 0f;

        private void Awake()
        {
            body = GetComponent<CharacterController>();
            movement = GetComponent<PlayerMovement>();
            stats = GetComponent<PlayerStats>();
            health = GetComponent<Health>();
        }

        private void Update()
        {
            cooldown.Tick(BattleClock.RealDelta);   // R1: 메뉴 동안 충전 정지
            if (Dashing)
            {
                float step = Mathf.Min(Time.deltaTime, dashRemaining);
                dashRemaining -= step;
                body.Move(dashDirection * (dashDistance / dashSeconds) * step);
                if (!Dashing) movement.DashLocked = false;
                return;
            }
            // S6: 공중 대쉬 없음(지상 전용)
            bool allowed = movement.InputEnabled && !movement.CombatLocked && cooldown.Ready && movement.Grounded;
            if (!allowed || !(Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))) return;

            dashDirection = movement.LastMoveDirection.sqrMagnitude > 0.01f ? movement.LastMoveDirection : transform.forward;
            dashDirection = Vector3.ProjectOnPlane(dashDirection, Vector3.up).normalized;
            dashRemaining = dashSeconds;
            movement.DashLocked = true;
            cooldown.Start(StatEffects.DashCooldown(stats.Stats, stats.Tuning));
            Audio.Sound.Play(Audio.Sfx.Dash, transform.position);
            Dashed?.Invoke(dashDirection);
            if (health != null) health.GrantInvulnerability(StatEffects.DodgeInvulnerability(stats.Stats, stats.Tuning));
        }
    }
}
