using UnityEngine;
using SwordPrototype.Battle;
using SwordPrototype.Audio;

namespace SwordPrototype.Enemy
{
    public enum EnemyState { Dormant, Chase, Telegraph, Attack, Recovery, Dead }

    /// <summary>
    /// 적 FSM (S4 설계 1.0). 대기 → 추적 → 예고 → 공격 → 후딜 → 추적.
    /// BattleFlow.EnemyMayAct가 false인 동안(플레이어 선택·교환, 패링 무방비)에는 정지한다.
    /// 판정은 예고에 보여준 AttackShape 그대로 한 번만 한다.
    /// </summary>
    public sealed class EnemyBrain : MonoBehaviour
    {
        private const float PlayerRadius = 0.35f;
        private const float BodyRadius = 0.6f;   // 소품 파괴용 적 몸통 반경

        [SerializeField] private BattleFlow flow;
        [SerializeField] private Transform player;
        [SerializeField] private EncounterTrigger encounter;
        [SerializeField] private EnemyMoveset moveset = new EnemyMoveset();

        private Health health;
        private EnemyPose pose;
        private TelegraphView telegraph;
        private float timer;
        private float gap;
        private float attackElapsed;
        private float attackDuration;
        private bool hasLast;
        private EnemyAttackKind last;
        private int streak;
        private Vector3 moveStart;
        private Vector3 moveEnd;

        public EnemyState State { get; private set; } = EnemyState.Dormant;
        public EnemyAttackDefinition Current { get; private set; }
        public EnemyMoveset Moveset => moveset;

        public string StateText
        {
            get
            {
                switch (State)
                {
                    case EnemyState.Dormant: return "대기";
                    case EnemyState.Chase: return "추적";
                    case EnemyState.Telegraph: return "예고 · " + Current.displayName;
                    case EnemyState.Attack: return "공격 · " + Current.displayName;
                    case EnemyState.Recovery: return "후딜";
                    default: return "쓰러짐";
                }
            }
        }

        public void Configure(BattleFlow battle, Transform target, EncounterTrigger gate)
        { flow = battle; player = target; encounter = gate; }

        private void Awake()
        {
            health = GetComponent<Health>();
            pose = GetComponent<EnemyPose>();
            telegraph = new GameObject("EnemyTelegraph").AddComponent<TelegraphView>();
        }

        private void Update()
        {
            if (State == EnemyState.Dead) return;
            if (health != null && health.IsDead) { Die(); return; }
            if (flow == null || !flow.EnemyMayAct) return;   // 정지
            float dt = Time.deltaTime;
            switch (State)
            {
                case EnemyState.Dormant:
                    if (encounter == null || encounter.Started) { State = EnemyState.Chase; gap = moveset.attackGap; }
                    break;
                case EnemyState.Chase: UpdateChase(dt); break;
                case EnemyState.Telegraph: UpdateTelegraph(dt); break;
                case EnemyState.Attack: UpdateAttack(dt); break;
                case EnemyState.Recovery:
                    timer -= dt;
                    if (timer <= 0f) { pose?.ResetPose(); State = EnemyState.Chase; gap = moveset.attackGap; }
                    break;
            }
            ClampToArena();
        }

        private float DistanceToPlayer => Flat(player.position - transform.position).magnitude;

        private void UpdateChase(float dt)
        {
            Face(player.position, dt);
            Ground();
            gap -= dt;
            if (gap > 0f)
            {
                if (DistanceToPlayer > 1.5f) transform.position += Flat(player.position - transform.position).normalized * moveset.moveSpeed * dt;
                World.PropBreaker.BreakTouching(transform.position, BodyRadius);   // S6: 추적 중 부딪힌 소품을 부숨(우회 없음)
                return;
            }
            if (forcedCounter) { forcedCounter = false; StartAttack(EnemyAttackKind.ComboAssault); return; }   // R8 턴 교대
            StartAttack(moveset.Choose(DistanceToPlayer, hasLast, last, streak, Random.value, Random.value));
        }

        private bool forcedCounter;

        private void StartAttack(EnemyAttackKind kind)
        {
            streak = hasLast && last == kind ? streak + 1 : 1;
            last = kind;
            hasLast = true;
            Current = moveset.Get(kind);
            timer = Current.telegraphSeconds;
            moveStart = transform.position;
            moveEnd = kind == EnemyAttackKind.LeapSlam ? Ground(player.position)
                : kind == EnemyAttackKind.RetreatShot ? moveStart - Flat(player.position - moveStart).normalized * Current.travel
                : moveStart;
            State = EnemyState.Telegraph;
            pose?.SetWindup(kind);
            Sound.Play(Sfx.Cue(kind), transform.position + Vector3.up * 1.5f);   // S8-4: 패턴별 예고음(적 위치)
        }

