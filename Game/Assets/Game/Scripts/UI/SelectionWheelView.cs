using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SwordPrototype.Battle;

namespace SwordPrototype.UI
{
    /// <summary>
    /// S8-3 선택 휠(수묵): 원형 8칸 + 바깥 시간 고리 + 가운데 비도 원 + 낙관 예약 슬롯 + 안내.
    /// 성공률은 CombatResolver.PreviewChance만 쓴다(기존 IMGUI와 같은 계산 경로).
    /// </summary>
    public sealed class SelectionWheelView : MonoBehaviour
    {
        private const float LabelRadius = 162f;
        private const float CursorRadius = 230f;
        public static readonly Vector2 WheelAnchor = new Vector2(0.68f, 0.48f);
        public const float WheelScale = 0.78f;

        [SerializeField] private BattleFlow flow;
        [SerializeField] private GameObject root;
        [SerializeField] private WheelGraphic wheel;
        [SerializeField] private TextMeshProUGUI[] labels = new TextMeshProUGUI[8];
        [SerializeField] private Image center;
        [SerializeField] private TextMeshProUGUI centerText, timerText, hint, info, extra;
        [SerializeField] private Image cursor;
        [SerializeField] private Image[] slots = new Image[3];
        [SerializeField] private TextMeshProUGUI[] slotText = new TextMeshProUGUI[3];

        public GameObject Root => root;
        private int lastHover = -2, lastReserved;
        private ReserveResult lastRejection = ReserveResult.Accepted;

