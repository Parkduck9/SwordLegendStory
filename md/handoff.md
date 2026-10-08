# 검협전 — 인계 문서 (다른 PC·다른 AI로 이어서 작업)

갱신 2026-10-08 · 작성 Claude · HTML: [`../html/handoff.html`](../html/handoff.html) · 작업 규칙: [`../AGENTS.md`](../AGENTS.md)

## 1. 새 PC에서 시작하기

1. GitHub `Parkduck9/SwordLegendStory` 를 받는다(clone / GitHub Desktop).
2. Unity Hub로 **Unity 6000.3.25f1** 설치(같은 버전 권장 — 다른 버전은 업그레이드 경고).
3. Unity Hub → Add → 저장소의 `Game` 폴더. 처음 열 때 `Library/` 재생성으로 몇 분 걸림.
4. `Assets/Game/Scenes/Title.unity` → ▶ Play.
5. AI로 작업하면: Codex는 `AGENTS.md`, Claude Code는 `CLAUDE.md`(→ AGENTS.md)를 자동으로 읽는다.
6. 확인: 에디터를 닫고 `BuildSceneAndVerify` 배치 실행 → `STAT_RULES_OK` (명령은 AGENTS.md 3절).

외부 에셋(동작·모델·소리·글꼴)은 모두 저장소에 들어 있어 따로 받을 필요 없다. 예외: 후보 비교용 `html/model-preview/` 사진은 배포 권리가 불명확해 올리지 않았다(`html/model-preview.html`은 새 PC에서 그림이 비어 보임 — 정상).

## 2. 지금 상태 (한 장 요약)

| 범위 | 내용 | 상태 |
|---|---|---|
| S0~S7 | 이동·카메라·락온·입구 / 스탯 5종 / 8방향 선택 전투 / 대쉬·패링·특수기 / 적 AI / 교환 연출 / 점프·맵 파괴 / 타이틀·스탯 배분·승패·재시작 | 구현 · 규칙 검사 통과 |
| S8-1~S8-4 | 수묵 렌더·환경 / 캐릭터·애니메이션 / 수묵 UI(한글 글꼴) / 사운드 | 구현 · 규칙 검사 통과 |
| R1~R4 | 검토 반영: 메뉴 중 시간 정지 · 투사체 경로 판정 · 현황 문서 · 휠 오른쪽 | 구현 |
| R5 | 검 자세 판정(휘두르는 각도 보정 ±10%, 움직임→자세) | 구현 · Play: 보정 체감 약하나 유지 |
| R6 | 실제 검술 겨눔 자세 3종 · 적 피격 3단계(스침·베임·깊게, 먹빛 자국) | 구현 |
| R7 | 캐릭터 모델 교체(Quaternius Base Characters, CC0) + 무협 의상·수묵 톤 + 옷 보강 | 구현 |
| R8 | Play 의견 2차: 같은 쪽 회전 베기·↑↑↓↓ 불가 / KayKit 동작 / 적 0.7배 / 턴 교대·재진입 1.2초·적 적응 / 합 공격 방어 / 적 기술 5종 추가·확률표 | 구현 · 규칙 검사 통과 · **사용자 Play 미확인** |

마지막 검사: 2026-10-08 `BuildSceneAndVerify` → `STAT_RULES_OK` (저장소 폴더로 옮긴 뒤 다시 확인).

## 3. 핵심 시스템 위치

| 시스템 | 파일 |
|---|---|
| 전투 흐름(상태 전환·슬로모션·재진입·적응·합 공격 방어) | `Scripts/Battle/BattleFlow.cs` |
| 스탯·수치(모든 시험값) | `Scripts/Battle/StatTuning.cs` · `StatEffects.cs` · `CharacterStats.cs` |
| 공격 선택·예약 규칙·판정 | `SelectionSession.cs` · `ComboRules.cs` · `CombatResolver.cs` · `SwordStance.cs` |
| 합 공격 방어 · 적 적응 | `Scripts/Battle/DefenseSession.cs` |
| 적 AI·기술 데이터·확률표 | `Scripts/Enemy/EnemyBrain.cs` · `EnemyMoveset.cs` · `AttackShape.cs` · `EnemyProjectile.cs` |
| 교환·방어 연출 | `Scripts/Presentation/ExchangeDirector.cs` · `ReactionLibrary.cs` |
| 캐릭터 외형(IK·겨눔·움찔·자국) | `Scripts/Presentation/AnimatorRig.cs` · `Kamae.cs` · `SlashMark.cs` |
| 장면·캐릭터·애니메이터 생성 | `Editor/PrototypeBuilder.cs` · `CharacterBuilder.cs` · `BaseCharacterDress.cs` |
| 규칙 검사 | `Editor/StatRulesVerification.cs` |
| 미리보기 | `Editor/R6Preview.cs`(Play 모드) · `ScenePreview.cs` |

