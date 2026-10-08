namespace SwordPrototype.Flow
{
    /// <summary>한 판의 전투 기록(결과 화면용). 시간은 전투장 진입부터 실제 시간.</summary>
    public sealed class RunRecord
    {
        private int streak;

        public bool Started { get; private set; }
        public float StartTime { get; private set; }
        public float EndTime { get; private set; } = -1f;
        public int Strikes { get; private set; }
        public int Successes { get; private set; }
        public int MaxStreak { get; private set; }
        public float DamageTaken { get; private set; }
        public int Parries { get; private set; }

        public void Begin(float now) { if (Started) return; Started = true; StartTime = now; }
        public void Finish(float now) { if (EndTime < 0f) EndTime = now; }
        public float Elapsed(float now) => !Started ? 0f : (EndTime >= 0f ? EndTime : now) - StartTime;
        public float SuccessRate => Strikes == 0 ? 0f : Successes / (float)Strikes;

        public void RecordStrike(bool success)
        {
            Strikes++;
            if (success)
            {
                Successes++;
                streak++;
                if (streak > MaxStreak) MaxStreak = streak;
            }
            else streak = 0;
        }

        public void RecordDamageTaken(float amount) { if (amount > 0f) DamageTaken += amount; }
        public void RecordParry() => Parries++;
    }
}