        public void Build(BattleFlow battle, InkUISkin skin)
        {
            flow = battle;
            // R4: 화면 오른쪽에 두어 가운데(적)·왼쪽(플레이어·검)을 가리지 않는다. 0.78배로 줄여 위 조작 안내·아래 쿨타임 원과도 겹치지 않음(검사로 확인).
            var r = Ui.Rect("Wheel", transform, WheelAnchor, Vector2.zero, new Vector2(560, 560));
            r.pivot = new Vector2(0.5f, 0.5f);   // Ui.Rect는 기준점=앵커라서, 휠 중심이 정확히 앵커에 오도록 가운데로
            r.localScale = Vector3.one * WheelScale;
            root = r.gameObject;
            var ring = Ui.Rect("Ring", r, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 560)).gameObject;
            ring.AddComponent<CanvasRenderer>();   // 직접 만든 Graphic은 그리기 부품이 자동으로 붙지 않는 경우가 있어 명시
            wheel = ring.AddComponent<WheelGraphic>();
            wheel.raycastTarget = false;
            for (int i = 0; i < 8; i++)
            {
                Vector2 v = SelectionSession.ToVector((AttackDirection)i);
                labels[i] = Ui.Text("Label" + i, r, skin.body, "", 24, InkUISkin.Paper, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), v * LabelRadius, new Vector2(150, 100));
                labels[i].lineSpacing = -10f;
            }
            center = Ui.Image("Center", r, skin.circle, InkUISkin.Paper, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 150));
            centerText = Ui.Text("CenterText", r, skin.body, "", 24, InkUISkin.Ink, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140, 100));
            cursor = Ui.Image("Cursor", r, skin.circle, InkUISkin.Seal, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22, 22));
            timerText = Ui.Text("Timer", r, skin.body, "", 24, InkUISkin.Paper, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0, 300), new Vector2(300, 36));
            for (int i = 0; i < 3; i++)
            {
                slots[i] = Ui.Image("Slot" + i, r, skin.seal, InkUISkin.Seal, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 86, -330), new Vector2(72, 72));
                slotText[i] = Ui.Text("SlotText", slots[i].transform, skin.body, "", 34, InkUISkin.Paper, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(72, 72));
            }
            hint = Ui.Text("Hint", r, skin.body, "", 24, InkUISkin.Paper, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0, -395), new Vector2(900, 34));
            info = Ui.Text("Info", r, skin.body, "", 22, InkUISkin.Paper, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0, -430), new Vector2(900, 32));
            extra = Ui.Text("Extra", r, skin.body, "", 22, InkUISkin.RepeatYellow, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0, -462), new Vector2(900, 32));
            foreach (var t in new[] { timerText, hint, info, extra }) Ui.Readable(t, skin);
            root.SetActive(false);
        }

        private void Update()
        {
            bool defending = flow != null && flow.State == BattleState.Defend && flow.Defense != null;
            bool show = defending || flow != null && flow.Session != null && (flow.State == BattleState.Focus || flow.State == BattleState.Planning);
            if (root.activeSelf != show) { root.SetActive(show); lastHover = -2; lastReserved = 0; lastRejection = ReserveResult.Accepted; lastLit = -1; }
            if (defending) RefreshDefense(flow.Defense);
            else if (show) Refresh(flow.Session);
        }

        private int lastLit = -1, lastDefenseCount;

        /// <summary>R8 합 공격 방어: 보여주기 중에는 칸이 순서대로 붉게, 입력 중에는 맞힌 칸(먹)·틀린 칸(붉은 테)과 남은 시간.</summary>
        private void RefreshDefense(DefenseSession d)
        {
            int lit = d.LitIndex;
            if (lit != lastLit && lit >= 0) Audio.Sound.Play(Audio.Sfx.UiTick, null, 0.8f + lit * 0.15f);
            lastLit = lit;
            if (d.Results.Count > lastDefenseCount) Audio.Sound.Play(d.Results[d.Results.Count - 1] ? Audio.Sfx.UiConfirm : Audio.Sfx.UiDenied);
            lastDefenseCount = d.Results.Count;
            int hover = d.AtCenter ? -1 : (int)d.Hovered;
            if (d.InInput && hover != lastHover && lastHover != -2) Audio.Sound.Play(Audio.Sfx.UiTick);
            lastHover = hover;

            wheel.timer = d.InInput ? d.InputLeft / d.InputSeconds : 1f - d.Elapsed / d.ShowSeconds;
            wheel.timerColor = d.InInput ? InkUISkin.Seal : InkUISkin.Faded;
            timerText.text = d.InInput ? $"{d.InputLeft:0.00}초" : "보기";
            for (int i = 0; i < 8; i++)
            {
                var dir = (AttackDirection)i;
                bool isLit = lit >= 0 && d.Pattern[lit] == dir;
                bool hovered = d.InInput && !d.AtCenter && d.Hovered == dir;
                Color fill = isLit ? new Color(0.85f, 0.12f, 0.08f, 0.95f) : hovered ? InkUISkin.Seal : new Color(0.13f, 0.12f, 0.11f, 0.9f);
                wheel.SetSegment(i, fill, Color.clear);
                labels[i].text = $"<size=30>{DirectionText.Arrow(dir)}</size>";
                labels[i].color = InkUISkin.Paper;
            }
            wheel.SetVerticesDirty();
            center.color = InkUISkin.Paper;
            centerText.text = "막기";
            centerText.color = InkUISkin.Ink;
            cursor.rectTransform.anchoredPosition = d.Cursor * CursorRadius;
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].gameObject.SetActive(true);
                bool done = i < d.Results.Count;
                slots[i].color = !done ? new Color(0.12f, 0.11f, 0.10f, 0.55f) : d.Results[i] ? InkUISkin.Ink : new Color(0.85f, 0.12f, 0.08f, 0.95f);
                slotText[i].text = done ? DirectionText.Arrow(d.Pattern[i]) : $"<size=20>{i + 1}타</size>";
            }
            hint.text = d.InInput ? "같은 순서로 커서 + 좌클릭" : "적 합 공격 — 붉게 빛나는 순서를 기억";
            info.text = "막으면 튕겨냄 · 3개 모두 막으면 반격 1회";
            extra.text = "";
        }

        /// <summary>R5: 휘두르는 크기 표시. 값은 판정과 같은 StatEffects.StanceBonus(상성표로 바꿔도 그대로 따라감).</summary>
        public static string SwingLabel(AttackDirection start, AttackDirection target, StatTuning t)
        {
            float bonus = StatEffects.StanceBonus(start, target, t);
            if (Mathf.Abs(bonus) < 0.005f) return "";
            string[] names = { "", "짧게", "", "넓게", "크게" };
            string name = names[StatEffects.SwingSteps(start, target)];
            string color = bonus > 0f ? "#E9C46A" : "#9A938A";
            return $"\n<size=19><color={color}>{name} {(bonus > 0f ? "+" : "-")}{Mathf.Abs(bonus) * 100f:0}%</color></size>";
        }

        private void Refresh(SelectionSession session)
        {
            // S8-4: 칸 이동 · 예약 확정 · 불가 소리
            int hover = session.AtCenter ? -1 : (int)session.Hovered;
            if (hover != lastHover && lastHover != -2) Audio.Sound.Play(Audio.Sfx.UiTick);
            lastHover = hover;
            if (session.Reserved.Count > lastReserved) Audio.Sound.Play(Audio.Sfx.UiConfirm);
            lastReserved = session.Reserved.Count;
            if (session.LastRejection != lastRejection && !ComboRules.IsAccepted(session.LastRejection)) Audio.Sound.Play(Audio.Sfx.UiDenied);
            lastRejection = session.LastRejection;
            CharacterStats s = flow.Stats.Stats;
            bool focusing = flow.State == BattleState.Focus;
            wheel.timer = focusing ? flow.FocusProgress : session.TimeLeft / session.Duration;
            wheel.timerColor = focusing ? InkUISkin.Faded : InkUISkin.Seal;
            timerText.text = focusing ? "포커스" : $"{session.TimeLeft:0.00}초";

            // R5: 다음 타의 검 시작 자세(앞 예약이 있으면 그 방향). 판정과 같은 함수.
            AttackDirection stanceNow = CombatResolver.SwingStart(session.Reserved, flow.SelectionStance);
            for (int i = 0; i < 8; i++)
            {
                var d = (AttackDirection)i;
                ReserveResult check = session.Evaluate(d);
                bool accepted = ComboRules.IsAccepted(check);
                bool hovered = !session.AtCenter && session.Hovered == d;
                int pressed = ComboRules.RepeatCount(session.Reserved, d) - 1;   // 끝에서 연속으로 누른 횟수
                Color fill = !accepted ? new Color(0.35f, 0.33f, 0.30f, 0.55f) : hovered ? InkUISkin.Seal : new Color(0.13f, 0.12f, 0.11f, 0.9f);
                wheel.SetSegment(i, fill, pressed > 0 ? InkUISkin.Repeat(pressed) : Color.clear);

                string body;
                if (accepted)
                {
                    float chance = session.Counter ? 1f
                        : CombatResolver.PreviewChance(session.Reserved, d, s, flow.Stats.Tuning, flow.InFront, flow.SideBias, flow.KnifePenalty, flow.AirStrike, flow.SelectionStance, flow.SelectionAdaptation);
                    body = $"{chance * 100f:0}%" + (check == ReserveResult.AcceptedAsFinisher ? "\n<size=18>마무리</size>" : "");
                }
                else body = check == ReserveResult.ComboFull ? "-" : "불가";
                // R8: 같은 방향·같은 쪽 묶음은 회전 베기(보정은 그대로 — 한 번 맞고 돌면 막기 쉬워짐)
                string bonus = accepted && !session.Counter ? SwingLabel(stanceNow, d, flow.Stats.Tuning) : "";
                string spin = ComboRules.IsSpin(stanceNow, d) && accepted ? "\n<size=19>회전 베기</size>" + bonus.Replace("\n", " ") : bonus;
                labels[i].text = $"<size=30>{DirectionText.Arrow(d)}</size> <size=18>{DirectionText.Name(d)}</size>\n{body}{spin}";
                labels[i].color = accepted ? InkUISkin.Paper : InkUISkin.Faded;
            }
            wheel.SetVerticesDirty();

            center.color = session.AtCenter && session.KnifeSelectable ? InkUISkin.Seal : InkUISkin.Paper;
            centerText.text = session.KnifeSelectable ? $"비도\n{(1f - flow.Knife.FailChance) * 100f:0}%" : "·";
            centerText.color = session.AtCenter && session.KnifeSelectable ? InkUISkin.Paper : InkUISkin.Ink;
            cursor.rectTransform.anchoredPosition = session.Cursor * CursorRadius;

            for (int i = 0; i < slots.Length; i++)
            {
                bool exists = i < session.MaxHits;
                slots[i].gameObject.SetActive(exists);
                if (!exists) continue;
                bool filled = i < session.Reserved.Count;
                slots[i].color = filled ? InkUISkin.Seal : new Color(0.12f, 0.11f, 0.10f, 0.55f);
                slotText[i].text = filled ? DirectionText.Arrow(session.Reserved[i]) : $"<size=20>{i + 1}타</size>";
            }
            hint.text = session.LastRejection == ReserveResult.FinisherUnavailable ? "같은 방향 3번째: 불가 (특수기 3·쿨타임 필요)"
                : session.LastRejection == ReserveResult.SameDirectionLimit ? "같은 방향은 최대 2번"
                : session.LastRejection == ReserveResult.VerticalRepeat ? "위·아래는 연속으로 벨 수 없음" : "마우스로 방향, 좌클릭으로 예약";
            info.text = (flow.InFront ? "적 정면" : "적 후면") + $" · 검 {DirectionText.Arrow(stanceNow)}" + (flow.AirStrike ? " · 공중 베기: ↓↙↘ +10%" : "")
                + (flow.SelectionAdaptation > 0.005f ? $" · 적 경계 -{flow.SelectionAdaptation * 100f:0}%" : "");
            extra.text = session.Counter ? "반격 1타 · 무조건 성공"
                : session.DashSlashSelectable ? "우클릭: 지나가며 베기 (확정)"
                : session.DashSlashBlockedByReservation ? "지나가며 베기: 예약 후 불가"
                : StatEffects.HasDashSlash(s) ? "지나가며 베기: 쿨타임" : "";
        }
    }
}
