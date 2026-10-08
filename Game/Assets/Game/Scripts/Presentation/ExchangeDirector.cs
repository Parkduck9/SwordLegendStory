using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SwordPrototype.Battle;
using SwordPrototype.Audio;

namespace SwordPrototype.Presentation
{
    /// <summary>
    /// 교환 연출(S5 설계 1.0). 이미 판정된 결과 목록을 시간표대로 재생한다.
    /// 준비 → 궤적 → 접촉(apply 콜백으로 피해 1회 · 히트스톱) → 반응. 마지막 타 반응이 분리 연출.
    /// 확률을 다시 계산하지 않는다. 모든 시간은 실제 시간.
    /// </summary>
    public sealed class ExchangeDirector
    {
        private readonly Transform player;
        private readonly Transform enemy;
        private readonly CharacterController body;
        private readonly IFighterVisual playerRig;
        private readonly IFighterVisual enemyRig;
        private readonly ThirdPersonCamera view;
        private readonly HitFeedback feedback = new HitFeedback();

        public SwordStance Stance { get; } = new SwordStance();

        public ExchangeDirector(Transform player, Transform enemy, ThirdPersonCamera view)
        {
            this.player = player;
            this.enemy = enemy;
            this.view = view;
            body = player.GetComponent<CharacterController>();
            playerRig = player.GetComponentInChildren<IFighterVisual>();
            enemyRig = enemy.GetComponentInChildren<IFighterVisual>();
        }

        /// <summary>
        /// 실시간 중 매 프레임. R5: 움직임·점프에 따라 검 자세가 바뀌면 검을 그 자세로 옮겨 든다
        /// (멈춘 채 일정 시간이면 기본 자세). localMove는 캐릭터 기준(x 오른쪽, y 앞).
        /// </summary>
        public void TickRealtime(float deltaTime, Vector2 localMove, bool airborne, StatTuning tuning)
        {
            if (Stance.TickRealtime(deltaTime, localMove, airborne, tuning) || !stanceShown) { stanceShown = true; ShowStance(); }
        }
        private bool stanceShown;   // 첫 실시간 프레임에 기본 겨눔으로

        /// <summary>R5: 대쉬 시작 — 대쉬 반대쪽 자세로 즉시.</summary>
        public void OnDash(Vector2 localDirection, StatTuning tuning)
        {
            if (Stance.ApplyDash(localDirection, tuning)) ShowStance();
        }

        // R6: 판정 자세를 실제 검술 겨눔 자세(Kamae 표)로 보여 준다.
        private void ShowStance() => Rig(playerRig, r => r.HoldStance(Stance.Current));

        /// <summary>R6: 적 베임 표현. 칼이 지나간 방향(플레이어 화면 기준 from→to)을 월드로 바꿔 넘긴다.</summary>
        private void WoundEnemy(StrikeOutcome o, Vector2 from, Vector2 to)
        {
            Vector2 c = (to - from).sqrMagnitude > 0.01f ? (to - from).normalized : to;
            Vector3 cut = player.right * c.x + Vector3.up * c.y;
            int tier = Kamae.WoundTier(o);
            Rig(enemyRig, r => r.Wound(cut, tier));
            if (tier == 1) Rig(enemyRig, r => r.Cue(FighterCue.Hit));
            else if (tier == 2) Rig(enemyRig, r => r.Cue(FighterCue.Knockback));
            if (tier >= 1) feedback.Burst(enemy.position + Vector3.up * 1.3f, HitFeedback.Fragment, tier * 8);   // 먹 튀김
        }

        // ---------------- 일반 타격 ----------------

