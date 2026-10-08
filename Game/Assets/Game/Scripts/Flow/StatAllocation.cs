using SwordPrototype.Battle;

namespace SwordPrototype.Flow
{
    public enum StatKind { Health, Stamina, Speed, Strength, Special }

    /// <summary>스탯 배분 화면의 규칙: 각 0~3, 합계 10 이하(미소진 허용, 0포인트는 하드코어).</summary>
    public sealed class StatAllocation
    {
        public static readonly string[] Names = { "체력", "기력", "속도", "힘", "특수기" };

        /// <summary>S7 설계 프리셋: 균형 / 공격형 / 회피형.</summary>
        public static readonly (string name, int[] levels)[] Presets =
        {
            ("균형", new[] { 2, 2, 2, 2, 2 }),
            ("공격형", new[] { 1, 2, 2, 3, 2 }),
            ("회피형", new[] { 2, 2, 3, 1, 2 }),
        };

        private readonly int[] levels = new int[5];

        public StatAllocation() { }
        public StatAllocation(CharacterStats from) => Set(from);

        public int this[StatKind kind] => levels[(int)kind];
        public int Used { get { int sum = 0; foreach (int l in levels) sum += l; return sum; } }
        public int Remaining => CharacterStats.TotalPoints - Used;
        public bool Hardcore => Used == 0;

        public bool CanIncrease(StatKind kind) => levels[(int)kind] < CharacterStats.MaxLevel && Remaining > 0;
        public bool CanDecrease(StatKind kind) => levels[(int)kind] > 0;
        public void Increase(StatKind kind) { if (CanIncrease(kind)) levels[(int)kind]++; }
        public void Decrease(StatKind kind) { if (CanDecrease(kind)) levels[(int)kind]--; }
        public void Clear() { for (int i = 0; i < levels.Length; i++) levels[i] = 0; }

        public void ApplyPreset(int index)
        {
            int[] p = Presets[index].levels;
            for (int i = 0; i < levels.Length; i++) levels[i] = p[i];
        }

        public void Set(CharacterStats s)
        {
            levels[0] = s.Health; levels[1] = s.Stamina; levels[2] = s.Speed; levels[3] = s.Strength; levels[4] = s.Special;
        }

        public CharacterStats ToStats() => new CharacterStats(levels[0], levels[1], levels[2], levels[3], levels[4]);
    }
}