        private void UpdateTelegraph(float dt)
        {
            timer -= dt;
            bool locked = timer <= moveset.lockBeforeHit;
            float progress = 1f - Mathf.Clamp01(timer / Current.telegraphSeconds);
            if (!locked)
            {
                // 고정 전까지는 플레이어를 따라간다.
                Face(player.position, dt);
                if (Current.kind == EnemyAttackKind.LeapSlam) moveEnd = Ground(player.position);
            }
            if (Current.kind == EnemyAttackKind.LeapSlam || Current.kind == EnemyAttackKind.RetreatShot)
            {
                float height = Current.kind == EnemyAttackKind.LeapSlam ? moveset.leapHeight : 1f;
                transform.position = Vector3.Lerp(moveStart, moveEnd, progress) + Vector3.up * Mathf.Sin(progress * Mathf.PI) * height;
                if (Current.kind == EnemyAttackKind.RetreatShot && !locked) Face(player.position, 999f);
            }
            telegraph.Show(Current.shape, ShapeOrigin(), transform.forward, progress, locked);
            if (timer <= 0f) Execute();
        }

        private Vector3 ShapeOrigin() => Current.kind == EnemyAttackKind.LeapSlam ? moveEnd : Ground(transform.position);

        private void Execute()
        {
            telegraph.Hide();
            State = EnemyState.Attack;
            attackElapsed = 0f;
            attackDuration = 0.2f;
            pose?.SetStrike(Current.kind);
            // TryHit 안에서 패링되면 OnParried가 상태를 후딜로 바꾼다.
            // S6: 공격 범위(예고와 같은 형태)에 걸린 소품을 부순다.
            switch (Current.kind)
            {
                case EnemyAttackKind.Sweep:
                    Sound.Play(Sfx.Swing, transform.position + Vector3.up * 1.5f, 0.7f);
                    World.PropBreaker.BreakInShape(Current.shape, Ground(transform.position), transform.forward);
                    TryHit(Ground(transform.position));
                    break;
                case EnemyAttackKind.ChargeSlash:
                    Sound.Play(Sfx.Dash, transform.position, 0.75f);
                    moveStart = Ground(transform.position);
                    moveEnd = moveStart + Flat(transform.forward).normalized * Current.travel;
                    attackDuration = moveset.chargeSeconds;
                    World.PropBreaker.BreakInShape(Current.shape, moveStart, transform.forward);
                    TryHit(moveStart);
                    break;
                case EnemyAttackKind.LeapSlam:
                    transform.position = moveEnd;
                    World.PropBreaker.BreakInShape(Current.shape, moveEnd, transform.forward);
                    World.GroundScar.Spawn(moveEnd, Current.shape.radius * 0.6f);
                    Sound.Play(Sfx.LeapLand, moveEnd);
                    Sound.Play(Sfx.HeavyDrum, moveEnd, 0.85f);
                    flow.ShakeCamera(0.2f, 0.3f);
                    TryHit(moveEnd);
                    break;
                case EnemyAttackKind.SpinSlash:   // R8: 제자리 원형(점프로 회피)
                    Sound.Play(Sfx.SpinSwing, transform.position + Vector3.up * 1.5f, 0.7f);
                    World.PropBreaker.BreakInShape(Current.shape, Ground(transform.position), transform.forward);
                    TryHit(Ground(transform.position));
                    attackDuration = 0.35f;
                    break;
                case EnemyAttackKind.Kick:        // R8: 맞으면 밀려남
                    Sound.Play(Sfx.Swing, transform.position + Vector3.up, 1.3f);
                    if (TryHit(Ground(transform.position))) flow.KnockPlayer(Flat(player.position - transform.position).normalized, Current.travel, 0.2f);
                    break;
                case EnemyAttackKind.ComboAssault: // R8: 합 공격 — 플레이어 방어 입력으로 넘김
                    if (DistanceToPlayer <= Current.maxDistance && !flow.PlayerAirborne && flow.BeginDefense(this, Current.damage))
                        attackDuration = 0f;
                    else flow.AddLog($"적 {Current.displayName}: 빗나감");
                    break;
                case EnemyAttackKind.Thrust:      // R8: 빠른 직선 돌진
                    Sound.Play(Sfx.Dash, transform.position, 0.9f);
                    moveStart = Ground(transform.position);
                    moveEnd = moveStart + Flat(transform.forward).normalized * Current.travel;
                    attackDuration = moveset.thrustSeconds;
                    World.PropBreaker.BreakInShape(Current.shape, moveStart, transform.forward);
                    TryHit(moveStart);
                    break;
                case EnemyAttackKind.FanThrow:    // R8: 비도 3개 부채꼴
                    Ground();
                    for (int i = -1; i <= 1; i++)
                        EnemyProjectile.Spawn(flow, player, transform.position + Vector3.up * 1.3f + transform.forward * 0.6f,
                            Quaternion.AngleAxis(i * moveset.fanSpreadDegrees, Vector3.up) * transform.forward,
                            moveset.projectileSpeed, moveset.projectileRadius, Current.damage, Current.displayName);
                    break;
                default:   // 투척 · 후퇴 사격
                    Ground();
                    EnemyProjectile.Spawn(flow, player, transform.position + Vector3.up * 1.3f + transform.forward * 0.6f, transform.forward,
                        moveset.projectileSpeed, moveset.projectileRadius, Current.damage, Current.displayName);
                    break;
            }
        }