        public IEnumerator PlayStrikes(List<StrikeOutcome> outcomes, Action<StrikeOutcome> apply, Func<bool> ended, float finisherSpinSeconds)
        {
            for (int i = 0; i < outcomes.Count; i++)
            {
                StrikeOutcome o = outcomes[i];
                bool last = i == outcomes.Count - 1;
                Vector2 from = SelectionSession.ToVector(Stance.Current);
                Vector2 to = SelectionSession.ToVector(o.direction);
                bool spin = Stance.IsSpin(o.direction);
                if (o.finisher) view.PlaySpin(finisherSpinSeconds);
                FaceEachOther();
                Rig(playerRig, r => r.SetTrail(true));
                Rig(playerRig, r => r.Cue(FighterCue.Swing));
                // S8-4: 베기 바람(방향마다 음높이 조금씩 다름), 같은 방향이면 회전 베기 소리
                Sound.Play(spin ? Sfx.SpinSwing : Sfx.Swing, player.position + Vector3.up * 1.3f, 0.9f + (int)o.direction * 0.03f);

                // 준비: 현재 끝자세에서 살짝 뒤로 당긴다.
                yield return Run(ReactionLibrary.Windup, t => Rig(playerRig, r => r.SetBlade(FighterRig.BladeDirection(from, 0.2f - 0.4f * t))));
                // 궤적: 같은 방향이면 몸 전체를 한 바퀴 돌리며 반대편에서 D로 벤다.
                if (spin)
                {
                    // 몸 전체 회전: 캐릭터 루트를 돌려 블록 인형·애니메이션 캐릭터 모두 같은 회전을 보인다.
                    // 카메라는 회전 전 구도에 고정(같이 돌면 어지러움 — Play 의견).
                    Quaternion facing = player.rotation;
                    view.HoldFacing(true);
                    Rig(playerRig, r => r.Cue(FighterCue.Spinning));   // R8: 몸통·팔 회전 동작(KayKit)
                    yield return Run(ReactionLibrary.SpinSwing, t =>
                    {
                        player.rotation = facing * Quaternion.Euler(0f, 360f * t, 0f);
                        Rig(playerRig, r => r.SetBlade(FighterRig.SwingDirection(-to, to, t)));
                    });
                    player.rotation = facing;
                    view.HoldFacing(false);
                }
                else
                    yield return Run(ReactionLibrary.Swing, t => Rig(playerRig, r => r.SetBlade(FighterRig.SwingDirection(from, to, t))));
                Rig(playerRig, r => r.ResetBody());
                Stance.Commit(o.direction);

                // 접촉: 피해 1회 + 타격감
                apply(o);
                Vector3 contact = Vector3.Lerp(player.position, enemy.position, 0.6f) + Vector3.up * 1.3f;
                if (o.success)
                {
                    bool heavy = o.critical || o.finisher;
                    Sound.Play(heavy ? Sfx.HeavyHit : Sfx.Hit, enemy.position + Vector3.up * 1.2f);
                    if (heavy) Sound.Play(Sfx.HeavyDrum);
                    feedback.Burst(enemy.position + Vector3.up * 1.2f, HitFeedback.Fragment, heavy ? 24 : 12);
                    view.Shake(heavy ? 0.15f : 0.06f, 0.15f);
                    WoundEnemy(o, spin ? -to : from, to);   // R6: 스침 · 베임 · 깊게
                    yield return Wait(heavy ? ReactionLibrary.StopCrit : ReactionLibrary.StopHit);
                    if (o.critical) yield return Crit(ReactionLibrary.ChooseCrit(o.direction), from, to);
                }
                else
                {
                    Rig(enemyRig, r => r.Cue(FighterCue.Block));
                    Rig(enemyRig, r => r.SetBlade(FighterRig.BladeDirection(new Vector2(-to.x, to.y), 0.8f)));   // 적은 마주 보므로 좌우 반전
                    Sound.Play(Sfx.Clash, contact);
                    feedback.Burst(contact, HitFeedback.Spark, 18);
                    view.Shake(0.05f, 0.1f);
                    yield return Wait(ReactionLibrary.StopClash);
                }
                Rig(playerRig, r => r.SetTrail(false));
                if (ended()) break;
                if (last) yield return o.success ? Success(o) : Fail(o, to);
                else yield return Mid(o);
            }
            Rig(enemyRig, r => r.ResetPose());
            Rig(playerRig, r => r.ResetBody());
            Rig(enemyRig, r => r.Cue(FighterCue.Idle));
            Rig(playerRig, r => r.Cue(FighterCue.Idle));
            ShowStance();   // R6: 끝자세의 겨눔으로 자연스럽게
        }

        // 적의 기본 겨눔 자세(검을 앞으로 세움). 회피·반격 동작의 끝 자세.
        private static readonly Vector3 EnemyGuard = FighterRig.BladeDirection(new Vector2(0.15f, 0.7f), 1f);

