namespace SwordPrototype.Battle
{
    /// <summary>
    /// 체력 3 우클릭 가드. 실시간 전투에서만 사용(입력 허용은 BattleFlow가 결정).
    /// 짧은 무적 패링 구간을 열고, 패링에 성공하면 쿨타임 일부를 돌려받는다.
    /// </summary>
    public sealed class GuardParry
    {
        private readonly Cooldown cooldown = new Cooldown();
        private float windowRemaining;

        public bool IsParrying => windowRemaining > 0f;
        public Cooldown Cooldown => cooldown;

        public bool TryStart(CharacterStats s, StatTuning t)
        {
            if (!StatEffects.HasGuardParry(s) || !cooldown.Ready) return false;
            windowRemaining = t.parryWindow;
            cooldown.Start(t.parryCooldown);
            return true;
        }

        public void Tick(float deltaTime)
        {
            cooldown.Tick(deltaTime);
            if (windowRemaining > 0f) windowRemaining -= deltaTime;
        }

        /// <summary>적 공격이 구간 안에 들어오면 호출. 패링 성공 시 true.</summary>
        public bool TryParry(StatTuning t)
        {
            if (!IsParrying) return false;
            cooldown.Refund(t.parryRefund);
            return true;
        }
    }
}
