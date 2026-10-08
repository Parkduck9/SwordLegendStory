using UnityEngine;

namespace SwordPrototype
{
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Transform enemy;
        [SerializeField] private float distance = 6f;
        private float yaw;
        private float pitch = 22f;
        private bool locked;
        private bool focused;
        private float spinTime;
        private float spinDuration;
        private PlayerMovement movement;
        private Vector3 focusBase;
        public bool Captured => Cursor.lockState == CursorLockMode.Locked;
        public bool Locked => locked;
        public void Configure(Transform owner, Transform target) { player = owner; enemy = target; }

        /// <summary>공격 선택 중 플레이어·검 포커스. 켜져 있는 동안 마우스는 방향 선택에만 쓰인다.</summary>
        public void SetFocus(bool value)
        {
            // 포커스 해제 시 플레이어 뒤쪽 시점으로 자연스럽게 복귀.
            if (focused && !value && player != null) yaw = player.eulerAngles.y;
            if (!focused && value) focusBase = transform.position;
            if (!value) heldFacing = null;   // 연출이 중간에 끊겨도 고정이 남지 않게
            focused = value;
        }

        /// <summary>특수기 3 마무리: 플레이어 주위를 한 바퀴 도는 카메라와 흔들림.</summary>
        public void PlaySpin(float seconds) { spinDuration = Mathf.Max(0.05f, seconds); spinTime = spinDuration; }

        private Quaternion? heldFacing;
        /// <summary>회전 베기 동안 카메라 구도를 지금 플레이어 방향에 고정한다(몸이 돌아도 카메라는 따라 돌지 않음).</summary>
        public void HoldFacing(bool hold) => heldFacing = hold && player != null ? player.rotation : (Quaternion?)null;
        public bool FacingHeld => heldFacing.HasValue;

        /// <summary>타격 흔들림. 실제 시간으로 감쇠.</summary>
        public void Shake(float amplitude, float seconds)
        {
            if (amplitude < shakeAmplitude * (shakeTime / Mathf.Max(0.01f, shakeDuration))) return;
            shakeAmplitude = amplitude; shakeDuration = Mathf.Max(0.01f, seconds); shakeTime = shakeDuration;
        }

        private float shakeTime, shakeDuration = 1f, shakeAmplitude;

        private Vector3 ShakeOffset()
        {
            if (shakeTime <= 0f) return Vector3.zero;
            shakeTime -= Time.unscaledDeltaTime;
            return Random.insideUnitSphere * shakeAmplitude * Mathf.Clamp01(shakeTime / shakeDuration);
        }

        private void Start()
        {
            movement = player.GetComponent<PlayerMovement>();
            Capture(true);
        }
        /// <summary>마우스 포획(게임 조작) / 해제(메뉴). S7: Esc 처리는 일시정지 메뉴가 담당.</summary>
        public void Capture(bool value)
        {
            Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !value;
            if (movement != null) movement.InputEnabled = value;
        }
        private void LateUpdate()
        {
            if (focused) { UpdateFocus(); return; }
            if (!Captured) return;
            if (Input.GetMouseButtonDown(2)) locked = !locked;
            movement.LockTarget = locked ? enemy : null;
            if (locked && enemy != null)
            {
                Vector3 delta = enemy.position - player.position;
                yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            }
            else yaw += Input.GetAxis("Mouse X") * 3f * Flow.GameSettings.CameraSensitivity;
            pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 2f * Flow.GameSettings.CameraSensitivity, -10f, 65f);
            Vector3 pivot = player.position + Vector3.up * 1.5f;
            Vector3 offset = Quaternion.Euler(pitch, yaw, 0) * Vector3.back;
            float actualDistance = distance;
            // Only scenery on layer 8 obstructs the camera, not the owning character.
            if (Physics.SphereCast(pivot, 0.18f, offset, out RaycastHit hit, distance, 1 << 8))
                actualDistance = Mathf.Max(0.3f, hit.distance - 0.1f);
            transform.position = pivot + offset * actualDistance;
            transform.LookAt(pivot);
        }

        // 오른쪽 어깨 뒤에서 검과 적 사이를 바라본다. 슬로모션 영향을 받지 않도록 실제 시간으로 보간.
        private void UpdateFocus()
        {
            Quaternion basis = heldFacing ?? player.rotation;
            Vector3 right = basis * Vector3.right, forward = basis * Vector3.forward;
            Vector3 pivot = player.position + Vector3.up * 1.3f;
            Vector3 target = pivot + right * 0.9f - forward * 2.4f + Vector3.up * 0.4f;
            Vector3 look = player.position + forward * 1.4f + Vector3.up * 1.1f;
            if (spinTime > 0f)
            {
                spinTime -= Time.unscaledDeltaTime;
                float angle = 360f * (1f - Mathf.Clamp01(spinTime / spinDuration));
                target = pivot + Quaternion.Euler(0, angle, 0) * (target - pivot) + Random.insideUnitSphere * 0.12f;
                transform.position = target;
                focusBase = target;
                transform.LookAt(pivot);
                return;
            }
            float blend = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
            focusBase = Vector3.Lerp(focusBase, target, blend);
            transform.position = focusBase + ShakeOffset();
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look - focusBase), blend);
        }
    }
}