        private IEnumerator Mid(StrikeOutcome o)
        {
            Vector3 away = Away();
            float enemyMove = o.success ? ReactionLibrary.MidPush : 0.2f, playerMove = o.success ? 0f : 0.2f;
            Rig(enemyRig, r => r.Cue(o.success ? FighterCue.Backstep : FighterCue.Hit));
            if (!o.success) Rig(playerRig, r => r.Cue(FighterCue.Hit));
            yield return Run(ReactionLibrary.MidReaction, t =>
            {
                // S5 수정: 맞는 대신 짧게 물러나며 검을 다시 겨눈다.
                Rig(enemyRig, r => r.SetBody(Quaternion.Euler(-6f * Mathf.Sin(t * Mathf.PI), 0f, 0f), Vector3.zero));
                if (o.success) Rig(enemyRig, r => r.SetBlade(EnemyGuard));
            }, step =>
            {
                MoveEnemy(away * enemyMove * step);
                MovePlayer(-away * playerMove * step);
            });
            Rig(enemyRig, r => r.ResetBody());
        }

        // 성공 반응 5종(S5 수정안): 맞는 모션 대신 반격·회피 동작으로 끝 거리까지 멀어진다.
        // 맞았다는 표시는 접촉 순간의 파편·히트스톱·체력바로만 전달한다. 반격 헛베기는 피해 없음.
        private IEnumerator Success(StrikeOutcome o)
        {
            SuccessReaction kind = ReactionLibrary.ChooseSuccess(o);
            float seconds = ReactionLibrary.FinalSeconds(o);
            Vector3 away = Away();
            float travel = Mathf.Max(0f, ReactionLibrary.EndDistance(kind) - Distance());
            // 베기 반대쪽으로 피한다: 플레이어가 →로 베면 (플레이어 기준) 왼쪽으로 구른다.
            float rollSide = o.direction == AttackDirection.Right ? -1f : 1f;
            Vector3 sideways = Vector3.Cross(Vector3.up, away).normalized * rollSide;
            // S8-2: 애니메이션 캐릭터용 동작 신호(블록 인형은 무시)
            FighterCue cue = kind == SuccessReaction.PushBack ? FighterCue.Backstep
                : kind == SuccessReaction.SideStagger ? FighterCue.Roll
                : kind == SuccessReaction.KneeBuckle ? FighterCue.Crouch
                : kind == SuccessReaction.Tumble ? FighterCue.Block : FighterCue.HitHead;
            Rig(enemyRig, r => r.Cue(cue));
            bool counterSwing = false;
            if (kind == SuccessReaction.Tumble)
            {
                Vector2 to = SelectionSession.ToVector(o.direction);
                Rig(enemyRig, r => r.SetBlade(FighterRig.BladeDirection(new Vector2(-to.x, to.y), 0.9f)));   // 맞받아침
                feedback.Burst(Vector3.Lerp(player.position, enemy.position, 0.6f) + Vector3.up * 1.3f, HitFeedback.Spark, 10);
            }
            yield return Run(seconds, t =>
            {
                float bump = Mathf.Sin(t * Mathf.PI);
                switch (kind)
                {
                    case SuccessReaction.PushBack:      // 성1 백스텝 회피 → 다시 겨눔
                        Rig(enemyRig, r => r.SetBody(Quaternion.Euler(-8f * bump, 0f, 0f), Vector3.zero));
                        if (t > 0.5f) Rig(enemyRig, r => r.SetBlade(EnemyGuard));
                        break;
                    case SuccessReaction.SideStagger:   // 성2 옆 구르기(애니메이션 캐릭터는 구르기 동작이 대신하므로 블록 인형만 몸 회전)
                        if (enemyRig is FighterRig) Rig(enemyRig, r => r.SetBody(Quaternion.Euler(0f, 0f, 360f * t * -rollSide), Vector3.down * 0.5f * bump));
                        break;
                    case SuccessReaction.KneeBuckle:    // 성3 몸을 낮춰 뒤로 미끄러지며 빠져나감
                        Rig(enemyRig, r => r.SetBody(Quaternion.Euler(15f * bump, 0f, 0f), Vector3.down * 0.45f * bump));
                        break;
                    case SuccessReaction.Tumble:        // 성4 맞받아치다 크게 밀려남
                        Rig(enemyRig, r => r.SetBody(Quaternion.Euler(-10f * bump, 0f, 0f), Vector3.zero));
                        break;
                    default:                            // 성5 젖혀 피하며 반격 헛베기
                        if (t < 0.5f) Rig(enemyRig, r => r.SetBody(Quaternion.Euler(-30f * t * 2f, 0f, 0f), Vector3.zero));
                        else
                        {
                            float k = (t - 0.5f) * 2f;
                            if (!counterSwing) { counterSwing = true; Rig(enemyRig, r => r.Cue(FighterCue.Swing)); }   // 반격 헛베기
                            Rig(enemyRig, r => r.SetBody(Quaternion.Euler(-30f * (1f - k), 0f, 0f), Vector3.zero));
                            Rig(enemyRig, r => r.SetBlade(FighterRig.SwingDirection(new Vector2(-0.8f, 0.8f), new Vector2(0.8f, -0.6f), k)));
                        }
                        break;
                }
            }, step =>
            {
                MoveEnemy(away * travel * step);
                if (kind == SuccessReaction.SideStagger) MoveEnemy(sideways * 1.5f * step);
            });
            Rig(enemyRig, r => r.ResetPose());
        }

