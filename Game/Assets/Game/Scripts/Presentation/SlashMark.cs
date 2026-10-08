using UnityEngine;

namespace SwordPrototype.Presentation
{
    /// <summary>
    /// R6: 베인 자국. 칼이 지나간 방향으로 몸에 남는 먹빛 붓 획(가운데 굵고 양끝 가늘게). 뼈에 붙어 함께 움직이고 서서히 사라진다.
    /// 단계 0 스침(얇고 짧게) · 1 베임 · 2 깊게(굵고 길게).
    /// </summary>
    public sealed class SlashMark : MonoBehaviour
    {
        private static readonly float[] Width = { 0.022f, 0.045f, 0.07f };
        private static readonly float[] Length = { 0.34f, 0.48f, 0.6f };
        private static readonly float[] Seconds = { 1.5f, 2.5f, 3.2f };
        private static readonly Color Ink = new Color(0.16f, 0.03f, 0.03f, 0.95f);   // 붉은 기 도는 먹
        private static Material material;

        private LineRenderer line;
        private float life, total;

        /// <param name="anchor">따라 움직일 뼈(가슴 등).</param>
        /// <param name="center">자국 가운데(월드, 몸 겉면).</param>
        /// <param name="cut">칼이 지나간 월드 방향.</param>
        /// <param name="scale">캐릭터 크기 배율.</param>
        public static SlashMark Spawn(Transform anchor, Vector3 center, Vector3 cut, int tier, float scale)
        {
            tier = Mathf.Clamp(tier, 0, 2);
            if (cut.sqrMagnitude < 0.0001f) cut = Vector3.right;
            cut.Normalize();
            if (material == null) material = new Material(Shader.Find("Sprites/Default"));
            var go = new GameObject("SlashMark");
            go.transform.SetParent(anchor, true);
            var mark = go.AddComponent<SlashMark>();
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.sharedMaterial = material;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.numCapVertices = 2;
            line.widthMultiplier = Width[tier] * scale;
            line.widthCurve = new AnimationCurve(new Keyframe(0f, 0.15f), new Keyframe(0.4f, 1f), new Keyframe(1f, 0.1f));
            float half = Length[tier] * scale * 0.5f;
            line.positionCount = 3;
            line.SetPosition(0, go.transform.InverseTransformPoint(center - cut * half));
            line.SetPosition(1, go.transform.InverseTransformPoint(center));
            line.SetPosition(2, go.transform.InverseTransformPoint(center + cut * half));
            line.startColor = line.endColor = Ink;
            mark.line = line;
            mark.life = mark.total = Seconds[tier];
            return mark;
        }

        private void Update()
        {
            life -= Battle.BattleClock.RealDelta;
            if (life <= 0f) { Destroy(gameObject); return; }
            // 마지막 40% 동안 옅어짐
            float a = Ink.a * Mathf.Clamp01(life / (total * 0.4f));
            line.startColor = line.endColor = new Color(Ink.r, Ink.g, Ink.b, a);
        }
    }
}
