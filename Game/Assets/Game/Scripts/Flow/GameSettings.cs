using UnityEngine;

namespace SwordPrototype.Flow
{
    /// <summary>S7 최소 설정 + S8-4 음량: 카메라 감도 · 선택 화면 커서 감도 · 전체화면 · 전체/배경음/효과음 음량. 이 기기에 저장.</summary>
    public static class GameSettings
    {
        public const float DefaultCursorSensitivity = 0.08f;
        private const string CameraKey = "검협전.CameraSensitivity";
        private const string CursorKey = "검협전.CursorSensitivity";
        private const string MasterKey = "검협전.MasterVolume";
        private const string MusicKey = "검협전.MusicVolume";
        private const string EffectsKey = "검협전.EffectsVolume";
        private static bool loaded;
        private static float cameraSensitivity = 1f;
        private static float cursorSensitivity = DefaultCursorSensitivity;
        private static float master = 0.8f, music = 0.6f, effects = 0.9f;

        public static float CameraSensitivity { get { Load(); return cameraSensitivity; } set { Load(); cameraSensitivity = Mathf.Clamp(value, 0.3f, 3f); } }
        public static float CursorSensitivity { get { Load(); return cursorSensitivity; } set { Load(); cursorSensitivity = Mathf.Clamp(value, 0.03f, 0.2f); } }
        public static float MasterVolume { get { Load(); return master; } set { Load(); master = Mathf.Clamp01(value); } }
        public static float MusicVolume { get { Load(); return music; } set { Load(); music = Mathf.Clamp01(value); } }
        public static float EffectsVolume { get { Load(); return effects; } set { Load(); effects = Mathf.Clamp01(value); } }
        public static bool Fullscreen { get => Screen.fullScreen; set => Screen.fullScreen = value; }

        // 값을 바꾸기 전에도 먼저 불러온다(안 그러면 첫 읽기 때 저장값이 방금 바꾼 값을 덮어씀).
        private static void Load()
        {
            if (loaded) return;
            loaded = true;
            try
            {
                cameraSensitivity = PlayerPrefs.GetFloat(CameraKey, 1f);
                cursorSensitivity = PlayerPrefs.GetFloat(CursorKey, DefaultCursorSensitivity);
                master = PlayerPrefs.GetFloat(MasterKey, 0.8f);
                music = PlayerPrefs.GetFloat(MusicKey, 0.6f);
                effects = PlayerPrefs.GetFloat(EffectsKey, 0.9f);
            }
            catch { }
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat(CameraKey, CameraSensitivity);
            PlayerPrefs.SetFloat(CursorKey, CursorSensitivity);
            PlayerPrefs.SetFloat(MasterKey, MasterVolume);
            PlayerPrefs.SetFloat(MusicKey, MusicVolume);
            PlayerPrefs.SetFloat(EffectsKey, EffectsVolume);
            PlayerPrefs.Save();
        }
    }
}