        // 실패 반응 5종: 충돌·힘겨루기 후 3m로 분리.
        private IEnumerator Fail(StrikeOutcome o, Vector2 to)
        {
            FailReaction kind = ReactionLibrary.ChooseFail(o);
            // S8-2 동작 신호
            switch (kind)
            {
                case FailReaction.Struggle: Rig(playerRig, r => r.Cue(FighterCue.Struggle)); Rig(enemyRig, r => r.Cue(FighterCue.Struggle)); break;
                case FailReaction.Deflect: Rig(enemyRig, r => r.Cue(FighterCue.Swing)); Rig(playerRig, r => r.Cue(FighterCue.Hit)); break;
                case FailReaction.BodyBlock: Rig(enemyRig, r => r.Cue(FighterCue.Block)); Rig(playerRig, r => r.Cue(FighterCue.Hit)); break;
                case FailReaction.Sidestep: Rig(enemyRig, r => r.Cue(FighterCue.Roll)); break;
                default: Rig(enemyRig, r => r.Cue(FighterCue.Hit)); Rig(playerRig, r => r.Cue(FighterCue.Hit)); break;
            }
            if (kind == FailReaction.Struggle)
            {
                // 칼날을 맞대고 힘겨루기: 서로 기울이며 떨린다.
                yield return Run(ReactionLibrary.StruggleExtra, t =>
                {
                    Rig(playerRig, r => r.SetBody(Quaternion.Euler(10f, 0f, 0f), Vector3.zero));
                    Rig(enemyRig, r => r.SetBody(Quaternion.Euler(10f, 0f, 0f), Vector3.zero));
                    Rig(playerRig, r => r.SetBlade(FighterRig.BladeDirection(new Vector2(0f, 0.6f), 1f)));
                    view.Shake(0.03f, 0.05f);
                });
            }
            if (kind == FailReaction.BodyBlock)
            {
                Vector3 toPlayer = Vector3.ProjectOnPlane(player.position - enemy.position, Vector3.up);
                if (toPlayer.sqrMagnitude > 0.01f) enemy.rotation = Quaternion.LookRotation(toPlayer);   // 돌아서며 막음
            }
            Vector3 away = Away();
            Vector3 sideways = Vector3.Cross(Vector3.up, away).normalized * (to.x >= 0f ? 1f : -1f);
            float gap = Mathf.Max(0f, ReactionLibrary.FailEndDistance - Distance());
            float playerShare = kind == FailReaction.Struggle ? 0.8f : 0.6f;
            yield return Run(ReactionLibrary.FinalReaction, t =>
            {
                float bump = Mathf.Sin(t * Mathf.PI);
                switch (kind)
                {
                    case FailReaction.Deflect:
                        Rig(enemyRig, r => r.SetBlade(FighterRig.SwingDirection(new Vector2(-to.x, to.y), new Vector2(to.x, -0.6f), t)));
                        Rig(playerRig, r => r.SetBody(Quaternion.Euler(0f, 30f * bump, 0f), Vector3.zero));
                        break;
                    case FailReaction.BodyBlock:
                        Rig(enemyRig, r => r.SetBody(Quaternion.Euler(8f * bump, 0f, 0f), Vector3.zero));
                        Rig(playerRig, r => r.SetBody(Quaternion.Euler(-12f * bump, 0f, 0f), Vector3.zero));
                        break;
                    case FailReaction.Sidestep:
                        Rig(enemyRig, r => r.SetBody(Quaternion.Euler(0f, 0f, -10f * bump), Vector3.zero));
                        break;
                    default:   // 충돌 · 힘겨루기 후 밀림
                        Rig(playerRig, r => r.SetBody(Quaternion.Euler(-10f * bump, 0f, 0f), Vector3.zero));
                        Rig(enemyRig, r => r.SetBody(Quaternion.Euler(-8f * bump, 0f, 0f), Vector3.zero));
                        break;
                }
            }, step =>
            {
                MovePlayer(-away * gap * playerShare * step);
                MoveEnemy(away * gap * (1f - playerShare) * step);
                if (kind == FailReaction.Sidestep) MoveEnemy(sideways * 1f * step);
            });
            Rig(playerRig, r => r.ResetBody());
            Rig(enemyRig, r => r.ResetPose());
        }

