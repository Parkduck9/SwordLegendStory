using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SwordPrototype.Battle;
using SwordPrototype.Flow;

namespace SwordPrototype.UI
{
    /// <summary>
    /// S8-3 일시정지(Esc): 계속·다시 시작·타이틀로·설정·종료, 설정(카메라·커서 감도, 전체화면).
    /// 실시간에서만 열린다(선택·교환 중 Esc 무시). 시간 정지·마우스 해제는 BattleFlow가 한다.
    /// </summary>
    public sealed class PauseView : MonoBehaviour
    {
        [SerializeField] private BattleFlow flow;
        [SerializeField] private GameObject menu, settings;
        [SerializeField] private Button resume, restart, toTitle, openSettings, quit, closeSettings;
        [SerializeField] private Slider cameraSlider, cursorSlider, masterSlider, musicSlider, effectsSlider;
        [SerializeField] private Toggle fullscreen;
        [SerializeField] private TextMeshProUGUI cameraLabel, cursorLabel, volumeLabel;

        public void Build(BattleFlow battle, InkUISkin skin)
        {
            flow = battle;
            var bg = Ui.Image("Pause", transform, skin.panel, InkUISkin.Paper, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 560));
            menu = bg.gameObject;
            Ui.Text("Title", bg.transform, skin.title, "일시정지", 72, InkUISkin.Ink, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0, -16), new Vector2(480, 90));
            string[] names = { "계속", "다시 시작", "타이틀로", "설정", "종료" };
            var buttons = new Button[5];
            for (int i = 0; i < 5; i++)
                buttons[i] = Ui.Button(names[i], bg.transform, skin, names[i], new Vector2(0.5f, 1f), new Vector2(0, -120 - i * 82), new Vector2(360, 66), 28);
            resume = buttons[0]; restart = buttons[1]; toTitle = buttons[2]; openSettings = buttons[3]; quit = buttons[4];

