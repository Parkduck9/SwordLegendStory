using UnityEngine;

namespace SwordPrototype.World
{
    /// <summary>
    /// S7·S8-1: 전투장에 들어서면 입구를 닫는다. 투명 충돌 벽을 켜고, 목조 문 두 짝이 0.6초 동안 돌며 닫힌다.
    /// 전투 중 이탈 불가.
    /// </summary>
    public sealed class ArenaGate : MonoBehaviour
    {
        private const float CloseSeconds = 0.6f;
        [SerializeField] private EncounterTrigger encounter;
        [SerializeField] private Collider wall;
        [SerializeField] private Transform leftDoor;
        [SerializeField] private Transform rightDoor;
        private float closing;

        public bool Closed { get; private set; }

        public void Configure(EncounterTrigger gate, Collider barrier, Transform left, Transform right)
        {
            encounter = gate; wall = barrier; leftDoor = left; rightDoor = right;
            Closed = false;
            if (wall != null) wall.enabled = false;
            SetDoors(0f);
        }

        private void Update()
        {
            if (!Closed && encounter != null && encounter.Started)
            {
                Closed = true;
                if (wall != null) wall.enabled = true;
                Audio.Sound.Play(Audio.Sfx.GateClose, transform.position);
            }
            if (Closed && closing < 1f)
            {
                closing = Mathf.Min(1f, closing + Time.deltaTime / CloseSeconds);
                SetDoors(Mathf.SmoothStep(0f, 1f, closing));
            }
        }

        // 0 = 열림(문짝이 진입로 벽을 따라 누움), 1 = 닫힘
        private void SetDoors(float t)
        {
            if (leftDoor != null) leftDoor.localRotation = Quaternion.Euler(0f, Mathf.Lerp(90f, 0f, t), 0f);
            if (rightDoor != null) rightDoor.localRotation = Quaternion.Euler(0f, Mathf.Lerp(-90f, 0f, t), 0f);
        }
    }
}