        // 힘 3 치명타 추가 모션 3종(+0.2초). 피해는 이미 적용됨.
        private IEnumerator Crit(CritMotion motion, Vector2 from, Vector2 to)
        {
            Vector3 forward = Away();
            Rig(playerRig, r => r.Cue(FighterCue.Swing));
            yield return Run(ReactionLibrary.CritExtra, t =>
            {
                switch (motion)
                {
                    case CritMotion.DoubleCut:
                        Rig(playerRig, r => r.SetBlade(FighterRig.SwingDirection(from, to, t)));
                        break;
                    case CritMotion.SpinThrust:
                        if (playerRig is FighterRig) Rig(playerRig, r => r.SetBody(Quaternion.Euler(0f, 180f * t, 0f), Vector3.zero));
                        Rig(playerRig, r => r.SetBlade(Vector3.forward, 0.35f + 0.5f * Mathf.Sin(t * Mathf.PI)));
                        break;
                    default:
                        Rig(playerRig, r => r.SetBody(Quaternion.Euler(15f * Mathf.Sin(t * Mathf.PI), -25f * (1f - t), 0f), Vector3.zero));
                        Rig(playerRig, r => r.SetBlade(FighterRig.SwingDirection(new Vector2(0f, 1f), new Vector2(0f, -1f), t)));
                        break;
                }
            }, step => { if (motion == CritMotion.ShoulderCut) MovePlayer(forward * 0.3f * step); });
            Rig(playerRig, r => r.ResetBody());
            Rig(playerRig, r => r.SetBlade(FighterRig.BladeDirection(to, 0.2f)));
            view.Shake(0.12f, 0.12f);
        }

        // ---------------- 비도 · 지나가며 베기 · 허공 베기 · 가드 ----------------

        public IEnumerator PlayKnife(StrikeOutcome o, Action<StrikeOutcome> apply)
        {
            var knife = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityEngine.Object.Destroy(knife.GetComponent<Collider>());
            knife.transform.localScale = new Vector3(0.05f, 0.05f, 0.35f);
            Rig(playerRig, r => r.Cue(FighterCue.Throw));
            Sound.Play(Sfx.KnifeThrow, player.position + Vector3.up * 1.3f);
            Vector3 start = player.position + Vector3.up * 1.3f, end = enemy.position + Vector3.up * 1.3f;
            if (!o.success) end += Vector3.Cross(Vector3.up, end - start).normalized * 1.2f;   // 빗나가면 옆으로
            yield return Run(0.2f, t =>
            {
                knife.transform.position = Vector3.Lerp(start, end, t);
                knife.transform.rotation = Quaternion.LookRotation(end - start);
            });
            UnityEngine.Object.Destroy(knife);
            apply(o);
            if (o.success)
            {
                feedback.Burst(enemy.position + Vector3.up * 1.3f, HitFeedback.Fragment, 8);
                Sound.Play(Sfx.Hit, enemy.position + Vector3.up * 1.3f);
                Rig(enemyRig, r => r.Wound(player.right, 0));   // R6: 비도는 스침
                yield return Wait(ReactionLibrary.StopHit);
                // S5 수정: 비도를 맞고 옆으로 한 발 비켜서며 다시 겨눔
                Vector3 side = Vector3.Cross(Vector3.up, Away()).normalized;
                Rig(enemyRig, r => r.Cue(FighterCue.Backstep));
                yield return Run(ReactionLibrary.MidReaction, t => Rig(enemyRig, r => r.SetBlade(EnemyGuard)), step => MoveEnemy(side * 0.5f * step));
                Rig(enemyRig, r => r.ResetPose());
            }
            ShowStance();
        }

