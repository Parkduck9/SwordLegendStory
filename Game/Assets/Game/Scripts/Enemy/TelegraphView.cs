using UnityEngine;

namespace SwordPrototype.Enemy
{
    /// <summary>바닥 붉은 예고 범위(임시 메시). AttackShape 값을 그대로 그려 판정과 일치시킨다.</summary>
    public sealed class TelegraphView : MonoBehaviour
    {
        private const int Segments = 32;
        private Mesh mesh;
        private MeshRenderer view;
        private Material material;

        private void Awake()
        {
            mesh = new Mesh { name = "Telegraph" };
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            view = gameObject.AddComponent<MeshRenderer>();
            material = new Material(Shader.Find("Sprites/Default"));
            view.sharedMaterial = material;
            view.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            view.receiveShadows = false;
            view.enabled = false;
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            if (material != null) Destroy(material);
        }

        /// <summary>progress 0→1: 예고 진행도. locked면 더 진하게 표시(마지막 고정 구간).</summary>
        public void Show(AttackShape shape, Vector3 origin, Vector3 forward, float progress, bool locked)
        {
            Vector3 f = Vector3.ProjectOnPlane(forward, Vector3.up);
            Quaternion rot = f.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(f) : Quaternion.identity;
            Vector3 o = new Vector3(origin.x, 0.03f, origin.z);
            mesh.Clear();
            switch (shape.kind)
            {
                case ShapeKind.Line:
                {
                    float w = shape.width * 0.5f;
                    mesh.vertices = new[] { o + rot * new Vector3(-w, 0, 0), o + rot * new Vector3(w, 0, 0), o + rot * new Vector3(w, 0, shape.length), o + rot * new Vector3(-w, 0, shape.length) };
                    mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                    break;
                }
                default:
                {
                    bool fan = shape.kind == ShapeKind.Fan;
                    float half = fan ? shape.angle * 0.5f : 180f;
                    var verts = new Vector3[Segments + 2];
                    var tris = new int[Segments * 3];
                    verts[0] = o;
                    for (int i = 0; i <= Segments; i++)
                    {
                        float a = Mathf.Lerp(-half, half, i / (float)Segments) * Mathf.Deg2Rad;
                        verts[i + 1] = o + rot * new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * shape.radius;
                    }
                    for (int i = 0; i < Segments; i++) { tris[i * 3] = 0; tris[i * 3 + 1] = i + 1; tris[i * 3 + 2] = i + 2; }
                    mesh.vertices = verts;
                    mesh.triangles = tris;
                    break;
                }
            }
            mesh.RecalculateBounds();
            material.color = new Color(1f, 0.15f, 0.1f, locked ? 0.6f : Mathf.Lerp(0.15f, 0.4f, progress));
            view.enabled = true;
        }

        public void Hide() { if (view != null) view.enabled = false; }
    }
}