        private void UpdateAttack(float dt)
        {
            attackElapsed += dt;
            if (Current.kind == EnemyAttackKind.ChargeSlash || Current.kind == EnemyAttackKind.Thrust)
            {
                transform.position = Vector3.Lerp(moveStart, moveEnd, Mathf.Clamp01(attackElapsed / Mathf.Max(0.01f, attackDuration)));
                World.PropBreaker.BreakTouching(transform.position, BodyRadius);
            }
            if (attackElapsed >= attackDuration) { State = EnemyState.Recovery; timer = Current.recoverySeconds; }
        }

        /// <summary>범위 판정 후 피격 처리. 실제로 맞았는지(패링·무적 제외 전) 돌려준다.</summary>
        private bool TryHit(Vector3 origin)
        {
            if (!Current.shape.Contains(origin, transform.forward, player.position, PlayerRadius))
                flow.AddLog($"적 {Current.displayName}: 빗나감");
            else if (Current.groundOnly && flow.PlayerAirborne)
                flow.AddLog($"적 {Current.displayName}: 점프로 회피");   // S6: 착지 상태에만 맞는 공격
            else
                return ReportHit(flow, Current.displayName, Current.damage) > 0f;
            return false;
        }

        /// <summary>플레이어 피격 처리 공통 경로(근접·투사체). 패링·무적은 Health가 처리한다.</summary>
        public static float ReportHit(BattleFlow flow, string label, float damage)
        {
            bool parrying = flow.Parry.IsParrying;
            bool invulnerable = flow.PlayerHealth.IsInvulnerable;
            float dealt = flow.PlayerHealth.TakeDamage(damage);
            if (dealt > 0f) { flow.AddLog($"적 {label} 적중 -{dealt:0}"); Sound.Play(Sfx.Hurt, flow.PlayerHealth.transform.position + Vector3.up); flow.PlayerHurtVisual(); }
            else if (!parrying) flow.AddLog(invulnerable ? $"적 {label}: 무적으로 회피" : $"적 {label}: 피해 없음");
            if (flow.PlayerHealth.IsDead) flow.NotifyPlayerDefeated();
            return dealt;
        }

        /// <summary>패링 성공: 진행 중이던 공격 취소. 무방비(정지)가 끝난 뒤 짧은 후딜.</summary>
        public void OnParried()
        {
            if (State == EnemyState.Dead) return;
            telegraph.Hide();
            Ground();
            State = EnemyState.Recovery;
            timer = moveset.parryRecoverySeconds;
        }

        /// <summary>플레이어 교환 종료. 하나라도 성공하면 공격 취소 + 경직, 전부 막히면 예고를 이어서 진행.</summary>
        /// <param name="turnCounter">R8 턴 교대: true면 짧은 후딜 뒤 바로 합 공격으로 반격.</param>
        public void OnExchangeFinished(bool anySuccess, bool turnCounter = false)
        {
            if (State == EnemyState.Dead || State == EnemyState.Dormant) return;
            if (turnCounter)
            {
                telegraph.Hide();
                Ground();
                if (anySuccess) pose?.SetHitStun();
                State = EnemyState.Recovery;
                timer = anySuccess ? moveset.hitStunSeconds : moveset.turnCounterDelay;
                forcedCounter = true;
                return;
            }
            if (anySuccess)
            {
                telegraph.Hide();
                Ground();
                pose?.SetHitStun();
                State = EnemyState.Recovery;
                timer = moveset.hitStunSeconds;
            }
            else if (State == EnemyState.Telegraph)
            {
                timer = Mathf.Max(timer, moveset.minResumeTelegraph);
                pose?.SetWindup(Current.kind);   // 교환 연출이 자세를 초기화했으므로 예고 자세 복원
            }
        }

        /// <summary>R8: 합 공격 방어가 끝남. 모두 막혔으면 무방비(반격은 BattleFlow가 연다), 아니면 후딜.</summary>
        public void OnDefenseFinished(bool allBlocked)
        {
            if (State == EnemyState.Dead) return;
            Ground();
            State = EnemyState.Recovery;
            timer = allBlocked ? moveset.hitStunSeconds : Current.recoverySeconds;
            gap = moveset.attackGap;
        }

        private void Die()
        {
            State = EnemyState.Dead;
            telegraph.Hide();
        }

        private void Face(Vector3 target, float dt)
        {
            Vector3 dir = Flat(target - transform.position);
            if (dir.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), moveset.turnDegreesPerSecond * dt);
        }

        private void ClampToArena()
        {
            Vector3 flat = Flat(transform.position);
            if (flat.magnitude > moveset.arenaRadius)
            {
                flat = flat.normalized * moveset.arenaRadius;
                transform.position = new Vector3(flat.x, transform.position.y, flat.z);
            }
        }

        private void Ground() => transform.position = Ground(transform.position);
        private static Vector3 Ground(Vector3 p) => new Vector3(p.x, 0f, p.z);
        private static Vector3 Flat(Vector3 v) => Vector3.ProjectOnPlane(v, Vector3.up);

        private void OnDestroy() { if (telegraph != null) Destroy(telegraph.gameObject); }
    }
}
