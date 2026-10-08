using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SwordPrototype.Presentation;
using SwordPrototype.Audio;

namespace SwordPrototype.Battle
{
    public enum BattleState { Realtime, Focus, Planning, Exchange, Victory, Defeat, Defend }

    /// <summary>
    /// 전투 상태 전환의 단일 책임. 실시간 → (거리 3m 안 좌클릭) 포커스 → 선택 → 교환 → 실시간.
    /// 시간 배율·카메라 포커스·선택 중 보호의 해제도 여기서만 한다. 교환 연출은 ExchangeDirector(S5)가 재생한다.
    /// </summary>
    public sealed class BattleFlow : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Transform enemy;
        [SerializeField] private ThirdPersonCamera view;
        [SerializeField] private EncounterTrigger encounter;
        [Header("확정값")]
        [SerializeField] private float attackRange = 3f;           // 8차 답변: 몸 중심 기준 3m
        [SerializeField] private float staggerSeconds = 1.5f;      // 9차 확인: 패링 후 적 무방비(실제 시간, 선택 중 정지)
        [SerializeField] private float counterRange = 4f;          // 9차 확인: 무방비 중 반격 진입 거리
        [Header("시험값")]
        [SerializeField] private float selectionTimeScale = 0.1f;
        [SerializeField] private float focusSeconds = 0.25f;
        [SerializeField] private float timeoutCounterDamage = 8f;   // 속도 0·1 시간 초과 시 적의 반격
        [SerializeField] private float finisherSpinSeconds = 0.7f;
        [SerializeField] private float dashSlashSeconds = 0.16f;
        [SerializeField] private float dashSlashOvershoot = 2.5f;   // 적 뒤로 지나가는 거리

        private PlayerStats stats;
        private PlayerMovement movement;
        private PlayerDash dash;
        private PlayerJump jump;
        private bool airStrike;
        private Health playerHealth;
        private Health enemyHealth;
        private Enemy.EnemyPose enemyPose;
        private Enemy.EnemyBrain enemyBrain;
        private ExchangeDirector director;
        private float staggerRemaining;
        private float baseFixedDelta;
        private float focusElapsed;
        private bool inFront;
        private float knifePenalty;
        private readonly SideBiasTracker sideBias = new SideBiasTracker();
        private readonly ThrowingKnifeState knife = new ThrowingKnifeState();
        // R8: 쉬움 보정 · 합 공격 방어
        private readonly Cooldown attackReentry = new Cooldown();
        private readonly EnemyAdaptation adaptation = new EnemyAdaptation();
        private float selectionAdaptation;   // 선택 시작 때 고정(표시 = 판정)
        private bool rewardCounter;          // 합 공격 방어 보상 반격 중
        private float defenseDamage;

        /// <summary>R8: 공격 재진입 대기(교환이 끝난 뒤 1.2초).</summary>
        public Cooldown AttackReentry => attackReentry;
        /// <summary>R8: 적 적응으로 오른 실패 확률(0~0.25).</summary>
        public float Adaptation => adaptation.Value;
        /// <summary>R8: 이번 선택에 적용되는 적응 값.</summary>
        public float SelectionAdaptation => selectionAdaptation;
        /// <summary>R8: 진행 중인 합 공격 방어(없으면 null).</summary>
        public DefenseSession Defense { get; private set; }
        private readonly Cooldown finisherCooldown = new Cooldown();
        private readonly Cooldown dashSlashCooldown = new Cooldown();
        private readonly GuardParry parry = new GuardParry();
        private readonly List<string> log = new List<string>();
        private readonly Flow.RunRecord record = new Flow.RunRecord();
        private bool paused;
        private bool resultReady;
        private const float EndSlowScale = 0.25f;     // S7: 끝 연출
        private const float EndSlowSeconds = 0.5f;