            var sp = Ui.Image("Settings", transform, skin.panel, InkUISkin.Paper, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 720));
            settings = sp.gameObject;
            Ui.Text("Title", sp.transform, skin.title, "설정", 72, InkUISkin.Ink, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0, -16), new Vector2(560, 90));
            cameraLabel = Ui.Text("CameraLabel", sp.transform, skin.body, "", 24, InkUISkin.Ink, TextAlignmentOptions.Left, new Vector2(0.5f, 1f), new Vector2(0, -122), new Vector2(520, 34));
            cameraSlider = Slider("CameraSlider", sp.transform, skin, new Vector2(0, -164), 0.3f, 3f);
            cursorLabel = Ui.Text("CursorLabel", sp.transform, skin.body, "", 24, InkUISkin.Ink, TextAlignmentOptions.Left, new Vector2(0.5f, 1f), new Vector2(0, -210), new Vector2(520, 34));
            cursorSlider = Slider("CursorSlider", sp.transform, skin, new Vector2(0, -252), 0.03f, 0.2f);
            fullscreen = Toggle(sp.transform, skin, new Vector2(0, -300));
            // S8-4 음량: 전체 · 배경음 · 효과음
            volumeLabel = Ui.Text("VolumeLabel", sp.transform, skin.body, "", 24, InkUISkin.Ink, TextAlignmentOptions.Left, new Vector2(0.5f, 1f), new Vector2(0, -356), new Vector2(520, 34));
            masterSlider = Slider("MasterSlider", sp.transform, skin, new Vector2(0, -398), 0f, 1f);
            musicSlider = Slider("MusicSlider", sp.transform, skin, new Vector2(0, -446), 0f, 1f);
            effectsSlider = Slider("EffectsSlider", sp.transform, skin, new Vector2(0, -494), 0f, 1f);
            Ui.Text("MasterName", sp.transform, skin.body, "전체", 20, InkUISkin.Ink, TextAlignmentOptions.Right, new Vector2(0.5f, 1f), new Vector2(-300, -398), new Vector2(70, 28));
            Ui.Text("MusicName", sp.transform, skin.body, "배경음", 20, InkUISkin.Ink, TextAlignmentOptions.Right, new Vector2(0.5f, 1f), new Vector2(-300, -446), new Vector2(70, 28));
            Ui.Text("EffectsName", sp.transform, skin.body, "효과음", 20, InkUISkin.Ink, TextAlignmentOptions.Right, new Vector2(0.5f, 1f), new Vector2(-300, -494), new Vector2(70, 28));
            closeSettings = Ui.Button("Close", sp.transform, skin, "저장하고 돌아가기", new Vector2(0.5f, 0f), new Vector2(0, 36), new Vector2(360, 64), 26);
            menu.SetActive(false);
            settings.SetActive(false);
        }

        private static Slider Slider(string name, Transform parent, InkUISkin skin, Vector2 position, float min, float max)
        {
            var rt = Ui.Rect(name, parent, new Vector2(0.5f, 1f), position, new Vector2(520, 28));
            Ui.Image("Track", rt, skin.brush, InkUISkin.Faded, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 14));
            var handleArea = Ui.Fill("HandleArea", rt);
            var handle = Ui.Image("Handle", handleArea, skin.seal, InkUISkin.Seal, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, 30));
            handle.raycastTarget = true;
            var slider = rt.gameObject.AddComponent<Slider>();
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = min; slider.maxValue = max;
            return slider;
        }

        private static Toggle Toggle(Transform parent, InkUISkin skin, Vector2 position)
        {
            var rt = Ui.Rect("Fullscreen", parent, new Vector2(0.5f, 1f), position, new Vector2(520, 40));
            var box = Ui.Image("Box", rt, skin.panel, InkUISkin.Paper, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(40, 40));
            box.raycastTarget = true;
            var check = Ui.Image("Check", box.transform, skin.seal, InkUISkin.Seal, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26, 26));
            Ui.Text("Label", rt, skin.body, "전체화면", 24, InkUISkin.Ink, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(56, 0), new Vector2(400, 40));
            var toggle = rt.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = box;
            toggle.graphic = check;
            return toggle;
        }

        private void Awake()
        {
            Ui.OnClick(resume, Close);
            Ui.OnClick(restart, () => flow.Restart());
            Ui.OnClick(toTitle, () => flow.GoToTitle(false));
            Ui.OnClick(openSettings, () => { settings.SetActive(true); menu.SetActive(false); });
            Ui.OnClick(closeSettings, () => { GameSettings.Save(); settings.SetActive(false); menu.SetActive(true); });
            Ui.OnClick(quit, Quit);
            cameraSlider.onValueChanged.AddListener(v => GameSettings.CameraSensitivity = v);
            cursorSlider.onValueChanged.AddListener(v => GameSettings.CursorSensitivity = v);
            fullscreen.onValueChanged.AddListener(v => GameSettings.Fullscreen = v);
            masterSlider.onValueChanged.AddListener(v => GameSettings.MasterVolume = v);
            musicSlider.onValueChanged.AddListener(v => GameSettings.MusicVolume = v);
            effectsSlider.onValueChanged.AddListener(v => GameSettings.EffectsVolume = v);
        }

        private void Update()
        {
            if (flow == null) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (flow.Paused) Close();
                else if (flow.CanPause)
                {
                    flow.SetPaused(true);
                    cameraSlider.SetValueWithoutNotify(GameSettings.CameraSensitivity);
                    cursorSlider.SetValueWithoutNotify(GameSettings.CursorSensitivity);
                    fullscreen.SetIsOnWithoutNotify(GameSettings.Fullscreen);
                    masterSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
                    musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
                    effectsSlider.SetValueWithoutNotify(GameSettings.EffectsVolume);
                    menu.SetActive(true);
                }
            }
            if (!flow.Paused && (menu.activeSelf || settings.activeSelf)) { menu.SetActive(false); settings.SetActive(false); }
            if (settings.activeSelf)
            {
                cameraLabel.text = $"카메라 감도  {GameSettings.CameraSensitivity:0.0}";
                cursorLabel.text = $"선택 화면 커서 감도  {GameSettings.CursorSensitivity:0.00}";
                volumeLabel.text = $"음량  전체 {GameSettings.MasterVolume * 100f:0} · 배경음 {GameSettings.MusicVolume * 100f:0} · 효과음 {GameSettings.EffectsVolume * 100f:0}";
            }
        }

        private void Close()
        {
            if (settings.activeSelf) GameSettings.Save();
            menu.SetActive(false);
            settings.SetActive(false);
            flow.SetPaused(false);
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