        /// <summary>특수기 2: 적을 관통해 뒤로 지나가며 벤다. 적은 제자리에서 반응(바로 이어 공격할 수 있게).</summary>
        public IEnumerator PlayDashSlash(StrikeOutcome o, Action<StrikeOutcome> apply, float seconds, float overshoot)
        {
            Vector3 away = Away();
            Vector3 start = player.position;
            Vector3 end = new Vector3(enemy.position.x, start.y, enemy.position.z) + away * overshoot;
            Rig(playerRig, r => r.SetTrail(true));
            Rig(playerRig, r => r.Cue(FighterCue.Swing));
            Sound.Play(Sfx.Dash, player.position);
            Sound.Play(Sfx.SpinSwing, player.position + Vector3.up * 1.3f, 1.1f);
            bool applied = false;
            yield return Run(seconds, t =>
            {
                Rig(playerRig, r => r.SetBlade(FighterRig.SwingDirection(new Vector2(-1f, 0f), new Vector2(1f, -0.2f), t)));
                body.Move(Vector3.Lerp(start, end, t) - player.position);
                if (!applied && t >= 0.5f) { applied = true; apply(o); Sound.Play(Sfx.HeavyHit, enemy.position + Vector3.up * 1.2f); feedback.Burst(enemy.position + Vector3.up * 1.2f, HitFeedback.Fragment, 24); view.Shake(0.15f, 0.2f); Rig(enemyRig, r => r.Wound(away, 2)); }
            });
            if (!applied) apply(o);
            body.Move(end - player.position);
            player.rotation = Quaternion.LookRotation(-away);
            Rig(playerRig, r => r.SetTrail(false));
            Stance.Commit(AttackDirection.Right);
            yield return Wait(ReactionLibrary.StopCrit);
            // S5 수정: 적은 몸을 돌리지 않고(후면 유지) 어깨 너머로 검을 뒤로 돌려 막는 자세만 취한다.
            Rig(enemyRig, r => r.Cue(FighterCue.Block));
            yield return Run(ReactionLibrary.TumbleReaction, t =>
            {
                Rig(enemyRig, r => r.SetBody(Quaternion.Euler(0f, 40f * Mathf.Sin(t * Mathf.PI), 0f), Vector3.zero));
                Rig(enemyRig, r => r.SetBlade(FighterRig.BladeDirection(new Vector2(0.3f, 0.6f), -0.8f)));
            });
            Rig(enemyRig, r => r.ResetPose());
            Rig(enemyRig, r => r.Cue(FighterCue.Idle));
            ShowStance();
        }

        // ---------------- R8 합 공격 방어 · 피격 ----------------

        /// <summary>
        /// 합 공격 3타 재생. 타마다 적이 베고(KayKit 동작), 막았으면 플레이어가 그 방향으로 검을 대며 튕겨냄(불꽃·충돌음),
        /// 못 막았으면 맞음(움찔·베인 자국). apply(i)가 피해·기록을 처리한다.
        /// </summary>
        public IEnumerator PlayDefense(AttackDirection[] pattern, IReadOnlyList<bool> results, Action<int> apply, Func<bool> ended)
        {
            FaceEachOther();
            Vector3 toPlayer = Vector3.ProjectOnPlane(player.position - enemy.position, Vector3.up);
            if (toPlayer.sqrMagnitude > 0.001f) enemy.rotation = Quaternion.LookRotation(toPlayer);
            for (int i = 0; i < pattern.Length; i++)
            {
                Vector2 dir = SelectionSession.ToVector(pattern[i]);
                Rig(enemyRig, r => r.Cue(i % 2 == 0 ? FighterCue.Slice : FighterCue.Chop));
                Sound.Play(Sfx.Swing, enemy.position + Vector3.up * 1.5f, 0.75f + i * 0.08f);
                yield return Wait(0.16f);   // 칼이 닿기까지
                apply(i);
                Vector3 contact = Vector3.Lerp(player.position, enemy.position, 0.4f) + Vector3.up * 1.3f;
                if (results[i])
                {
                    // 막음: 그 방향으로 검을 대고 튕겨냄
                    Rig(playerRig, r => r.Cue(FighterCue.BlockHit));
                    Rig(playerRig, r => r.SetBlade(FighterRig.BladeDirection(dir, 0.9f), 0.5f));
                    Sound.Play(Sfx.Clash, contact);
                    feedback.Burst(contact, HitFeedback.Spark, 16);
                    view.Shake(0.05f, 0.1f);
                    MovePlayer(-Away() * 0.15f);
                }
                else
                {
                    Rig(playerRig, r => r.Cue(FighterCue.Hit));
                    Rig(playerRig, r => r.Wound(player.right * -dir.x + Vector3.up * dir.y, 1));
                    feedback.Burst(player.position + Vector3.up * 1.2f, HitFeedback.Fragment, 10);
                    view.Shake(0.08f, 0.15f);
                }
                yield return Wait(ReactionLibrary.StopClash + 0.12f);
                if (ended()) break;
            }
            Rig(enemyRig, r => r.Cue(FighterCue.Idle));
            Rig(enemyRig, r => r.ResetPose());
            Rig(playerRig, r => r.Cue(FighterCue.Idle));
            ShowStance();
        }