        public BattleState State { get; private set; } = BattleState.Realtime;
        public SelectionSession Session { get; private set; }
        public float FocusProgress => Mathf.Clamp01(focusElapsed / focusSeconds);
        public IReadOnlyList<string> Log => log;
        public PlayerStats Stats => stats;
        public Health PlayerHealth => playerHealth;
        public Health EnemyHealth => enemyHealth;
        public SideBiasTracker SideBias => sideBias;
        public ThrowingKnifeState Knife => knife;
        public Cooldown FinisherCooldown => finisherCooldown;
        public Cooldown DashSlashCooldown => dashSlashCooldown;
        public GuardParry Parry => parry;
        public PlayerDash Dash => dash;
        public bool InFront => inFront;
        public float KnifePenalty => knifePenalty;
        public float DistanceToEnemy => Vector3.ProjectOnPlane(enemy.position - player.position, Vector3.up).magnitude;
        /// <summary>현재 선택 진입 거리. 적 무방비 중에는 반격 거리 4m.</summary>
        public float AttackRange => EnemyStaggered ? counterRange : attackRange;
        public bool EnemyStaggered => staggerRemaining > 0f;
        /// <summary>적이 움직여도 되는지: 실시간이고 무방비가 아니며 양쪽 모두 살아 있을 때만.</summary>
        public bool EnemyMayAct => State == BattleState.Realtime && !paused && !EnemyStaggered && !playerHealth.IsDead && !enemyHealth.IsDead;
        public Enemy.EnemyBrain EnemyBrain => enemyBrain;
        public float StaggerRemaining => staggerRemaining;
        /// <summary>S6: 플레이어가 공중인지(착지 상태에만 맞는 적 공격 회피 판정).</summary>
        public bool PlayerAirborne => jump != null && jump.Airborne;
        /// <summary>S6: 이번 선택이 공중에서 시작됐는지(↓↙↘ 보너스).</summary>
        public bool AirStrike => airStrike;
        public void ShakeCamera(float amplitude, float seconds) => view.Shake(amplitude, seconds);
        /// <summary>플레이어 검 끝자세(선택 화면 표시용).</summary>
        public SwordStance Stance => director.Stance;
        /// <summary>R5: 이번 선택이 시작될 때의 검 자세. 성공률 표시와 판정이 같은 값을 쓴다.</summary>
        public AttackDirection SelectionStance => selectionStance;
        private AttackDirection selectionStance = SwordStance.Rest;

        /// <summary>S7 결과 화면용 전투 기록.</summary>
        public Flow.RunRecord Record => record;
        public bool Paused => paused;
        /// <summary>일시정지는 실시간에서만(선택·교환 연출 중에는 무시).</summary>
        public bool CanPause => State == BattleState.Realtime && !paused;
        /// <summary>끝 연출(0.5초 슬로모션)이 끝나 결과 화면을 띄워도 되는지.</summary>
        public bool ResultReady => resultReady;

        public void Configure(Transform owner, Transform target, ThirdPersonCamera cameraView, EncounterTrigger gate = null)
        { player = owner; enemy = target; view = cameraView; encounter = gate; }

        public void SetPaused(bool value)
        {
            if (value && !CanPause) return;
            if (!value && !paused) return;
            paused = value;
            BattleClock.SetPaused(value);   // R1: 메뉴 동안 대쉬 충전·무적·기록 시간도 정지
            Time.timeScale = value ? 0f : 1f;
            view.Capture(!value);
        }

