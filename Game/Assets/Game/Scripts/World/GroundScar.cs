using UnityEngine;

namespace SwordPrototype.World
{
    /// <summary>S6: 도약 내려찍기 착지점의 금 간 자국(바닥 표시만, 충돌 없음). 30초 뒤 사라지며 마지막 2초 동안 옅어진다.</summary>
    public sealed class GroundScar : MonoBehaviour
    {
        public const float LifeSeconds = 30f;
        private const float FadeSeconds = 2f;
        private Material material;
        private float age;

        public static void Spawn(Vector3 center, float radius)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "GroundScar";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = new Vector3(center.x, 0.012f, center.z);
            go.transform.localScale = new Vector3(radius * 2f, 0.003f, radius * 2f);
            var scar = go.AddComponent<GroundScar>();
            scar.material = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.08f, 0.07f, 0.06f, 0.55f) };
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = scar.material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // 금: 가는 막대 몇 개를 방사형으로
            for (int i = 0; i < 6; i++)
            {
                var crack = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(crack.GetComponent<Collider>());
                crack.transform.SetParent(go.transform, false);
                crack.transform.localRotation = Quaternion.Euler(0f, i * 60f + 15f, 0f);
                crack.transform.localPosition = crack.transform.localRotation * new Vector3(0f, 0f, 0.3f);
                crack.transform.localScale = new Vector3(0.008f, 1f, 0.45f);
                crack.GetComponent<Renderer>().sharedMaterial = scar.material;
            }
        }

        private void Update()
        {
            age += Time.deltaTime;
            float left = LifeSeconds - age;
            if (left <= 0f) { Destroy(gameObject); return; }
            if (left < FadeSeconds) { Color c = material.color; c.a = 0.55f * left / FadeSeconds; material.color = c; }
        }

        private void OnDestroy() { if (material != null) Destroy(material); }
    }
}
