using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SwordPrototype.Audio;

namespace SwordPrototype.Editor
{
    /// <summary>S8-4: 합성 소리 생성 + Kenney CC0 클립을 소리 이름에 연결해 SoundBank 에셋을 만든다.</summary>
    public static class SoundBankBuilder
    {
        public const string BankPath = "Assets/Game/Audio/SoundBank.asset";
        private const string Kenney = "Assets/Game/Art/Audio/Kenney/";

        public static SoundBank Build()
        {
            var generated = SoundSynth.GenerateAll();
            Directory.CreateDirectory(Path.GetDirectoryName(BankPath));
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(BankPath);
            if (bank == null) { bank = ScriptableObject.CreateInstance<SoundBank>(); AssetDatabase.CreateAsset(bank, BankPath); }

            AudioClip[] Gen(params string[] names) => names.Select(n => AssetDatabase.LoadAssetAtPath<AudioClip>(generated[n])).ToArray();
            AudioClip[] Pack(string pack, string prefix) => Directory.GetFiles(Kenney + pack, prefix + "*.ogg")
                .Select(p => AssetDatabase.LoadAssetAtPath<AudioClip>(p.Replace('\\', '/'))).Where(c => c != null).ToArray();
            SoundBank.Entry E(string name, AudioClip[] clips, float volume, float pitchMin = 0.95f, float pitchMax = 1.05f, SoundCategory cat = SoundCategory.Effects)
                => new SoundBank.Entry { name = name, clips = clips, volume = volume, pitchMin = pitchMin, pitchMax = pitchMax, category = cat };

            var list = new List<SoundBank.Entry>
            {
                E(Sfx.Swing, Gen("Swing0", "Swing1", "Swing2"), 0.7f),
                E(Sfx.SpinSwing, Gen("SpinSwing"), 0.8f),
                E(Sfx.Dash, Gen("Dash").Concat(Pack("rpg-audio", "cloth")).ToArray(), 0.6f),
                E(Sfx.Jump, Pack("rpg-audio", "cloth"), 0.5f, 1.05f, 1.2f),
                E(Sfx.Land, Pack("impact-sounds", "impactSoft_heavy"), 0.45f),
                E(Sfx.Footstep, Pack("impact-sounds", "footstep_concrete"), 0.3f, 0.9f, 1.1f),
                E(Sfx.Hit, Pack("impact-sounds", "impactPunch_medium").Concat(Pack("rpg-audio", "knifeSlice")).ToArray(), 0.85f),
                E(Sfx.HeavyHit, Pack("impact-sounds", "impactPunch_heavy"), 1f),
                E(Sfx.HeavyDrum, Gen("HeavyDrum"), 0.9f, 0.97f, 1.03f),
                E(Sfx.Clash, Pack("impact-sounds", "impactMetal_medium"), 0.85f),
                E(Sfx.Parry, Pack("impact-sounds", "impactMetal_light"), 1f, 1.2f, 1.35f),
                E(Sfx.Hurt, Pack("impact-sounds", "impactPunch_heavy"), 0.9f, 0.75f, 0.85f),
                E(Sfx.KnifeThrow, Pack("rpg-audio", "drawKnife"), 0.7f, 1.1f, 1.25f),
                E(Sfx.EnemyThrow, Pack("rpg-audio", "drawKnife"), 0.8f, 0.75f, 0.85f),
                E(Sfx.LeapLand, Pack("impact-sounds", "impactSoft_heavy"), 1f, 0.65f, 0.75f),
                E(Sfx.PropBreak, Pack("impact-sounds", "impactMining"), 0.9f, 0.8f, 1f),
                E(Sfx.GateClose, Pack("impact-sounds", "impactWood_heavy"), 0.9f, 0.85f, 0.9f),
                // 적 예고음 5종: 패턴마다 다른 합성 소리, 음높이 고정(구분이 흐려지지 않게)
                E(Sfx.CueSweep, Gen("CueSweep"), 0.9f, 1f, 1f),
                E(Sfx.CueCharge, Gen("CueCharge"), 1f, 1f, 1f),
                E(Sfx.CueThrow, Gen("CueThrow"), 0.8f, 1f, 1f),
                E(Sfx.CueLeap, Gen("CueLeap"), 1f, 1f, 1f),
                E(Sfx.CueRetreat, Gen("CueRetreat"), 0.85f, 1f, 1f),
                E(Sfx.UiClick, Pack("interface-sounds", "click"), 0.6f, 1f, 1f, SoundCategory.Interface),
                E(Sfx.UiTick, Pack("interface-sounds", "select"), 0.35f, 1f, 1.1f, SoundCategory.Interface),
                E(Sfx.UiConfirm, Pack("interface-sounds", "confirmation"), 0.6f, 1f, 1f, SoundCategory.Interface),
                E(Sfx.UiDenied, Pack("interface-sounds", "error"), 0.5f, 1f, 1f, SoundCategory.Interface),
                E(Sfx.Victory, Gen("Victory"), 0.9f, 1f, 1f, SoundCategory.Interface),
                E(Sfx.Defeat, Gen("Defeat"), 0.9f, 1f, 1f, SoundCategory.Interface),
                E(Sfx.BgmTitle, Gen("BgmTitle"), 0.55f, 1f, 1f, SoundCategory.Music),
                E(Sfx.BgmBattle, Gen("BgmBattle"), 0.5f, 1f, 1f, SoundCategory.Music),
            };
            bank.entries = list.ToArray();
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            return bank;
        }

        public static void AddService(SoundBank bank, string music)
        {
            new GameObject("AudioService").AddComponent<AudioService>().Configure(bank, music);
        }
    }
}
