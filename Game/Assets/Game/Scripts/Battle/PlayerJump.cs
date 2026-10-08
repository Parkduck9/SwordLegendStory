using UnityEngine;

namespace SwordPrototype.Battle
{
    /// <summary>
    /// S6 점프: Space, 실시간에서만(선택·교환 중 불가). 높이 1.2m · 체공 0.55초에 맞춰 도약 속도와 중력을 계산한다.
    /// 2단 점프·쿨타임 없음. 공중 여부는 적 공격 회피(착지 상태에만 맞는 공격)와 공중 베기 판정에 쓰인다.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    public sealed class PlayerJump : MonoBehaviour
    {
        /// <summary>바닥(y=0)에서 이 높이보다 위면 공중으로 본다.</summary>
        public const float AirborneHeight = 0.35f;

        [SerializeField] private float height = 1.2f;
        [SerializeField] private float airtime = 0.55f;
        private PlayerMovement movement;

        public bool Airborne => transform.position.y > AirborneHeight;

        /// <summary>정점 높이 h, 체공 T: 도약 속도 v = 4h/T.</summary>
        public static float LaunchSpeed(float h, float t) => 4f * h / t;
        /// <summary>중력 g = 8h/T².</summary>
        public static float GravityFor(float h, float t) => 8f * h / (t * t);

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            movement.Gravity = GravityFor(height, airtime);
        }

        private bool wasAirborne;

        private void LateUpdate()
        {
            // S8-4: 착지 소리
            if (wasAirborne && !Airborne) Audio.Sound.Play(Audio.Sfx.Land, transform.position);
            wasAirborne = Airborne;
        }

        private void Update()
        {
            if (!movement.InputEnabled || movement.CombatLocked || movement.DashLocked) return;
            if (Input.GetKeyDown(KeyCode.Space) && movement.Jump(LaunchSpeed(height, airtime))) Audio.Sound.Play(Audio.Sfx.Jump, transform.position);
        }
    }
}
