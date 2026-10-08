using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SwordPrototype.Battle;
using SwordPrototype.Flow;

namespace SwordPrototype.UI
{
    /// <summary>S8-3 결과 화면(두루마리): 붓글씨 승리/패배, 기록 7항목, 다시 하기(R)·스탯 다시 배분·타이틀로.</summary>
    public sealed class ResultView : MonoBehaviour
    {
        [SerializeField] private BattleFlow flow;
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI title, body;
        [SerializeField] private Button retry, realloc, toTitle;

        public void Build(BattleFlow battle, InkUISkin skin)
        {
            flow = battle;
            var bg = Ui.Image("Result", transform, skin.panel, InkUISkin.Paper, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 640));
            panel = bg.gameObject;
            title = Ui.Text("Title", bg.transform, skin.title, "승리", 110, InkUISkin.Seal, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(700, 130));
            body = Ui.Text("Body", bg.transform, skin.body, "", 26, InkUISkin.Ink, TextAlignmentOptions.TopLeft, new Vector2(0.5f, 1f), new Vector2(0, -160), new Vector2(620, 330));
            body.lineSpacing = 18f;
            retry = Ui.Button("Retry", bg.transform, skin, "다시 하기 (R)", new Vector2(0.5f, 0f), new Vector2(-235, 40), new Vector2(210, 64), 26);
            realloc = Ui.Button("Realloc", bg.transform, skin, "스탯 다시 배분", new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(210, 64), 26);
            toTitle = Ui.Button("ToTitle", bg.transform, skin, "타이틀로", new Vector2(0.5f, 0f), new Vector2(235, 40), new Vector2(210, 64), 26);
            panel.SetActive(false);
        }

        private void Awake()
        {
            Ui.OnClick(retry, () => flow.Restart());
            Ui.OnClick(realloc, () => flow.GoToTitle(true));
            Ui.OnClick(toTitle, () => flow.GoToTitle(false));
        }

        private void Update()
        {
            bool show = flow != null && flow.ResultReady;
            if (panel.activeSelf != show) panel.SetActive(show);
            if (!show) return;
            bool win = flow.State == BattleState.Victory;
            title.text = win ? "승리" : "패배";
            title.color = win ? InkUISkin.Seal : InkUISkin.Ink;
            RunRecord r = flow.Record;
            CharacterStats s = flow.Stats.Stats;
            float t = r.Elapsed(SwordPrototype.Battle.BattleClock.Now);
            body.text =
                $"걸린 시간\t{(int)(t / 60f)}분 {t % 60f:00.0}초\n" +
                $"남은 체력\t{flow.PlayerHealth.Current:0} / {flow.PlayerHealth.Max:0}\n" +
                $"받은 피해\t{r.DamageTaken:0}\n" +
                $"공격 성공률\t{r.SuccessRate * 100f:0}%  ({r.Successes}/{r.Strikes})\n" +
                $"최대 연속 성공\t{r.MaxStreak}\n" +
                $"패링\t{r.Parries}회\n" +
                $"<size=22>스탯  체력 {s.Health} · 기력 {s.Stamina} · 속도 {s.Speed} · 힘 {s.Strength} · 특수기 {s.Special}</size>";
        }
    }
}