        /// <summary>실시간 중 적 공격에 맞음: 맞은 쪽 반대로 움찔 + 얇은 자국.</summary>
        public void PlayerHurt(Vector3 from)
        {
            Vector3 away = Vector3.ProjectOnPlane(player.position - from, Vector3.up).normalized;
            Rig(playerRig, r => r.Cue(FighterCue.Hit));
            Rig(playerRig, r => r.Wound(Vector3.Cross(Vector3.up, away), 0));
        }

        public IEnumerator PlayAirSwing()
        {
            Vector2 from = SelectionSession.ToVector(Stance.Current);
            Vector2 to = new Vector2(-from.x, -from.y);
            Rig(playerRig, r => r.SetTrail(true));
            Rig(playerRig, r => r.Cue(FighterCue.Swing));
            Sound.Play(Sfx.Swing, player.position + Vector3.up * 1.3f);
            yield return Run(ReactionLibrary.Swing + ReactionLibrary.Windup, t => Rig(playerRig, r => r.SetBlade(FighterRig.SwingDirection(from, to, t))));
            Rig(playerRig, r => r.SetTrail(false));
            Stance.Commit(SelectionSession.FromVector(to));
            ShowStance();
        }

        public IEnumerator PlayGuard(float seconds)
        {
            Rig(playerRig, r => r.Cue(FighterCue.Block));
            Rig(playerRig, r => r.SetBlade(FighterRig.BladeDirection(new Vector2(-1f, 0.35f), 0.5f)));
            yield return Wait(seconds);
            ShowStance();
        }

        // ---------------- 보조 ----------------

        private Vector3 Away()
        {
            Vector3 d = Vector3.ProjectOnPlane(enemy.position - player.position, Vector3.up);
            return d.sqrMagnitude > 0.0001f ? d.normalized : player.forward;
        }

        private float Distance() => Vector3.ProjectOnPlane(enemy.position - player.position, Vector3.up).magnitude;

        private void FaceEachOther()
        {
            Vector3 d = Away();
            player.rotation = Quaternion.LookRotation(d);
        }

        private void MovePlayer(Vector3 delta) { if (body != null) body.Move(delta); else player.position += delta; }
        private void MoveEnemy(Vector3 delta) => enemy.position += delta;

        private static void Rig(IFighterVisual rig, Action<IFighterVisual> action) { if (rig != null) action(rig); }

        private static IEnumerator Wait(float seconds)
        {
            for (float time = 0f; time < seconds; time += Time.unscaledDeltaTime) yield return null;
        }

        /// <summary>실제 시간으로 seconds 동안 step(t 0→1) 호출. move는 프레임별 진행 비율(합 1)을 받는다.</summary>
        private static IEnumerator Run(float seconds, Action<float> step, Action<float> move = null)
        {
            float done = 0f;
            for (float time = 0f; time < seconds; time += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(time / seconds);
                step(t);
                if (move != null) { float eased = Ease(t); move(eased - done); done = eased; }
                yield return null;
            }
            step(1f);
            if (move != null) move(1f - done);
        }

        private static float Ease(float t) => 1f - (1f - t) * (1f - t);
    }
}
