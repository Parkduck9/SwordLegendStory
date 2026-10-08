using TMPro;
using UnityEngine;

namespace SwordPrototype.UI
{
    /// <summary>S8-3 수묵 UI 공용 자원(글꼴·스프라이트)과 색. 에디터 생성기가 에셋으로 만든다.</summary>
    [CreateAssetMenu(menuName = "검협전/Ink UI Skin")]
    public sealed class InkUISkin : ScriptableObject
    {
        public TMP_FontAsset body;     // Noto Sans KR (OFL)
        public TMP_FontAsset title;    // 나눔손글씨 붓 (OFL)
        public Sprite panel;           // 한지 판넬(9-slice, 먹 테두리)
        public Sprite brush;           // 붓 획(체력바)
        public Sprite circle;          // 먹 원(쿨타임)
        public Sprite seal;            // 붉은 낙관
        public static readonly Color Paper = new Color(0.91f, 0.88f, 0.81f);
        public static readonly Color PaperDim = new Color(0.91f, 0.88f, 0.81f, 0.92f);
        public static readonly Color Ink = new Color(0.12f, 0.11f, 0.10f);
        public static readonly Color InkSoft = new Color(0.12f, 0.11f, 0.10f, 0.78f);
        public static readonly Color Faded = new Color(0.55f, 0.52f, 0.47f);
        public static readonly Color Seal = new Color(0.70f, 0.15f, 0.12f);
        // 사용자 지정 반복 색(노랑·파랑·빨강)을 수묵 톤으로 채도만 낮춘 것
        public static readonly Color RepeatYellow = new Color(0.86f, 0.70f, 0.28f);
        public static readonly Color RepeatBlue = new Color(0.30f, 0.50f, 0.78f);
        public static readonly Color RepeatRed = new Color(0.78f, 0.24f, 0.20f);

        public static Color Repeat(int count) => count >= 3 ? RepeatRed : count == 2 ? RepeatBlue : RepeatYellow;
    }
}
