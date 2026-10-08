using UnityEngine;
using SwordPrototype.Battle;

namespace SwordPrototype.Flow
{
    /// <summary>
    /// 장면 사이 스탯 전달(Title → Foundation). 마지막 스탯은 이 기기의 PlayerPrefs에만 기억한다.
    /// Foundation을 단독 실행하면 HasSelection이 false라 PlayerStats Inspector 값을 쓴다.
    /// </summary>
    public static class RunSetup
    {
        public const string TitleScene = "Title";
        public const string ArenaScene = "Foundation";
        private const string LastStatsKey = "검협전.LastStats";

        public static bool HasSelection { get; private set; }
        public static CharacterStats Stats { get; private set; }
        /// <summary>결과 화면의 "스탯 다시 배분"으로 타이틀에 돌아갈 때 배분 화면을 바로 연다.</summary>
        public static bool OpenAllocation { get; set; }

        public static void Select(CharacterStats stats)
        {
            Stats = stats;
            HasSelection = true;
            PlayerPrefs.SetString(LastStatsKey, Format(stats));
            PlayerPrefs.Save();
        }

        public static CharacterStats LoadLast(CharacterStats fallback)
            => TryParse(PlayerPrefs.GetString(LastStatsKey, ""), out CharacterStats s) ? s : fallback;

        public static string Format(CharacterStats s) => $"{s.Health},{s.Stamina},{s.Speed},{s.Strength},{s.Special}";

        public static bool TryParse(string text, out CharacterStats stats)
        {
            stats = default;
            if (string.IsNullOrEmpty(text)) return false;
            string[] parts = text.Split(',');
            if (parts.Length != 5) return false;
            var v = new int[5];
            for (int i = 0; i < 5; i++) if (!int.TryParse(parts[i], out v[i])) return false;
            stats = new CharacterStats(v[0], v[1], v[2], v[3], v[4]);
            return stats.IsValid(out _);
        }
    }
}
