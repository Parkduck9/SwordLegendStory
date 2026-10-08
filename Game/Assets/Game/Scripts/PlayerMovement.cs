using UnityEngine;

namespace SwordPrototype
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private float testWalkSpeed = 5f;
        [SerializeField] private float airControl = 0.4f;   // S6: 공중 조작은 지상의 40%
        private CharacterController body;
        private Transform view;
        private float fallSpeed;
        private Vector3 airVelocity;
        public bool InputEnabled { get; set; } = true;
        /// <summary>전투 선택·교환 연출 중 이동 입력 차단.</summary>
        public bool CombatLocked { get; set; }
        /// <summary>대쉬 중 보행 입력 차단(PlayerDash가 설정).</summary>
        public bool DashLocked { get; set; }
        /// <summary>선택·교환 중 중력 정지(공중 베기 시 공중에 멈춤). BattleFlow가 설정.</summary>
        public bool Suspended { get; set; }
        /// <summary>중력 가속도(m/s²). PlayerJump가 점프 높이·체공 시간에 맞춰 설정.</summary>
        public float Gravity { get; set; } = 9.81f;
        public bool Grounded => body != null && body.isGrounded;
        /// <summary>마지막 입력 이동 방향(대쉬 방향 결정용). 입력이 없으면 0.</summary>
        public Vector3 LastMoveDirection { get; private set; }
        /// <summary>속도 스탯에서 설정하는 보행 속도(m/s).</summary>
        public float WalkSpeed { get => testWalkSpeed; set => testWalkSpeed = value; }
        public float Motion { get; private set; }
        public Transform LockTarget { get; set; }

        private void Awake() { body = GetComponent<CharacterController>(); }
        private void Start() { view = Camera.main.transform; }

        /// <summary>지상에서만 위로 도약. 현재 수평 속도를 공중 관성으로 유지한다.</summary>
        public bool Jump(float upSpeed)
        {
            if (!body.isGrounded || Suspended) return false;
            fallSpeed = upSpeed;
            return true;
        }

        private void Update()
        {
            if (Suspended) { Motion = 0f; fallSpeed = 0f; return; }
            Vector3 direction = Vector3.zero;
            if (InputEnabled && !CombatLocked && !DashLocked)
            {
                Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized;
                Vector3 right = Vector3.ProjectOnPlane(view.right, Vector3.up).normalized;
                direction = Vector3.ClampMagnitude(forward * Input.GetAxisRaw("Vertical") + right * Input.GetAxisRaw("Horizontal"), 1f);
            }
            Motion = direction.magnitude;
            LastMoveDirection = direction;
            Vector3 horizontal = direction * testWalkSpeed;
            if (body.isGrounded) airVelocity = horizontal;
            else horizontal = Vector3.Lerp(airVelocity, horizontal, airControl);
            if (body.isGrounded && fallSpeed < 0f) fallSpeed = -2f;
            fallSpeed -= Gravity * Time.deltaTime;
            body.Move((horizontal + Vector3.up * fallSpeed) * Time.deltaTime);
            Vector3 facing = LockTarget != null ? Vector3.ProjectOnPlane(LockTarget.position - transform.position, Vector3.up) : direction;
            if (facing.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(facing), 14f * Time.deltaTime);
        }
    }
}
