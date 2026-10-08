using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SwordPrototype.Battle;

namespace SwordPrototype.UI
{
    /// <summary>
    /// S8-3 전투 HUD(수묵): 플레이어·적 붓 체력바, 적 상태, 거리, 쿨타임 먹 원 5개, 무방비 알림, 전투 기록.
    /// 값은 BattleFlow에서 읽기만 한다(기존 IMGUI CombatHUD와 같은 정보).
    /// </summary>
    public sealed class CombatHudView : MonoBehaviour
    {
        private static readonly string[] CooldownNames = { "대쉬", "베기", "마무리", "가드", "비도" };

        [SerializeField] private BattleFlow flow;
        [SerializeField] private Image playerFill, enemyFill;
        [SerializeField] private TextMeshProUGUI playerText, enemyText, enemyState, rangeText, stagger;
        [SerializeField] private Image[] cooldownFill = new Image[5];
        [SerializeField] private TextMeshProUGUI[] cooldownText = new TextMeshProUGUI[5];
        [SerializeField] private GameObject[] cooldownRoot = new GameObject[5];
        [SerializeField] private TextMeshProUGUI[] logLines = new TextMeshProUGUI[5];

        public void Build(BattleFlow battle, InkUISkin skin)
        {
            flow = battle;
            var root = transform;
            // 글자 뒤 먹 번짐 배경(밝은 바닥 위에서도 읽히게)
            Ui.Image("InkWashLeft", root, skin.brush, new Color(0.08f, 0.07f, 0.06f, 0.6f), new Vector2(0f, 0f), new Vector2(0, 16), new Vector2(860, 380));
            Ui.Image("InkWashTop", root, skin.brush, new Color(0.08f, 0.07f, 0.06f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0, 0), new Vector2(1000, 140));
            // 적 체력(위 가운데)
            enemyState = Ui.Text("EnemyState", root, skin.body, "", 26, InkUISkin.Paper, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0, -24), new Vector2(900, 40));
            Ui.Image("EnemyBarBack", root, skin.brush, new Color(0, 0, 0, 0.45f), new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(760, 30));
            enemyFill = Ui.Image("EnemyBarFill", root, skin.brush, InkUISkin.Seal, new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(760, 30));
            Filled(enemyFill);
            enemyText = Ui.Text("EnemyHp", root, skin.body, "", 20, InkUISkin.Paper, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0, -104), new Vector2(400, 28));
            // 플레이어 체력(아래 왼쪽)
            playerText = Ui.Text("PlayerHp", root, skin.body, "", 22, InkUISkin.Paper, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(48, 92), new Vector2(500, 30));
            Ui.Image("PlayerBarBack", root, skin.brush, new Color(0, 0, 0, 0.45f), new Vector2(0f, 0f), new Vector2(40, 48), new Vector2(520, 34));
            playerFill = Ui.Image("PlayerBarFill", root, skin.brush, InkUISkin.Paper, new Vector2(0f, 0f), new Vector2(40, 48), new Vector2(520, 34));
            Filled(playerFill);
            rangeText = Ui.Text("Range", root, skin.body, "", 20, InkUISkin.Paper, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(48, 126), new Vector2(700, 28));
            // 쿨타임 먹 원(아래 오른쪽)
            for (int i = 0; i < 5; i++)
            {
                var slot = Ui.Rect("Cooldown_" + CooldownNames[i], root, new Vector2(1f, 0f), new Vector2(-60 - (4 - i) * 104, 52), new Vector2(88, 88));
                cooldownRoot[i] = slot.gameObject;
                Ui.Image("Back", slot, skin.circle, InkUISkin.InkSoft, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(88, 88));
                cooldownFill[i] = Ui.Image("Fill", slot, skin.circle, new Color(0.55f, 0.52f, 0.47f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(88, 88));
                cooldownFill[i].type = Image.Type.Filled;
                cooldownFill[i].fillMethod = Image.FillMethod.Radial360;
                cooldownFill[i].fillOrigin = (int)Image.Origin360.Top;
                cooldownFill[i].fillClockwise = false;
                cooldownText[i] = Ui.Text("Label", slot, skin.body, CooldownNames[i], 20, InkUISkin.Paper, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(88, 88));
            }
            stagger = Ui.Text("Stagger", root, skin.title, "", 54, InkUISkin.Seal, TextAlignmentOptions.Center, new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(1200, 80));
            foreach (var t in new[] { enemyState, enemyText, playerText, rangeText }) Ui.Readable(t, skin);
            foreach (var t in cooldownText) Ui.Readable(t, skin);
            // 전투 기록(왼쪽, 아래로 갈수록 최신·진함)
            for (int i = 0; i < logLines.Length; i++)
                logLines[i] = Ui.Readable(Ui.Text("Log" + i, root, skin.body, "", 22, InkUISkin.Paper, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(48, 330 - i * 30), new Vector2(700, 30)), skin);
        }

