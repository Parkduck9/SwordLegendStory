using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SwordPrototype.UI
{
    /// <summary>
    /// S8-3: 화면 요소를 코드로 만드는 도구. 각 화면의 Build()가 에디터에서 호출돼 장면에 저장된다.
    /// 위치는 anchor(0~1) 기준점 + 픽셀 오프셋(1920×1080 기준).
    /// </summary>
    public static class Ui
    {
        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>부모를 가득 채우는 영역.</summary>
        public static RectTransform Fill(string name, Transform parent)
        {
            var rt = Rect(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            return rt;
        }

        public static Image Image(string name, Transform parent, Sprite sprite, Color color, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var image = Rect(name, parent, anchor, position, size).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            if (sprite != null && sprite.border != Vector4.zero) image.type = UnityEngine.UI.Image.Type.Sliced;
            image.raycastTarget = false;
            return image;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, TMP_FontAsset font, string text, float size, Color color,
            TextAlignmentOptions align, Vector2 anchor, Vector2 position, Vector2 box)
        {
            var t = Rect(name, parent, anchor, position, box).gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        public static Button Button(string name, Transform parent, InkUISkin skin, string label, Vector2 anchor, Vector2 position, Vector2 size, float fontSize = 30f)
        {
            var image = Image(name, parent, skin.panel, InkUISkin.Paper, anchor, position, size);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.86f, 0.82f);
            colors.pressedColor = new Color(0.85f, 0.6f, 0.55f);
            colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);
            button.colors = colors;
            var text = Text("Label", image.transform, skin.body, label, fontSize, InkUISkin.Ink, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return button;
        }

        /// <summary>전투 화면 글자: 굵게 해서 먹 번짐 배경 위에서 또렷하게(테두리 재질은 작은 글자를 뭉개 사용하지 않음).</summary>
        public static TextMeshProUGUI Readable(TextMeshProUGUI text, InkUISkin skin)
        {
            text.fontStyle |= FontStyles.Bold;
            return text;
        }

        /// <summary>런타임 연결용: 버튼 클릭에 동작을 붙인다(중복 방지 위해 먼저 비움).</summary>
        public static void OnClick(Button button, UnityAction action)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => Audio.Sound.Play(Audio.Sfx.UiClick));   // S8-4 버튼 소리
            button.onClick.AddListener(action);
        }

        public static string Label(Button button) => button.GetComponentInChildren<TextMeshProUGUI>().text;
        public static void SetLabel(Button button, string text) => button.GetComponentInChildren<TextMeshProUGUI>().text = text;
    }
}
