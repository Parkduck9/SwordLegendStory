using UnityEngine;

namespace SwordPrototype.Audio
{
    /// <summary>
    /// S8-4 소리 재생 한 곳: 효과음(2D/위치 3D), 배경음 반복, 분류별 음량(설정), 선택 화면 동안 배경음 먹먹하게.
    /// 장면마다 하나. 다른 코드는 Sound.Play(...)로 부른다(없으면 조용히 무시 — 블록 인형 테스트 등).
    /// </summary>
    public sealed class AudioService : MonoBehaviour
    {
        private const int Voices = 16;
        [SerializeField] private SoundBank bank;
        [SerializeField] private string music;
        private AudioSource[] voices;
        private AudioSource musicSource;
        private AudioLowPassFilter muffle;
        private int next;
        private float musicDuck = 1f;

        public static AudioService Instance { get; private set; }
        /// <summary>선택 화면 등에서 켜면 배경음이 먹먹해진다.</summary>
        public bool Muffled { get; set; }
        /// <summary>결과 화면 등에서 배경음을 줄인다(0~1).</summary>
        public float MusicDuck { get => musicDuck; set => musicDuck = Mathf.Clamp01(value); }

        public void Configure(SoundBank soundBank, string musicName) { bank = soundBank; music = musicName; }

        private void Awake()
        {
            Instance = this;
            voices = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                var go = new GameObject("Voice" + i);
                go.transform.SetParent(transform, false);
                voices[i] = go.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].rolloffMode = AudioRolloffMode.Linear;
                voices[i].minDistance = 4f;
                voices[i].maxDistance = 45f;
            }
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.ignoreListenerPause = true;
            muffle = gameObject.AddComponent<AudioLowPassFilter>();
            muffle.cutoffFrequency = 22000f;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Start()
        {
            var entry = bank != null && !string.IsNullOrEmpty(music) ? bank.Find(music) : null;
            if (entry != null && entry.clips.Length > 0)
            {
                musicSource.clip = entry.clips[0];
                musicSource.Play();
            }
        }

        private void Update()
        {
            var entry = bank != null ? bank.Find(music) : null;
            float baseVolume = entry != null ? entry.volume : 0.5f;
            musicSource.volume = baseVolume * musicDuck * Volume(SoundCategory.Music);
            float target = Muffled ? 900f : 22000f;
            muffle.cutoffFrequency = Mathf.Lerp(muffle.cutoffFrequency, target, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
        }

        /// <summary>position이 있으면 그 위치에서(입체음 60%), 없으면 화면 소리로.</summary>
        public void Play(string soundName, Vector3? position = null, float pitchScale = 1f, float volumeScale = 1f)
        {
            var entry = bank != null ? bank.Find(soundName) : null;
            if (entry == null || entry.clips == null || entry.clips.Length == 0) return;
            var source = voices[next];
            next = (next + 1) % voices.Length;
            source.Stop();
            source.clip = entry.clips[Random.Range(0, entry.clips.Length)];
            source.pitch = Random.Range(entry.pitchMin, entry.pitchMax) * pitchScale;
            source.volume = entry.volume * volumeScale * Volume(entry.category);
            source.spatialBlend = position.HasValue ? 0.6f : 0f;
            source.transform.position = position ?? Vector3.zero;
            source.ignoreListenerPause = entry.category == SoundCategory.Interface;
            source.Play();
        }

        private static float Volume(SoundCategory category)
        {
            float master = Flow.GameSettings.MasterVolume;
            return master * (category == SoundCategory.Music ? Flow.GameSettings.MusicVolume : Flow.GameSettings.EffectsVolume);
        }
    }

    /// <summary>짧은 호출용: Sound.Play("Hit", 위치).</summary>
    public static class Sound
    {
        public static void Play(string name, Vector3? position = null, float pitchScale = 1f, float volumeScale = 1f)
        {
            if (AudioService.Instance != null) AudioService.Instance.Play(name, position, pitchScale, volumeScale);
        }
    }
}
