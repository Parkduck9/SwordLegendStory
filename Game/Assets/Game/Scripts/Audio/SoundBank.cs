using System;
using UnityEngine;

namespace SwordPrototype.Audio
{
    public enum SoundCategory { Music, Effects, Interface }

    /// <summary>S8-4 소리 이름 목록. 연결 지점과 SoundBank가 같은 이름을 쓴다.</summary>
    public static class Sfx
    {
        public const string Swing = "Swing", SpinSwing = "SpinSwing", Dash = "Dash", Jump = "Jump", Land = "Land", Footstep = "Footstep";
        public const string Hit = "Hit", HeavyHit = "HeavyHit", HeavyDrum = "HeavyDrum", Clash = "Clash", Parry = "Parry", Hurt = "Hurt";
        public const string KnifeThrow = "KnifeThrow", EnemyThrow = "EnemyThrow", LeapLand = "LeapLand", PropBreak = "PropBreak", GateClose = "GateClose";
        public const string CueSweep = "CueSweep", CueCharge = "CueCharge", CueThrow = "CueThrow", CueLeap = "CueLeap", CueRetreat = "CueRetreat";
        public const string UiClick = "UiClick", UiTick = "UiTick", UiConfirm = "UiConfirm", UiDenied = "UiDenied";
        public const string Victory = "Victory", Defeat = "Defeat", BgmTitle = "BgmTitle", BgmBattle = "BgmBattle";

        public static readonly string[] All =
        {
            Swing, SpinSwing, Dash, Jump, Land, Footstep, Hit, HeavyHit, HeavyDrum, Clash, Parry, Hurt, KnifeThrow, EnemyThrow, LeapLand,
            PropBreak, GateClose, CueSweep, CueCharge, CueThrow, CueLeap, CueRetreat, UiClick, UiTick, UiConfirm, UiDenied,
            Victory, Defeat, BgmTitle, BgmBattle
        };

        /// <summary>적 패턴 → 예고음(서로 다른 음색·리듬).</summary>
        public static string Cue(Enemy.EnemyAttackKind kind)
        {
            switch (kind)
            {
                case Enemy.EnemyAttackKind.Sweep: return CueSweep;
                case Enemy.EnemyAttackKind.ChargeSlash: return CueCharge;
                case Enemy.EnemyAttackKind.Throw: return CueThrow;
                case Enemy.EnemyAttackKind.LeapSlam: return CueLeap;
                // R8 새 기술: 비슷한 계열 예고음을 함께 쓰고, 합 공격만 검 맞부딪는 소리로 구분
                case Enemy.EnemyAttackKind.SpinSlash: return CueSweep;
                case Enemy.EnemyAttackKind.Thrust: return CueCharge;
                case Enemy.EnemyAttackKind.FanThrow: return CueThrow;
                case Enemy.EnemyAttackKind.ComboAssault: return Clash;
                default: return CueRetreat;   // 후퇴 사격 · 밀쳐 차기
            }
        }
    }

    /// <summary>소리 이름 → 클립 목록·음량·음높이 범위·분류. 에디터 생성기가 채운다.</summary>
    [CreateAssetMenu(menuName = "검협전/Sound Bank")]
    public sealed class SoundBank : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string name;
            public AudioClip[] clips;
            [Range(0f, 1f)] public float volume = 1f;
            public float pitchMin = 0.95f, pitchMax = 1.05f;
            public SoundCategory category = SoundCategory.Effects;
        }

        public Entry[] entries = new Entry[0];

        public Entry Find(string soundName)
        {
            foreach (var e in entries) if (e != null && e.name == soundName) return e;
            return null;
        }
    }
}
