using System.Collections.Generic;
using UnityEngine;

namespace SwordPrototype.World
{
    /// <summary>
    /// S6 파괴 가능 소품(돌기둥·바위 더미). 플레이어·투사체·카메라를 막는 엄폐물.
    /// 적 공격 범위나 적 몸통에 닿으면 부서져 조각 4~6개로 흩어지고 3초 뒤 사라진다.
    /// 조각은 플레이어와 충돌하지 않는다(끼임 방지). 재시작 시에만 복구.
    /// </summary>
    public sealed class DestructibleProp : MonoBehaviour
    {
        /// <summary>조각 전용 레이어. 플레이어(0)와의 충돌을 끈다.</summary>
        public const int DebrisLayer = 9;
        private static readonly List<DestructibleProp> active = new List<DestructibleProp>();

        [SerializeField] private float radius = 0.7f;
        [SerializeField] private float height = 4f;
        [SerializeField] private int chunks = 5;

        public static IReadOnlyList<DestructibleProp> Active => active;
        public bool Broken { get; private set; }
        public float Radius => radius;
        public float Height => height;

        public void Configure(float propRadius, float propHeight, int chunkCount)
        { radius = propRadius; height = propHeight; chunks = Mathf.Clamp(chunkCount, 4, 6); }

        private void OnEnable() { if (!active.Contains(this)) active.Add(this); }
        private void OnDisable() => active.Remove(this);

        /// <summary>점이 이 소품 안(수평 반경·높이)인지. 투사체 차단에 사용.</summary>
        public bool Blocks(Vector3 point)
        {
            if (Broken) return false;
            Vector3 d = Vector3.ProjectOnPlane(point - transform.position, Vector3.up);
            float bottom = transform.position.y - height * 0.5f;
            return d.magnitude <= radius && point.y >= bottom && point.y <= bottom + height;
        }

        public void Break()
        {
            if (Broken) return;
            Broken = true;
            if (Application.isPlaying) { SpawnChunks(); Audio.Sound.Play(Audio.Sfx.PropBreak, transform.position); }
            gameObject.SetActive(false);
        }

        private void SpawnChunks()
        {
            Physics.IgnoreLayerCollision(DebrisLayer, 0, true);
            // S8-1: 보이는 모델은 자식, 크기 기준은 판정용 상자 충돌체.
            var source = GetComponentInChildren<Renderer>();
            var box = GetComponent<BoxCollider>();
            Vector3 size = box != null ? Vector3.Scale(box.size, transform.lossyScale) : transform.lossyScale;
            for (int i = 0; i < chunks; i++)
            {
                var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.name = name + "_Chunk";
                piece.layer = DebrisLayer;
                piece.transform.position = transform.position + new Vector3(Random.Range(-0.3f, 0.3f) * size.x, Random.Range(-0.4f, 0.4f) * size.y, Random.Range(-0.3f, 0.3f) * size.z);
                piece.transform.rotation = Random.rotation;
                piece.transform.localScale = new Vector3(size.x, size.y / chunks * 1.5f, size.z) * Random.Range(0.35f, 0.55f);
                if (source != null) piece.GetComponent<Renderer>().sharedMaterial = source.sharedMaterial;
                var body = piece.AddComponent<Rigidbody>();
                body.mass = 2f;
                Vector3 outward = Vector3.ProjectOnPlane(piece.transform.position - transform.position, Vector3.up).normalized;
                body.AddForce((outward * 3f + Vector3.up * 4f) * body.mass, ForceMode.Impulse);
                body.AddTorque(Random.insideUnitSphere * 6f, ForceMode.Impulse);
                Destroy(piece, 3f);
            }
        }
    }
}