## 4. 알려진 문제·한계

- **R1~R8 직접 조작 미확인.** 특히 합 공격 방어 입력 시간, 턴 교대 리듬, 난이도(적응·재진입)는 시험값.
- 적 공격 동작 중 검 방향은 "쥔 손(새끼→검지 마디)" 추정이라 일부 동작에서 어색할 수 있다.
- 플레이어 베기는 여전히 IK 궤적 + 기본 베기 동작. KayKit 한손 검 공격 동작으로 바꿀 여지 있음.
- KayKit 동작은 다른 체형용을 재타깃한 것이라 손·발 위치가 조금 어긋날 수 있다.
- 적 손목 끝동이 일부 자세에서 비스듬한 판처럼 보인다. 무료판 남자 모델은 근육질 체형 하나뿐.
- 예전 Play 모드 자동 점검(`FoundationVerification`)은 검색 인덱스 예외 이력이 있다 — 미리보기는 `R6Preview` 사용.
- Windows 빌드는 아직 만들지 않음(OFL 문서는 `StreamingAssets/Licenses`에 있어 빌드에 자동 포함).
- 미사용 파일: `Art/Quaternius/UBC/Textures/T_Hair_2_*`(정리 후보).

## 5. 다음 할 일 (계획)

| 순서 | 할 일 | 메모 |
|---|---|---|
| **1** | **사용자 Play 확인 (S0~S8-4 + R1~R8)** → 의견을 R9 설계로 | Title에서 한 판. 합 공격·턴 교대·적 기술·새 동작·모델 |
| 2 | **S8-5 템포 조정** | Play 의견으로 `StatTuning`·`EnemyMoveset` 수치 조정(예고·후딜, 선택 시간, 방어 입력, 적응, 재진입, 피해량) |
| 3 | **애니메이션 보강** | 플레이어 공격을 KayKit 한손 검 동작으로, 적 검 방향 보정. 부족하면 Quaternius 유료판(사용자 구매) 검토 |
| 4 | **Windows 빌드** | 빌드 설정에 Title·Foundation, 라이선스 문서 포함 확인, 실행 확인 |
| 후보 | 상성표(`StatTuning.stanceMatrix` 64칸만 채우면 됨) · 적 종류 추가·보스 패턴 · 맵 추가 · 옷 상의 자락·신발 형태 다듬기 | 모두 **설계 → 사용자 확인** 후 |

## 6. 사용자가 정한 것 중 다시 확인할 필요 없는 것

- 가제 "검협전" · 스탯 10포인트(0포인트 하드코어 허용) · 3m 진입 · 실제 시간 선택
- 같은 쪽 이어 베기 = 회전 베기(성공률 보정 유지), ↑↓ = 크게 베기, ↑↑·↓↓ 불가
- 합 공격: 3칸 기억 입력, 속도 스탯으로 시간 약간 증가, 3개 모두 막으면 반격 1회(적응 절반)
- 쉬움 보정: 턴 교대 + 재진입 1.2초 + 적 적응(+5%/타, 최대 25%, 회복)
- 적 기술 확률표(roadmap R8) · 예고·후딜 ×0.7 · 삿갓 그림자 유지
- 에셋: 무료 + 출처 표기 의무 없음만. git 설치는 보류(커밋·푸시는 사용자 요청 시 GitHub Desktop 내장 git 사용 이력)

## 7. 문서 지도

- [`roadmap.md`](roadmap.md) / [`../html/roadmap.html`](../html/roadmap.html) — 단계별 설계도·체크리스트·미리보기(작업 기준)
- [`implementation-status.md`](implementation-status.md) — 맨 위 현재 상태, 아래 날짜별 변경 이력
- [`game-plan.md`](game-plan.md) — 요구사항 원문·사용자 답변·결정 기록 표
- [`technical-design.md`](technical-design.md) — 초기 구조 설계(1.0)
- [`review-fix-plan.md`](review-fix-plan.md) — 검토 R1~R5와 해결법
- `ual-clips.txt` · `ual2-clips.txt` · `kaykit-clips.txt` · `ubc-report.txt` — 동작·모델 목록