        /// <summary>같은 스탯으로 다시 하기.</summary>
        public void Restart()
        {
            RestoreTime();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>타이틀로. openAllocation이면 스탯 배분 화면을 바로 연다.</summary>
        public void GoToTitle(bool openAllocation)
        {
            RestoreTime();
            Flow.RunSetup.OpenAllocation = openAllocation;
            SceneManager.LoadScene(Flow.RunSetup.TitleScene);
        }

        private void Awake()
        {
            stats = player.GetComponent<PlayerStats>();
            movement = player.GetComponent<PlayerMovement>();
            dash = player.GetComponent<PlayerDash>();
            jump = player.GetComponent<PlayerJump>();
            playerHealth = player.GetComponent<Health>();
            enemyHealth = enemy.GetComponent<Health>();
            enemyPose = enemy.GetComponent<Enemy.EnemyPose>();
            enemyBrain = enemy.GetComponent<Enemy.EnemyBrain>();
            BattleClock.Reset();
            director = new ExchangeDirector(player, enemy, view);
            if (dash != null) dash.Dashed += OnDashed;
            baseFixedDelta = Time.fixedDeltaTime;
            playerHealth.ParryCheck = CheckParry;
            playerHealth.Damaged += record.RecordDamageTaken;
            if (encounter == null) encounter = FindFirstObjectByType<EncounterTrigger>();
        }

        // 체력 3 가드 패링 구간 안에서 들어온 피해는 막고 쿨타임 일부를 돌려받는다.
        // 성공하면 적이 무방비(기지개) 상태가 되어 좌클릭 반격 1회가 가능하다.
        private bool CheckParry()
        {
            if (!parry.TryParry(stats.Tuning)) return false;
            staggerRemaining = staggerSeconds;
            if (enemyPose != null) enemyPose.SetStagger(true);
            if (enemyBrain != null) enemyBrain.OnParried();
            record.RecordParry();
            Sound.Play(Sfx.Parry, player.position + Vector3.up * 1.3f);
            AddLog("패링 성공 · 적 무방비 (좌클릭 반격)");
            return true;
        }

        private void EndStagger(string message)
        {
            if (!EnemyStaggered && (enemyPose == null || !enemyPose.Staggered)) return;
            staggerRemaining = 0f;
            if (enemyPose != null) enemyPose.SetStagger(false);
            if (message != null) AddLog(message);
        }

        private void OnDisable() { RestoreTime(); BattleClock.Reset(); }

        private void Update()
        {
            if (paused) return;
            float realDelta = Time.unscaledDeltaTime;
            if (encounter != null && encounter.Started) record.Begin(BattleClock.Now);
            knife.Tick(realDelta, stats.Tuning);
            finisherCooldown.Tick(realDelta);
            dashSlashCooldown.Tick(realDelta);
            parry.Tick(realDelta);
            if (State == BattleState.Realtime) { attackReentry.Tick(realDelta); adaptation.Tick(realDelta, stats.Tuning); }
            // 무방비 시간은 실시간에서만 흐른다(반격 선택 화면 동안 정지).
            if (State == BattleState.Realtime && EnemyStaggered)
            {
                staggerRemaining -= realDelta;
                if (!EnemyStaggered) EndStagger("무방비 해제");
            }

            if ((State == BattleState.Victory || State == BattleState.Defeat) && Input.GetKeyDown(KeyCode.R))
            {
                Restart();
                return;
            }
            switch (State)
            {
                case BattleState.Realtime:
                    director.TickRealtime(realDelta, LocalOf(movement.LastMoveDirection), PlayerAirborne, stats.Tuning);
                    UpdateRealtime();
                    break;
                case BattleState.Focus:
                    focusElapsed += realDelta;
                    if (focusElapsed >= focusSeconds) State = BattleState.Planning;   // 타이머는 포커스 완료 후 시작
                    break;
                case BattleState.Planning: UpdatePlanning(realDelta); break;
                case BattleState.Defend: UpdateDefense(realDelta); break;
            }
        }

        // 월드 수평 방향 → 캐릭터 기준(x 오른쪽, y 앞)
        private Vector2 LocalOf(Vector3 world)
        {
            Vector3 local = player.InverseTransformDirection(Vector3.ProjectOnPlane(world, Vector3.up));
            return new Vector2(local.x, local.z);
        }

        private void OnDashed(Vector3 worldDirection)
        {
            if (State == BattleState.Realtime) director.OnDash(LocalOf(worldDirection), stats.Tuning);
        }

        private void UpdateRealtime()
        {
            if (!view.Captured || (dash != null && dash.Dashing)) return;
            // 체력 3 우클릭 가드: 실시간 전투에서만.
            if (Input.GetMouseButtonDown(1) && parry.TryStart(stats.Stats, stats.Tuning))
            {
                AddLog("가드");
                StartCoroutine(director.PlayGuard(stats.Tuning.parryWindow));
                return;
            }
            if (!Input.GetMouseButtonDown(0)) return;
            if (DistanceToEnemy > AttackRange) { StartCoroutine(AirSwing()); return; }
            // R8: 교환 직후 1.2초는 다시 공격 선택에 들어갈 수 없다(무방비 반격은 예외)
            if (!attackReentry.Ready && !EnemyStaggered) { AddLog("자세를 가다듬는 중"); return; }
            BeginSelection();
        }

        private void BeginSelection(bool reward = false)
        {
            rewardCounter = reward;
            selectionAdaptation = reward || EnemyStaggered ? 0f : adaptation.Value;   // 확정 성공 반격은 적응 무관
            CharacterStats s = stats.Stats;
            Vector3 toEnemy = Vector3.ProjectOnPlane(enemy.position - player.position, Vector3.up);
            if (toEnemy.sqrMagnitude > 0.001f) player.rotation = Quaternion.LookRotation(toEnemy);
            inFront = StatEffects.IsInFront(enemy.position, enemy.forward, player.position);
            knifePenalty = knife.ConsumeNextAttackPenalty(stats.Tuning);
            airStrike = PlayerAirborne;
            selectionStance = director.Stance.Current;   // R5: 이번 선택의 검 시작 자세(표시·판정 공통)
            movement.Suspended = true;   // S6: 공중 베기 시 선택·교환 동안 공중에 멈춤
            bool finisherReady = StatEffects.HasFinisher(s) && finisherCooldown.Ready;
            bool dashSlashReady = StatEffects.HasDashSlash(s) && dashSlashCooldown.Ready;
            // 적 무방비 중이면 반격 선택: 1타, 비도·베기 불가, 무조건 성공.
            Session = new SelectionSession(s, stats.Tuning, finisherReady, dashSlashReady, EnemyStaggered || reward);
            // 첫 클릭이 비도로 오인되지 않도록 커서를 위쪽에서 시작한다.
            Session.SetCursor(new Vector2(0f, 0.6f));
            focusElapsed = 0f;
            Time.timeScale = selectionTimeScale;
            Time.fixedDeltaTime = baseFixedDelta * selectionTimeScale;
            playerHealth.Invulnerable = true;
            movement.CombatLocked = true;
            if (AudioService.Instance != null) AudioService.Instance.Muffled = true;   // S8-4: 선택 동안 배경음 먹먹하게
            view.SetFocus(true);
            State = BattleState.Focus;
        }

        private void UpdatePlanning(float realDelta)
        {
            Session.MoveCursor(new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")), Flow.GameSettings.CursorSensitivity);
            if (Input.GetMouseButtonDown(0)) Session.Click();
            if (Input.GetMouseButtonDown(1)) Session.RequestDashSlash();   // 특수기 2: 좌클릭으로 들어간 선택 중에만
            Session.Tick(realDelta);
            if (Session.End != SelectionEnd.None) StartCoroutine(Exchange(Session));
        }

        private IEnumerator Exchange(SelectionSession session)
        {
            State = BattleState.Exchange;
            RestoreTime();
            playerHealth.Invulnerable = false;
            if (AudioService.Instance != null) AudioService.Instance.Muffled = false;
            CharacterStats s = stats.Stats;
            StatTuning t = stats.Tuning;
            bool anySuccess = false;

            if (session.End == SelectionEnd.KnifeThrown)
            {
                StrikeOutcome o = CombatResolver.ResolveKnife(knife, s, t, () => Random.value);
                yield return director.PlayKnife(o, Apply);
                anySuccess = o.success;
            }
            else if (session.End == SelectionEnd.DashSlash)
            {
                StrikeOutcome o = CombatResolver.ResolveDashSlash(s, t);
                dashSlashCooldown.Start(StatEffects.SkillCooldown(s, t, t.dashSlashCooldown));
                yield return director.PlayDashSlash(o, Apply, dashSlashSeconds, dashSlashOvershoot);
                anySuccess = true;
            }
            else if (session.Reserved.Count == 0)
            {
                if (session.Counter) AddLog("반격 기회 놓침");
                else if (s.Speed <= 1)
                {
                    float hurt = playerHealth.TakeDamage(timeoutCounterDamage, true);
                    Sound.Play(Sfx.Hurt, player.position + Vector3.up);
                    AddLog($"시간 초과: 적의 반격 -{hurt:0}");
                }
                else AddLog("시간 초과: 공격 취소");
            }
            else
            {
                if (session.End == SelectionEnd.TimedOut && session.Reserved.Count < session.MaxHits)
                    AddLog($"시간 초과: 예약한 {session.Reserved.Count}타만 실행");
                List<StrikeOutcome> outcomes = CombatResolver.Resolve(session.Reserved, s, t, inFront, sideBias, knifePenalty, () => Random.value, session.Counter, airStrike, selectionStance, selectionAdaptation);
                foreach (StrikeOutcome o in outcomes)
                {
                    anySuccess |= o.success;
                    // R8 적 적응: 성공한 타마다 다음 공격 실패 확률↑(방어 보상 반격은 절반)
                    if (o.success) adaptation.Add(rewardCounter ? t.adaptCounterHit : t.adaptPerHit, t);
                    if (o.finisher) finisherCooldown.Start(StatEffects.SkillCooldown(s, t, t.finisherCooldown));
                }
                yield return director.PlayStrikes(outcomes, Apply, () => enemyHealth.IsDead || playerHealth.IsDead, finisherSpinSeconds);
            }

            if (session.Counter) EndStagger(null);   // 반격은 1회뿐
            // R8 턴 교대: 일반 공격이 끝나면 적이 바로 합 공격으로 반격(반격·보상 반격 뒤에는 평소대로)
            if (enemyBrain != null) enemyBrain.OnExchangeFinished(anySuccess, !session.Counter);
            attackReentry.Start(t.attackReentrySeconds);
            rewardCounter = false;
            view.SetFocus(false);
            movement.CombatLocked = false;
            movement.Suspended = false;   // 공중이었다면 이제 착지
            airStrike = false;
            Session = null;
            if (enemyHealth.IsDead) EndBattle(BattleState.Victory);
            else if (playerHealth.IsDead) EndBattle(BattleState.Defeat);
            else State = BattleState.Realtime;
        }

        /// <summary>접촉 순간 1회 호출되어 피해를 적용한다(연출 쪽에서 재계산 없음).</summary>
        private void Apply(StrikeOutcome o)
        {
            record.RecordStrike(o.success);
            float dealt = enemyHealth.TakeDamage(o.damageToEnemy);
            float taken = playerHealth.TakeDamage(o.damageToPlayer);
            string name = o.knife ? "비도" : o.dashSlash ? "지나가며 베기" : o.finisher ? "마무리 " + DirectionText.Arrow(o.direction) : DirectionText.Arrow(o.direction);
            if (o.success) AddLog($"{name} 성공{(o.critical ? " 치명타" : "")} -{dealt:0}");
            else if (o.knife) AddLog("비도 빗나감");
            else AddLog($"{name} 막힘" + (taken > 0f ? $" (반동 -{taken:0})" : dealt > 0f ? $" (적 -{dealt:0})" : ""));
        }

        // ---------------- R8 합 공격 방어 ----------------

        /// <summary>적 합 공격 실행 시 호출. 실시간일 때만 방어 입력을 연다(슬로모션 · 휠 3칸 깜빡임).</summary>
        public bool BeginDefense(Enemy.EnemyBrain brain, float damagePerHit)
        {
            if (State != BattleState.Realtime || paused || playerHealth.IsDead || enemyHealth.IsDead) return false;
            Defense = new DefenseSession(stats.Stats, stats.Tuning, () => Random.value);
            defenseDamage = damagePerHit;
            Vector3 toEnemy = Vector3.ProjectOnPlane(enemy.position - player.position, Vector3.up);
            if (toEnemy.sqrMagnitude > 0.001f) player.rotation = Quaternion.LookRotation(toEnemy);
            movement.Suspended = true;
            movement.CombatLocked = true;
            Time.timeScale = selectionTimeScale;
            Time.fixedDeltaTime = baseFixedDelta * selectionTimeScale;
            playerHealth.Invulnerable = true;
            if (AudioService.Instance != null) AudioService.Instance.Muffled = true;
            view.SetFocus(true);
            AddLog("적 합 공격! 붉게 빛난 순서대로 막기");
            State = BattleState.Defend;
            return true;
        }

        private void UpdateDefense(float realDelta)
        {
            Defense.MoveCursor(new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")), Flow.GameSettings.CursorSensitivity);
            if (Input.GetMouseButtonDown(0)) Defense.Click();
            Defense.Tick(realDelta);
            if (Defense.Done) StartCoroutine(DefenseExchange(Defense));
        }

        private IEnumerator DefenseExchange(DefenseSession d)
        {
            State = BattleState.Exchange;
            RestoreTime();
            playerHealth.Invulnerable = false;
            if (AudioService.Instance != null) AudioService.Instance.Muffled = false;
            yield return director.PlayDefense(d.Pattern, d.Results, i =>
            {
                if (d.Results[i]) { AddLog($"합 {i + 1}타 {DirectionText.Arrow(d.Pattern[i])} 막음"); return; }
                float hurt = playerHealth.TakeDamage(defenseDamage, true);   // 방어 입력 실패는 패링·무적과 무관
                record.RecordStrike(false);
                Sound.Play(Sfx.Hurt, player.position + Vector3.up);
                AddLog($"합 {i + 1}타 {DirectionText.Arrow(d.Pattern[i])} 맞음 -{hurt:0}");
            }, () => playerHealth.IsDead);
            bool all = d.AllBlocked;
            if (enemyBrain != null) enemyBrain.OnDefenseFinished(all);
            Defense = null;
            view.SetFocus(false);
            movement.CombatLocked = false;
            movement.Suspended = false;
            if (playerHealth.IsDead) { EndBattle(BattleState.Defeat); yield break; }
            State = BattleState.Realtime;
            if (all)
            {
                // 3개 모두 막음 → 반격 1회(확정 성공, 적응은 덜 오름)
                AddLog("합 공격을 모두 막음 · 반격!");
                BeginSelection(true);
            }
        }

        /// <summary>R8 밀쳐 차기: 플레이어를 dir로 distance만큼 밀어낸다(벽·소품 충돌 존중).</summary>
        public void KnockPlayer(Vector3 dir, float distance, float seconds) => StartCoroutine(Knock(dir, distance, seconds));

        private IEnumerator Knock(Vector3 dir, float distance, float seconds)
        {
            var body = player.GetComponent<CharacterController>();
            if (body == null) yield break;
            for (float time = 0f; time < seconds; time += Time.deltaTime)
            {
                body.Move(dir * (distance / seconds) * Time.deltaTime);
                yield return null;
            }
        }

        /// <summary>R8: 실시간 중 적 공격에 맞았을 때 플레이어 몸 반응.</summary>
        public void PlayerHurtVisual() => director.PlayerHurt(enemy.position);

        /// <summary>실시간 중 적 공격으로 플레이어가 쓰러졌을 때 호출.</summary>
        public void NotifyPlayerDefeated()
        {
            if (State == BattleState.Realtime && playerHealth.IsDead) EndBattle(BattleState.Defeat);
        }

        private void EndBattle(BattleState result)
        {
            EndStagger(null);
            State = result;
            movement.CombatLocked = true;
            AddLog(result == BattleState.Victory ? "승리" : "패배");
            // S8-2: 애니메이션 캐릭터면 쓰러짐·승리 동작, 블록 인형이면 기존처럼 넘어뜨린다.
            var loser = (result == BattleState.Victory ? enemy : player).GetComponentInChildren<AnimatorRig>();
            var winner = (result == BattleState.Victory ? player : enemy).GetComponentInChildren<AnimatorRig>();
            if (loser != null) loser.Cue(FighterCue.Death);
            else if (result == BattleState.Victory) enemy.rotation *= Quaternion.Euler(80f, 0f, 0f);
            if (winner != null) winner.Cue(FighterCue.Victory);
            record.Finish(BattleClock.Now);
            StartCoroutine(EndSlowMotion());
        }

        // S7: 마지막 타격 직후 0.5초 슬로모션 → 결과 화면.
        private IEnumerator EndSlowMotion()
        {
            Time.timeScale = EndSlowScale;
            for (float time = 0f; time < EndSlowSeconds; time += Time.unscaledDeltaTime) yield return null;
            RestoreTime();
            view.Capture(false);
            resultReady = true;
            Sound.Play(State == BattleState.Victory ? Sfx.Victory : Sfx.Defeat);
            if (AudioService.Instance != null) AudioService.Instance.MusicDuck = 0.35f;
        }

        private void RestoreTime()
        {
            Time.timeScale = 1f;
            if (baseFixedDelta > 0f) Time.fixedDeltaTime = baseFixedDelta;
        }

        public void AddLog(string line)
        {
            log.Add(line);
            if (log.Count > 6) log.RemoveAt(0);
        }

        private IEnumerator AirSwing()
        {
            State = BattleState.Exchange;
            yield return director.PlayAirSwing();
            if (State == BattleState.Exchange) State = BattleState.Realtime;
        }
    }

    /// <summary>방향 표시 글자.</summary>
    public static class DirectionText
    {
        private static readonly string[] Arrows = { "↑", "↗", "→", "↘", "↓", "↙", "←", "↖" };
        private static readonly string[] Names = { "위", "오른쪽 위", "오른쪽", "오른쪽 아래", "아래", "왼쪽 아래", "왼쪽", "왼쪽 위" };
        public static string Arrow(AttackDirection d) => Arrows[(int)d];
        public static string Name(AttackDirection d) => Names[(int)d];
    }
}
