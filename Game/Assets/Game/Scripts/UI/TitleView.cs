using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SwordPrototype.Battle;
using SwordPrototype.Flow;

namespace SwordPrototype.UI
{
    /// <summary>
    /// S8-3 타이틀 장면(수묵): 붓글씨 "검협전" + 낙관 · 조작법 · 스탯 배분(먹점 ●○, −/+, 설명, 다음 레벨, 프리셋, 확인 창).
    /// 규칙은 StatAllocation·StatDescriber·RunSetup 그대로(기존 IMGUI TitleMenu와 같은 동작).
    /// </summary>
    public sealed class TitleView : MonoBehaviour
    {
        public const string GameTitle = "검협전";   // 가제(10차 결정)

        [SerializeField] private StatTuning tuning = new StatTuning();
        [SerializeField] private GameObject titlePage, controlsPage, allocPage, confirmPage;
        [SerializeField] private Button start, controls, quit, controlsBack, back, depart, clear, confirmGo, confirmCancel;
        [SerializeField] private Button[] presets = new Button[3];
        [SerializeField] private Button[] minus = new Button[5], plus = new Button[5];
        [SerializeField] private TextMeshProUGUI[] dots = new TextMeshProUGUI[5], descriptions = new TextMeshProUGUI[5];
        [SerializeField] private TextMeshProUGUI remain, hoverText, confirmText;

        private StatAllocation allocation;

        public void Build(InkUISkin skin)
        {
            // 타이틀
            titlePage = Ui.Fill("TitlePage", transform).gameObject;
            Ui.Text("GameTitle", titlePage.transform, skin.title, GameTitle, 260, InkUISkin.Ink, TextAlignmentOptions.Center, new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(1200, 320));
            var seal = Ui.Image("Seal", titlePage.transform, skin.seal, InkUISkin.Seal, new Vector2(0.5f, 0.68f), new Vector2(330, -90), new Vector2(96, 96));
            Ui.Text("SealText", seal.transform, skin.title, "검", 64, InkUISkin.Paper, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96, 96));
            start = Ui.Button("Start", titlePage.transform, skin, "시작", new Vector2(0.5f, 0.36f), Vector2.zero, new Vector2(320, 72), 32);
            controls = Ui.Button("Controls", titlePage.transform, skin, "조작법", new Vector2(0.5f, 0.36f), new Vector2(0, -90), new Vector2(320, 72), 32);
            quit = Ui.Button("Quit", titlePage.transform, skin, "종료", new Vector2(0.5f, 0.36f), new Vector2(0, -180), new Vector2(320, 72), 32);

