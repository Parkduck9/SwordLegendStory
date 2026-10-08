using UnityEngine;
using UnityEngine.UI;

namespace SwordPrototype.UI
{
    /// <summary>
    /// S8-3 선택 휠: 원형 8칸(먹 고리)과 바깥 시간 고리를 메시로 그린다.
    /// 칸 0 = 위(↑), 시계 방향(AttackDirection 순서와 같음). 시간 고리는 위에서 시작해 시계 방향으로 줄어든다.
    /// </summary>
    public sealed class WheelGraphic : MaskableGraphic
    {
        private const int StepsPerSegment = 12;
        public float innerRadius = 95f;
        public float outerRadius = 230f;
        public float gapDegrees = 2.5f;
        public float timerInner = 244f;
        public float timerOuter = 258f;
        public Color timerColor = InkUISkin.Seal;
        public Color timerTrack = new Color(0.78f, 0.74f, 0.66f, 0.6f);
        [Range(0f, 1f)] public float timer = 1f;
        public Color[] segmentColors = new Color[8];
        public Color[] bandColors = new Color[8];   // 반복 색 띠(투명이면 그리지 않음)

        public void SetSegment(int index, Color fill, Color band)
        {
            segmentColors[index] = fill;
            bandColors[index] = band;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            for (int i = 0; i < 8; i++)
            {
                float start = i * 45f - 22.5f + gapDegrees * 0.5f, end = i * 45f + 22.5f - gapDegrees * 0.5f;
                Arc(vh, innerRadius, outerRadius, start, end, segmentColors[i]);
                if (bandColors[i].a > 0.01f) Arc(vh, outerRadius - 12f, outerRadius, start, end, bandColors[i]);
            }
            Arc(vh, timerInner, timerOuter, 0f, 360f, timerTrack);
            if (timer > 0.001f) Arc(vh, timerInner, timerOuter, 0f, 360f * timer, timerColor);
        }

        // 각도는 위(12시)=0°, 시계 방향 증가
        private static void Arc(VertexHelper vh, float r0, float r1, float fromDeg, float toDeg, Color color)
        {
            int steps = Mathf.Max(2, Mathf.CeilToInt((toDeg - fromDeg) / 45f * StepsPerSegment));
            int baseIndex = vh.currentVertCount;
            for (int s = 0; s <= steps; s++)
            {
                float a = Mathf.Lerp(fromDeg, toDeg, s / (float)steps) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
                vh.AddVert(dir * r0, color, Vector2.zero);
                vh.AddVert(dir * r1, color, Vector2.zero);
            }
            for (int s = 0; s < steps; s++)
            {
                int k = baseIndex + s * 2;
                vh.AddTriangle(k, k + 1, k + 3);
                vh.AddTriangle(k, k + 3, k + 2);
            }
        }
    }
}
