using TMPro;
using UnityEngine;

namespace SwordPrototype.UI
{
    /// <summary>S8-3 조작 안내: 처음 전투장에 들어왔을 때만 보이고, F1로 다시 열고 닫는다.</summary>
    public sealed class ControlsHintView : MonoBehaviour
    {
        private const string SeenKey = "검협전.ControlsHintSeen";
        public static readonly string[] Lines =
        {
            "WASD 이동 · 마우스 시점 · 휠 클릭 락온",
            "좌클릭 공격 (3m 안: 방향 선택 / 밖: 허공 베기)",
            "Space 점프 · Shift 대쉬",
            "우클릭: 가드(체력 3) · 선택 중 지나가며 베기(특수기 2)",
            "Esc 일시정지 · F1 이 안내 열기/닫기",
        };

        [SerializeField] private GameObject panel;

        public void Build(InkUISkin skin)
        {
            var bg = Ui.Image("ControlsHint", transform, skin.panel, InkUISkin.PaperDim, new Vector2(1f, 1f), new Vector2(-32, -32), new Vector2(660, 84 + Lines.Length * 38));   // 오른쪽 위(적 체력바와 겹치지 않게)
            panel = bg.gameObject;
            Ui.Text("Title", bg.transform, skin.title, "조작", 40, InkUISkin.Ink, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(26, -10), new Vector2(300, 50));
            for (int i = 0; i < Lines.Length; i++)
                Ui.Text("Line" + i, bg.transform, skin.body, Lines[i], 22, InkUISkin.Ink, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(26, -60 - i * 38), new Vector2(600, 34));
        }

        private void Start()
        {
            bool seen = false;
            try { seen = PlayerPrefs.GetInt(SeenKey, 0) == 1; PlayerPrefs.SetInt(SeenKey, 1); } catch { }
            panel.SetActive(!seen);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) panel.SetActive(!panel.activeSelf);
        }
    }
}