            // 조작법
            var cp = Ui.Image("ControlsPage", transform, skin.panel, InkUISkin.Paper, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 640));
            controlsPage = cp.gameObject;
            Ui.Text("Title", cp.transform, skin.title, "조작법", 80, InkUISkin.Ink, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0, -16), new Vector2(900, 100));
            string[] lines =
            {
                "WASD 이동 · 마우스 시점 · 휠 클릭 락온",
                "좌클릭 공격 (3m 안: 방향 선택 / 밖: 허공 베기) · 방향 예약",
                "선택 화면 가운데 원 좌클릭: 비도(기력 3)",
                "우클릭: 가드 패링(체력 3) · 선택 화면에서 지나가며 베기(특수기 2)",
                "Shift 대쉬 · Space 점프 · Esc 일시정지 · F1 조작 안내",
                "R: 결과 화면에서 다시 하기",
            };
            for (int i = 0; i < lines.Length; i++)
                Ui.Text("Line" + i, cp.transform, skin.body, lines[i], 26, InkUISkin.Ink, TextAlignmentOptions.Left, new Vector2(0.5f, 1f), new Vector2(0, -140 - i * 56), new Vector2(880, 50));
            controlsBack = Ui.Button("Back", cp.transform, skin, "뒤로", new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(240, 64), 28);

            // 스탯 배분
            var ap = Ui.Image("AllocPage", transform, skin.panel, InkUISkin.Paper, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 860));
            allocPage = ap.gameObject;
            Ui.Text("Title", ap.transform, skin.title, "스탯 배분", 76, InkUISkin.Ink, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(50, -14), new Vector2(600, 100));
            remain = Ui.Text("Remain", ap.transform, skin.body, "", 30, InkUISkin.Seal, TextAlignmentOptions.Right, new Vector2(1f, 1f), new Vector2(-50, -40), new Vector2(600, 50));
            for (int i = 0; i < 5; i++)
            {
                float y = -150 - i * 100;
                Ui.Text("Name" + i, ap.transform, skin.body, StatAllocation.Names[i], 34, InkUISkin.Ink, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(60, y), new Vector2(160, 60));
                dots[i] = Ui.Text("Dots" + i, ap.transform, skin.body, "", 40, InkUISkin.Ink, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(220, y), new Vector2(160, 60));
                minus[i] = Ui.Button("Minus" + i, ap.transform, skin, "−", new Vector2(0f, 1f), new Vector2(390, y), new Vector2(64, 60), 36);
                plus[i] = Ui.Button("Plus" + i, ap.transform, skin, "+", new Vector2(0f, 1f), new Vector2(466, y), new Vector2(64, 60), 36);
                plus[i].gameObject.AddComponent<HoverHint>();
                descriptions[i] = Ui.Text("Desc" + i, ap.transform, skin.body, "", 24, InkUISkin.Ink, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(560, y + 6), new Vector2(800, 72));
            }
            hoverText = Ui.Text("Hover", ap.transform, skin.body, "", 24, InkUISkin.Faded, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(60, -655), new Vector2(1280, 40));
            for (int i = 0; i < presets.Length; i++)
            {
                var p = StatAllocation.Presets[i];
                presets[i] = Ui.Button("Preset" + i, ap.transform, skin, $"{p.name} {string.Join("·", p.levels)}", new Vector2(0f, 0f), new Vector2(60 + i * 245, 40), new Vector2(230, 64), 24);
            }
            clear = Ui.Button("Clear", ap.transform, skin, "초기화", new Vector2(0f, 0f), new Vector2(60 + 3 * 245, 40), new Vector2(140, 64), 24);
            back = Ui.Button("Back", ap.transform, skin, "뒤로", new Vector2(1f, 0f), new Vector2(-240, 40), new Vector2(160, 64), 28);
            depart = Ui.Button("Depart", ap.transform, skin, "출발", new Vector2(1f, 0f), new Vector2(-50, 40), new Vector2(180, 64), 30);

            // 남은 포인트 확인 창
            var cf = Ui.Image("ConfirmPage", transform, skin.panel, InkUISkin.Paper, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640, 260));
            confirmPage = cf.gameObject;
            confirmText = Ui.Text("Text", cf.transform, skin.body, "", 30, InkUISkin.Ink, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(580, 90));
            confirmGo = Ui.Button("Go", cf.transform, skin, "출발", new Vector2(0.5f, 0f), new Vector2(-120, 36), new Vector2(200, 64), 28);
            confirmCancel = Ui.Button("Cancel", cf.transform, skin, "취소", new Vector2(0.5f, 0f), new Vector2(120, 36), new Vector2(200, 64), 28);
        }

        private void Awake()
        {
            Ui.OnClick(start, () => Show(allocPage));
            Ui.OnClick(controls, () => Show(controlsPage));
            Ui.OnClick(quit, Quit);
            Ui.OnClick(controlsBack, () => Show(titlePage));
            Ui.OnClick(back, () => Show(titlePage));
            Ui.OnClick(clear, () => allocation.Clear());
            for (int i = 0; i < 5; i++)
            {
                var kind = (StatKind)i;
                Ui.OnClick(minus[i], () => allocation.Decrease(kind));
                Ui.OnClick(plus[i], () => allocation.Increase(kind));
            }
            for (int i = 0; i < presets.Length; i++) { int index = i; Ui.OnClick(presets[i], () => allocation.ApplyPreset(index)); }
            Ui.OnClick(depart, () => { if (allocation.Remaining > 0) confirmPage.SetActive(true); else Depart(); });
            Ui.OnClick(confirmGo, Depart);
            Ui.OnClick(confirmCancel, () => confirmPage.SetActive(false));
        }

        private void Start()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            allocation = new StatAllocation(RunSetup.LoadLast(new CharacterStats(2, 2, 2, 2, 2)));
            if (RunSetup.OpenAllocation) { RunSetup.OpenAllocation = false; Show(allocPage); }
            else Show(titlePage);
        }

        private void Show(GameObject page)
        {
            titlePage.SetActive(page == titlePage);
            controlsPage.SetActive(page == controlsPage);
            allocPage.SetActive(page == allocPage);
            confirmPage.SetActive(false);
        }

        private void Update()
        {
            if (allocation == null || !allocPage.activeSelf) return;
            remain.text = allocation.Hardcore ? "0포인트 · 하드코어" : $"남은 포인트 {allocation.Remaining} / {CharacterStats.TotalPoints}";
            for (int i = 0; i < 5; i++)
            {
                var kind = (StatKind)i;
                int level = allocation[kind];
                dots[i].text = new string('●', level) + new string('○', CharacterStats.MaxLevel - level);
                descriptions[i].text = StatDescriber.Describe(kind, level, tuning);
                minus[i].interactable = allocation.CanDecrease(kind);
                plus[i].interactable = allocation.CanIncrease(kind);
                plus[i].GetComponent<HoverHint>().hint = StatDescriber.DescribeNext(kind, level, tuning);
            }
            hoverText.text = HoverHint.Current ?? "+ 버튼에 마우스를 올리면 다음 레벨 변화가 보입니다.";
            confirmText.text = $"남은 포인트 {allocation.Remaining} — 그대로 시작할까요?";
        }

        private void Depart()
        {
            RunSetup.Select(allocation.ToStats());
            SceneManager.LoadScene(RunSetup.ArenaScene);
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
