using SwordPrototype.Battle;

namespace SwordPrototype.Flow
{
    /// <summary>스탯 배분 화면 설명 문장. 모든 숫자는 StatTuning·StatEffects에서 가져와 수치가 바뀌면 화면도 따라간다.</summary>
    public static class StatDescriber
    {
        public static string Describe(StatKind kind, int level, StatTuning t)
        {
            switch (kind)
            {
                case StatKind.Health:
                {
                    var s = new CharacterStats(level, 0, 0, 0, 0);
                    string text = $"HP {StatEffects.MaxHealth(s, t):0}";
                    text += StatEffects.IsLowHealthStat(s) ? $" · 실패 시 자신 {t.failSelfDamage:0} 피해" : $" · 실패 시 적 {t.failEnemyDamage:0} 피해";
                    if (level == 0) text += $" · 정면 {t.frontSuccessHealth0 * 100f:0}% · 피해 ×{t.health0DamageScale:0.##}";
                    else if (level >= 2) text += $" · 정면 +{t.frontSuccessHealthHigh * 100f:0}%";
                    if (StatEffects.HasGuardParry(s)) text += $" · 우클릭 가드 패링 {t.parryWindow:0.#}초";
                    return text;
                }
                case StatKind.Stamina:
                {
                    var s = new CharacterStats(0, level, 0, 0, 0);
                    string text = $"스킬 쿨 ×{StatTuning.At(t.skillCooldownScale, level):0.##}";
                    if (StatEffects.HasThrowingKnife(s)) text += " · 비도";
                    return text;
                }
                case StatKind.Speed:
                {
                    var s = new CharacterStats(0, 0, level, 0, 0);
                    float back = StatTuning.At(t.backSuccessBySpeed, level) * 100f;
                    string text = $"이동 {StatEffects.MoveSpeed(s, t):0.#} · {ComboRules.MaxHits(level)}타 {StatEffects.SelectionSeconds(s, t):0.#}초 · 대쉬 {StatEffects.DashCooldown(s, t):0.#}초";
                    if (back != 0f) text += $" · 후면 {(back > 0f ? "+" : "")}{back:0}%";
                    if (level == 0) text += $" · 성공률 -{t.speed0SuccessPenalty * 100f:0}%";
                    return text;
                }
                case StatKind.Strength:
                {
                    var s = new CharacterStats(0, 0, 0, level, 0);
                    string text = $"피해 ×{StatTuning.At(t.strengthDamageScale, level):0.##} · 특수기 ×{StatTuning.At(t.strengthSpecialScale, level):0.##}";
                    if (StatEffects.HasCritical(s)) text += $" · +{t.strength3FlatBonus:0} · 치명타 {t.critChance * 100f:0}%";
                    return text;
                }
                default:
                {
                    var s = new CharacterStats(0, 0, 0, 0, level);
                    if (level == 0) return "대쉬 무적 없음";
                    string text = $"회피 무적 {StatEffects.DodgeInvulnerability(s, t):0.#}초";
                    if (StatEffects.HasDashSlash(s)) text += " · 지나가며 베기";
                    if (StatEffects.HasFinisher(s)) text += " · 같은 방향 3연속 마무리";
                    return text;
                }
            }
        }

        /// <summary>다음 레벨로 올리면 어떻게 바뀌는지(마우스를 올렸을 때 표시).</summary>
        public static string DescribeNext(StatKind kind, int level, StatTuning t)
            => level >= CharacterStats.MaxLevel ? "최대 레벨" : $"{level + 1}레벨: " + Describe(kind, level + 1, t);
    }
}
