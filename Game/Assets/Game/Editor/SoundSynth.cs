using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SwordPrototype.Editor
{
    /// <summary>
    /// S8-4: 외부 음원 없이 코드로 합성하는 소리(WAV, 44.1kHz 모노). 고정 시드라 매번 같은 결과.
    /// 베기 바람·대쉬, 적 예고음 5종(서로 다른 음색·리듬), 큰 북, 승리·패배 가락, 배경음 2곡(오음계, 끊김 없이 반복).
    /// </summary>
    public static class SoundSynth
    {
        public const string Folder = "Assets/Game/Art/Audio/Generated";
        private const int Rate = 44100;
        // 오음계(궁상각치우) — D 기준: D E F# A B
        private static readonly float[] Pentatonic = { 146.83f, 164.81f, 185.00f, 220.00f, 246.94f, 293.66f, 329.63f, 369.99f, 440.00f, 493.88f, 587.33f };

        /// <summary>생성한 소리 이름 → 경로. SoundBank가 이 이름으로 연결한다.</summary>
        public static Dictionary<string, string> GenerateAll()
        {
            Directory.CreateDirectory(Folder);
            var made = new Dictionary<string, string>();
            void Save(string name, float[] samples, bool loop = false) => made[name] = Wav(name, samples, loop);

            for (int i = 0; i < 3; i++) Save("Swing" + i, Whoosh(0.22f, 900f + i * 250f, 5200f + i * 600f, 11 + i));
            Save("SpinSwing", Concat(Whoosh(0.2f, 700f, 4200f, 21), Whoosh(0.24f, 1100f, 6000f, 22)));
            Save("Dash", Whoosh(0.26f, 300f, 1800f, 31));
            Save("CueSweep", MetalScrape(0.45f));
            Save("CueCharge", Mix(Drum(0f, 1f), Drum(0.22f, 1f), Drum(0.36f, 1.05f)));
            Save("CueThrow", Mix(Click(0f, 2600f), Click(0.12f, 3000f)));
            Save("CueLeap", Gong(1.3f));
            Save("CueRetreat", Mix(Knock(0f), Knock(0.08f), Knock(0.16f)));
            Save("HeavyDrum", Drum(0f, 0.75f, 0.7f));
            Save("Victory", Melody(new[] { 3, 4, 5, 7, 8, 10 }, 0.16f, 1.2f, 41));
            Save("Defeat", Melody(new[] { 8, 7, 5, 3, 1, 0 }, 0.3f, 1.6f, 42));
            Save("BgmTitle", Loop(TitleMusic(48f), 4f), true);
            Save("BgmBattle", Loop(BattleMusic(40f), 3f), true);
            AssetDatabase.Refresh();
            foreach (var path in made.Values)
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                bool music = path.Contains("Bgm");
                var settings = importer.defaultSampleSettings;
                settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = music ? 0.6f : 0.8f;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
            return made;
        }

        // ---------------- 소리 재료 ----------------

        // 걸러낸 잡음을 휩쓸어 "휙" 소리. 잘림 주파수가 올라갔다 내려간다.
        private static float[] Whoosh(float seconds, float lowHz, float highHz, int seed)
        {
            var rng = new System.Random(seed);
            int n = (int)(seconds * Rate);
            var s = new float[n];
            float lp = 0f, lp2 = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float cutoff = Mathf.Lerp(lowHz, highHz, Mathf.Sin(t * Mathf.PI));
                float a = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / Rate);
                float noise = (float)(rng.NextDouble() * 2 - 1);
                lp += a * (noise - lp);
                lp2 += a * (lp - lp2);
                float env = Mathf.Pow(Mathf.Sin(Mathf.Min(1f, t * 1.15f) * Mathf.PI), 1.5f);
                s[i] = (lp - lp2 * 0.5f) * env * 2.2f;
            }
            return Normalize(s, 0.8f);
        }

        // 금속 긁힘: 어긋난 배음들이 위로 미끄러지며 잡음으로 흔들린다(휘두르기 예고).
        private static float[] MetalScrape(float seconds)
        {
            var rng = new System.Random(5);
            int n = (int)(seconds * Rate);
            var s = new float[n];
            float[] ratios = { 1f, 1.47f, 2.09f, 2.81f };
            float[] phase = new float[ratios.Length];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float baseHz = Mathf.Lerp(700f, 1500f, t * t);
                float v = 0f;
                for (int k = 0; k < ratios.Length; k++)
                {
                    phase[k] += 2f * Mathf.PI * baseHz * ratios[k] / Rate;
                    v += Mathf.Sin(phase[k]) / (k + 1);
                }
                float grit = 0.6f + 0.4f * (float)rng.NextDouble();
                float env = Mathf.Min(1f, t * 12f) * (1f - t) * (1f - t);
                s[i] = v * grit * env;
            }
            return Normalize(s, 0.75f);
        }

        // 북: 음높이가 빠르게 떨어지는 사인파 + 짧은 잡음 타격.
        private static float[] Drum(float at, float gain, float decay = 0.32f)
        {
            var rng = new System.Random(7);
            int start = (int)(at * Rate), n = start + (int)((decay * 2f) * Rate);
            var s = new float[n];
            float phase = 0f;
            for (int i = start; i < n; i++)
            {
                float t = (i - start) / (float)Rate;
                float hz = 52f + 95f * Mathf.Exp(-t * 22f);
                phase += 2f * Mathf.PI * hz / Rate;
                float body = Mathf.Sin(phase) * Mathf.Exp(-t / decay);
                float click = (float)(rng.NextDouble() * 2 - 1) * Mathf.Exp(-t * 90f) * 0.5f;
                s[i] = (body + click) * gain;
            }
            return s;
        }

        // 높고 짧은 "딱"(투척 예고).
        private static float[] Click(float at, float hz)
        {
            var rng = new System.Random((int)hz);
            int start = (int)(at * Rate), n = start + (int)(0.09f * Rate);
            var s = new float[n];
            for (int i = start; i < n; i++)
            {
                float t = (i - start) / (float)Rate;
                s[i] = (Mathf.Sin(2f * Mathf.PI * hz * t) * 0.7f + (float)(rng.NextDouble() * 2 - 1) * 0.3f) * Mathf.Exp(-t * 55f);
            }
            return s;
        }

        // 나무 딱딱이(후퇴 사격 예고): 좁은 대역 잡음 타격.
        private static float[] Knock(float at)
        {
            var rng = new System.Random((int)(at * 1000) + 3);
            int start = (int)(at * Rate), n = start + (int)(0.07f * Rate);
            var s = new float[n];
            float bp1 = 0f, bp2 = 0f;
            float f = 2f * Mathf.Sin(Mathf.PI * 1150f / Rate);
            for (int i = start; i < n; i++)
            {
                float t = (i - start) / (float)Rate;
                float x = (float)(rng.NextDouble() * 2 - 1) * Mathf.Exp(-t * 70f);
                bp1 += f * (x - bp1 - 0.25f * bp2);
                bp2 += f * bp1;
                s[i] = bp1 * 3f;
            }
            return s;
        }

        // 징(도약 예고): 어긋난 배음, 긴 여운, 음높이가 살짝 올라간다.
        private static float[] Gong(float seconds)
        {
            int n = (int)(seconds * Rate);
            var s = new float[n];
            float[] partials = { 98f, 211f, 337f, 468f, 602f };
            float[] amp = { 1f, 0.7f, 0.45f, 0.3f, 0.2f };
            float[] phase = new float[partials.Length];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float bend = Mathf.Lerp(0.88f, 1.02f, Mathf.Min(1f, t / 0.6f));
                float v = 0f;
                for (int k = 0; k < partials.Length; k++)
                {
                    phase[k] += 2f * Mathf.PI * partials[k] * bend / Rate;
                    v += Mathf.Sin(phase[k]) * amp[k] * Mathf.Exp(-t * (0.9f + k * 0.6f));
                }
                s[i] = v * Mathf.Min(1f, t * 40f);
            }
            return Normalize(s, 0.85f);
        }

        // 줄 뜯는 소리(Karplus-Strong) — 가야금풍.
        private static float[] Pluck(float hz, float seconds, float brightness, System.Random rng)
        {
            int n = (int)(seconds * Rate);
            var s = new float[n];
            int period = Mathf.Max(2, (int)(Rate / hz));
            var buffer = new float[period];
            for (int i = 0; i < period; i++) buffer[i] = (float)(rng.NextDouble() * 2 - 1);
            int idx = 0;
            float damp = Mathf.Lerp(0.990f, 0.998f, brightness);
            for (int i = 0; i < n; i++)
            {
                int next = (idx + 1) % period;
                float v = 0.5f * (buffer[idx] + buffer[next]) * damp;
                s[i] = buffer[idx];
                buffer[idx] = v;
                idx = next;
            }
            return s;
        }

        private static float[] Melody(int[] degrees, float step, float tail, int seed)
        {
            var rng = new System.Random(seed);
            var parts = new List<float[]>();
            for (int i = 0; i < degrees.Length; i++)
                parts.Add(Shift(Pluck(Pentatonic[degrees[i]], tail, 0.7f, rng), i * step, 0.6f));
            return Normalize(Mix(parts.ToArray()), 0.8f);
        }

        // 타이틀: 드문드문 뜯는 오음계 선율 + 낮은 지속음 + 바람.
        private static float[] TitleMusic(float seconds)
        {
            var rng = new System.Random(77);
            int n = (int)(seconds * Rate);
            var s = new float[n];
            int degree = 5;
            for (float t = 0.5f; t < seconds; t += (float)(1.4 + rng.NextDouble() * 1.6))
            {
                degree = Mathf.Clamp(degree + rng.Next(-2, 3), 0, Pentatonic.Length - 1);
                Add(s, Pluck(Pentatonic[degree], 3.5f, 0.6f, rng), t, 0.45f);
                if (rng.NextDouble() < 0.35) Add(s, Pluck(Pentatonic[Mathf.Max(0, degree - 3)] * 0.5f, 3.5f, 0.4f, rng), t + 0.05f, 0.3f);
            }
            Drone(s, 73.42f, 0.10f);
            Wind(s, 0.06f, 78);
            return Normalize(s, 0.7f);
        }

        // 전투: 장구·북 장단(덩 · 기덕 · 쿵 · 더러러) + 빠른 오음계 선율 + 지속음.
        private static float[] BattleMusic(float seconds)
        {
            var rng = new System.Random(91);
            int n = (int)(seconds * Rate);
            var s = new float[n];
            float beat = 60f / 100f;
            for (float t = 0f; t < seconds; t += beat * 4)
            {
                Add(s, Drum(0f, 0.9f), t, 1f);                       // 덩
                Add(s, Knock(0f), t + beat * 1f, 0.5f);               // 기
                Add(s, Knock(0f), t + beat * 1.5f, 0.4f);             // 덕
                Add(s, Drum(0f, 0.6f), t + beat * 2f, 1f);           // 쿵
                for (int k = 0; k < 3; k++) Add(s, Knock(0f), t + beat * (3f + k * 0.25f), 0.35f);   // 더러러
            }
            int degree = 3;
            for (float t = 0f; t < seconds; t += beat * (rng.NextDouble() < 0.3 ? 1f : 0.5f))
            {
                if (rng.NextDouble() < 0.25) continue;
                degree = Mathf.Clamp(degree + rng.Next(-2, 3), 0, Pentatonic.Length - 1);
                Add(s, Pluck(Pentatonic[degree], 1.4f, 0.8f, rng), t, 0.3f);
            }
            Drone(s, 73.42f, 0.12f);
            return Normalize(s, 0.75f);
        }

        // ---------------- 보조 ----------------

        private static void Drone(float[] s, float hz, float gain)
        {
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)Rate;
                float swell = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * t / 8f);
                s[i] += (Mathf.Sin(2f * Mathf.PI * hz * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * hz * 1.5f * t)) * gain * swell;
            }
        }

        private static void Wind(float[] s, float gain, int seed)
        {
            var rng = new System.Random(seed);
            float lp = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)Rate;
                lp += 0.02f * ((float)(rng.NextDouble() * 2 - 1) - lp);
                s[i] += lp * gain * 6f * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * t / 11f));
            }
        }

        // 끝에서 넘친 여운을 앞쪽에 겹쳐 끊김 없는 반복 소리로 만든다.
        private static float[] Loop(float[] body, float fadeSeconds)
        {
            int fade = (int)(fadeSeconds * Rate);
            int n = body.Length - fade;
            var s = new float[n];
            Array.Copy(body, s, n);
            for (int i = 0; i < fade; i++)
            {
                float w = i / (float)fade;
                s[i] = s[i] * w + body[n + i] * (1f - w);
            }
            return s;
        }

        private static float[] Shift(float[] src, float at, float gain)
        {
            int start = (int)(at * Rate);
            var s = new float[start + src.Length];
            for (int i = 0; i < src.Length; i++) s[start + i] = src[i] * gain;
            return s;
        }

        private static void Add(float[] dst, float[] src, float at, float gain)
        {
            int start = (int)(at * Rate);
            for (int i = 0; i < src.Length && start + i < dst.Length; i++) if (start + i >= 0) dst[start + i] += src[i] * gain;
        }

        private static float[] Mix(params float[][] parts)
        {
            int n = 0;
            foreach (var p in parts) n = Mathf.Max(n, p.Length);
            var s = new float[n];
            foreach (var p in parts) for (int i = 0; i < p.Length; i++) s[i] += p[i];
            return Normalize(s, 0.85f);
        }

        private static float[] Concat(float[] a, float[] b)
        {
            var s = new float[a.Length + b.Length];
            a.CopyTo(s, 0); b.CopyTo(s, a.Length);
            return s;
        }

        private static float[] Normalize(float[] s, float peak)
        {
            float max = 0.0001f;
            foreach (float v in s) max = Mathf.Max(max, Mathf.Abs(v));
            for (int i = 0; i < s.Length; i++) s[i] = s[i] / max * peak;
            return s;
        }

        private static string Wav(string name, float[] samples, bool loop)
        {
            string path = $"{Folder}/{name}.wav";
            using (var stream = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(stream))
            {
                int bytes = samples.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + bytes);
                w.Write(new[] { 'W', 'A', 'V', 'E' }); w.Write(new[] { 'f', 'm', 't', ' ' });
                w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(bytes);
                foreach (float v in samples) w.Write((short)Mathf.Clamp(v * 32767f, -32768f, 32767f));
            }
            return path;
        }
    }
}