        private static void Filled(Image image)
        {
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = 0;
        }

        private void Update()
        {
            if (flow == null || flow.Stats == null || flow.PlayerHealth == null) return;
            CharacterStats s = flow.Stats.Stats;
            playerFill.fillAmount = flow.PlayerHealth.Current / flow.PlayerHealth.Max;
            playerText.text = $"체력 {flow.PlayerHealth.Current:0} / {flow.PlayerHealth.Max:0}" + (flow.PlayerHealth.IsInvulnerable ? "  · 무적" : "");
            enemyFill.fillAmount = flow.EnemyHealth.Current / flow.EnemyHealth.Max;
            enemyText.text = $"{flow.EnemyHealth.Current:0} / {flow.EnemyHealth.Max:0}";
            enemyState.text = flow.EnemyBrain != null ? "적 · " + flow.EnemyBrain.StateText : "";
            bool inRange = flow.DistanceToEnemy <= flow.AttackRange;
            // R8: 재진입 대기 · 적 경계(적응) 표시
            string ready = !flow.AttackReentry.Ready && !flow.EnemyStaggered ? $"자세 가다듬는 중 {flow.AttackReentry.Remaining:0.0}초"
                : inRange ? "<color=#E8DFC9>공격 가능</color>" : "사거리 밖(허공 베기)";
            rangeText.text = $"거리 {flow.DistanceToEnemy:0.0}m · " + ready + (flow.Adaptation > 0.005f ? $" · 적 경계 -{flow.Adaptation * 100f:0}%" : "");

            Cooldown(0, flow.Dash != null, flow.Dash != null ? flow.Dash.Cooldown : null, StatEffects.DodgeInvulnerability(s, flow.Stats.Tuning) <= 0f ? "대쉬\n<size=14>무적 없음</size>" : "대쉬");
            Cooldown(1, StatEffects.HasDashSlash(s), flow.DashSlashCooldown, "베기");
            Cooldown(2, StatEffects.HasFinisher(s), flow.FinisherCooldown, "마무리");
            Cooldown(3, StatEffects.HasGuardParry(s), flow.Parry.Cooldown, flow.Parry.IsParrying ? "가드 중" : "가드");
            cooldownRoot[4].SetActive(StatEffects.HasThrowingKnife(s));
            cooldownFill[4].fillAmount = flow.Knife.FailChance;
            cooldownText[4].text = $"비도\n<size=16>{(1f - flow.Knife.FailChance) * 100f:0}%</size>";

            stagger.text = flow.EnemyStaggered ? $"적 무방비 {flow.StaggerRemaining:0.0}초 · 4m 안 좌클릭 반격" : "";

            var log = flow.Log;
            for (int i = 0; i < logLines.Length; i++)
            {
                int index = log.Count - logLines.Length + i;
                logLines[i].text = index >= 0 ? log[index] : "";
                var c = logLines[i].color; c.a = 0.35f + 0.65f * (i + 1) / logLines.Length; logLines[i].color = c;
            }
        }

        // 남은 쿨타임 비율만큼 먹이 덮이고, 준비되면 비워진다.
        private void Cooldown(int index, bool owned, Battle.Cooldown cooldown, string label)
        {
            cooldownRoot[index].SetActive(owned);
            if (!owned || cooldown == null) return;
            float ratio = cooldown.Ready ? 0f : Mathf.Clamp01(cooldown.Remaining / Mathf.Max(0.01f, Max(index)));
            cooldownFill[index].fillAmount = ratio;
            cooldownText[index].text = cooldown.Ready ? label : $"{label}\n<size=16>{cooldown.Remaining:0.0}</size>";
        }

        private float Max(int index)
        {
            StatTuning t = flow.Stats.Tuning; CharacterStats s = flow.Stats.Stats;
            switch (index)
            {
                case 0: return StatEffects.DashCooldown(s, t);
                case 1: return StatEffects.SkillCooldown(s, t, t.dashSlashCooldown);
                case 2: return StatEffects.SkillCooldown(s, t, t.finisherCooldown);
                default: return t.parryCooldown;
            }
        }
    }
}
