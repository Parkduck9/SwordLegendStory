# 구현 현황 — 검협전

맨 위 "현재 상태"만 최신이다. 그 아래는 날짜별 변경 이력이며 당시 상태를 그대로 보존한다(지우지 않음). 진행 단계는 `roadmap.md`의 S 번호를 기준으로 한다.

## 현재 상태 (2026-10-08 갱신)

### 실행

- 프로젝트 `C:\Unity\1\SwordLegendStory\Game`(2026-10-08 git 저장소 폴더로 이동, 이전 `C:\Unity\1\Game`) · Unity 6000.3.25f1 · URP 17.3 · uGUI+TextMeshPro · Legacy Input
- `Assets/Game/Scenes/Title.unity`(빌드 0)를 열고 Play → 스탯 배분 → 전투장(`Foundation.unity`, 빌드 1) 입구 통과 시 전투 시작
- 장면은 코드로 생성: `Editor/PrototypeBuilder.Build`(직접 고치지 말고 빌더를 수정)
- 조작: WASD·마우스 / 휠 클릭 락온 / 좌클릭(3m 안) 선택 화면 / 우클릭 패링·지나가며 베기 / Shift 대쉬 / Space 점프 / Esc 일시정지 / F1 조작 안내

### 실제 모듈 (`Assets/Game/Scripts/`)

| 묶음 | 파일 |
|---|---|
| 루트 | PlayerMovement · ThirdPersonCamera · EncounterTrigger |
| Battle(규칙) | CharacterStats · StatTuning · StatEffects · AttackDirection · ComboRules · SelectionSession · DefenseSession(+EnemyAdaptation) · CombatResolver · SideBiasTracker · ThrowingKnifeState · GuardParry · Cooldown · BattleClock · SwordStance · BattleFlow · Health · PlayerStats · PlayerDash · PlayerJump |
| Enemy | EnemyBrain · EnemyMoveset · AttackShape · TelegraphView · EnemyProjectile · EnemyPose |
| Presentation | ExchangeDirector · ReactionLibrary · HitFeedback · IFighterVisual · FighterRig · AnimatorRig · Kamae · SlashMark |
| World | DestructibleProp · PropBreaker · GroundScar · ArenaGate |
| Flow | RunSetup · GameSettings · StatAllocation · StatDescriber · RunRecord |
| UI | Ui · InkUISkin · CombatHudView · SelectionWheelView · WheelGraphic · ControlsHintView · ResultView · PauseView · TitleView · HoverHint |
| Audio | SoundBank(Sfx) · AudioService(Sound) |

### 검증 상태

| 단계 | 코드 | 컴파일 | 규칙 검사 | Play 재현 | 사용자 체감 |
|---|---|---|---|---|---|
| S0~S8-4 | 완료 | 완료 | `BuildSceneAndVerify` → STAT_RULES_OK (2026-10-08) | 미실시 | 진행 중 (2026-10-08 Play 시작) |
| review R1·R2·R4 | 완료 | 완료 | STAT_RULES_OK (R1 시계·R2 경로 판정 7항·R4 배치 2해상도) | 미실시 | 대기 |
| R5 검 자세 판정 | 완료 | 완료 | STAT_RULES_OK (각도 단계·보정·표시=판정·확정 성공·상성표 교체·이동→자세 검사 9항) | 미실시 | 보정 체감 약하나 유지 |
| R6 검 이동 자세·적 피격 | 완료 | 완료 | STAT_RULES_OK (애니메이터 구성 4항 · 자세 표·하체·베임 단계 5항) | 자동 Play 모드 미리보기 `R6Preview` → R6_PREVIEW_OK, `html/r6-*.png` | 대기 |
| R7 캐릭터 모델 교체 | 완료 | 완료 | STAT_RULES_OK (키·정면·옷/피부·머리·라이선스) | 미리보기 `html/r7-face-*.png` | 대기 |
| R8 Play 의견 2차 | 완료 | 완료 | STAT_RULES_OK (확률·방어·적응·회전 규칙 등) | 미리보기 `html/r8-*.png` | 대기 |

- Play에서 발견·반영: 캐릭터 뒤 보고 달림(수정), 회전 베기 카메라 회전(고정으로 변경).

### 남은 일

0. 다른 PC·AI 인계: [`handoff.md`](handoff.md) · 작업 규칙 [`../AGENTS.md`](../AGENTS.md) (2026-10-08 GitHub `Parkduck9/SwordLegendStory` 푸시)
1. 사용자 Play 확인(S0~S8-4 + R1~R8) → 의견 반영
2. R5 후속(필요 시): 상성표(`StatTuning.stanceMatrix` 64칸) · 적 스탯 반영
3. S8-5 템포 조정(Play 의견 기반) → Windows 빌드
4. git 설치 여부 결정(현재 zip 백업만)

---

# 날짜별 변경 이력

## 프로젝트와 실행 (2026-10-07 P1 당시 — 과거 기록)

- 프로젝트: `C:\Unity\1\Game`
- Unity: 6000.3.25f1 (설치된 6.3 계열)
- 씬: `Assets/Game/Scenes/Title.unity`에서 시작(S7 이후). 전투장은 `Assets/Game/Scenes/Foundation.unity`
- Unity Hub에서 Game 폴더를 추가하고 위 씬을 연 뒤 Play.
- WASD 이동, 마우스 시점, 휠 클릭 락온 토글. Esc 마우스 해제, 좌클릭 재포획.
- 테스트 입력은 기본 Legacy Input, 기본 Built-in 렌더링. 향후 변경 가능.

## 작성한 코드 (P1 당시 — PrototypeHUD는 S8-3에서 제거됨)

- PlayerMovement: 카메라 기준 이동, 중력, 락온 시 적 방향 회전.
- ThirdPersonCamera: 자유 시점, 락온 토글, 지형 카메라 충돌 검사.
- EncounterTrigger: 입구 통과 시 전투 시작 상태 기록, 중복 시작 차단.
- PrototypeHUD: 조작과 전투 진입 상태 표시.
- PrototypeBuilder: 맵·캐릭터·조명·카메라 및 씬 에셋 생성.
- FoundationVerification: 씬 참조 검사, 렌더 이미지 생성, 5초 Play Mode 오류 검사.

## 당시 범위 (P1 — 과거 기록)

- 직접 제작한 블록형 로우폴리 플레이어/검, 중앙 적 1종(정지 상태).
- 직선 길과 원형 전투장, 입구 기둥·트리거, 경계벽.
- 이동속도 5m/s, 진입로 약 5m, 원형 맵 반경 25m는 배치 확인용 임시값. 최종 스탯·밸런스 승인값이 아니다.
- 스탯 5종은 규칙 계층만 구현함(game-plan.md 5·6차 답변). 아래 '스탯 규칙 계층' 참조. 실제 전투 입력·연출·HUD에는 아직 연결하지 않음.

## 스탯 규칙 계층 — 2026-10-07 (Claude)

`Assets/Game/Scripts/Battle/`의 순수 C# 로직. 난수는 호출자가 0~1 값으로 넘긴다.

- CharacterStats: 5종 0~3, 10포인트 초과 금지. 다 쓰지 않아도 시작 가능(7차 답변).
- StatTuning: 모든 시험 수치를 한곳에 모음. Inspector에서 편집 가능.
- StatEffects: 체력(HP 60/80/100/120, 정면 보정, 실패 피드백 피해, 체력 0 피해 ×0.8), 속도(이동 4/4.5/5/5.5, 대쉬 충전 2.5/1.8/1.2/0.6초, 후면 보정 -10/-5/0/+10%), 기력(스킬 쿨 ×1.8/1.3/1.0/0.5), 힘(피해 ×0.4/0.8/1.0/1.2, 특수기 ×0.6/0.85/1/1.15, 3은 +2 고정 피해 + 15% 치명타 ×1.5), 특수기(1 이상 회피 무적 0.3초, 2 지나가며 베기 ×2.5·10초, 3 마무리 ×4·20초, 누적).
- ComboRules: 속도별 1/1/2/3타, 같은 방향 3번째는 특수기 3 + 쿨타임 준비 시에만 마무리로 허용, 그 외 '불가'. 반복 색상 노랑/파랑/빨강.
- SideBiasTracker: 좌우 묶음 막힘 +10%씩, 같은 쪽 연속 성공 시 누적, 상한 30%, 미사용 시 초기화.
- ThrowingKnifeState: 첫 투척 100%, 성공 시 실패 90% → 8초 동안 하한 30%까지 감소, 다음 선택만 성공률 -15%.
- GuardParry: 체력 3, 0.3초 패링, 쿨타임 8초, 성공 시 50% 환급.
- 시험 기본값(사용자 미확정): 기본 성공률 60%, 기본 피해 10, 성공률 범위 5~95%, 같은 방향 둘째 타 실패 +15%·피해 +3, 실패 피드백 피해 자신 3 / 적 2, 비도 피해 8.

검증: `Editor/StatRulesVerification.Run`을 배치 모드로 실행 → `unity-stat-rules.log`에 `STAT_RULES_OK`, 컴파일 오류 없음. 규칙 단위 검사만 했고, 게임 안에서 직접 해 보는 검증은 하지 않음.
- 공격·적 AI·점프·회피·피해·선택 UI·애니메이션·특수기는 아직 미구현.

## 전투 선택 연결 — 2026-10-07 (Claude)

- BattleFlow: 실시간 → 3m 안 좌클릭 → 포커스(0.25초) → 선택(속도별 1/1.3/1.5/2초, 실제 시간) → 교환 → 분리 → 실시간. 3m 밖은 허공 베기. 선택 중 플레이어 무적, 이동 잠금, 시간 배율 0.1.
- SelectionSession: 마우스 이동량으로 가상 커서를 움직여 8방향을 고르고 좌클릭으로 예약. 커서는 위쪽에서 시작. 가운데 원은 비도(기력 3, 예약 전에만 가능, 던지면 선택 종료).
- 시간 초과: 속도 0·1은 미예약 시 적의 반격 피해 8(시험값), 2·3은 예약분만 실행하고 0개면 취소.
- CombatResolver: 예약 계획을 내부 추첨으로 판정. HUD 성공률과 같은 계산 경로. 좌우 막힘 기록, 마무리 쿨타임 시작.
- CombatHUD(임시 IMGUI): 체력바, 스탯, 거리, 마무리 쿨타임, 비도 성공률, 방향 글자 + 성공률 + 반복 색(노랑/파랑/빨강) + 불가 표시, 타이머, 예약 슬롯, 정면/후면, 결과 기록, 승리/패배와 R 재시작.
- 임시 연출: 검 회전 베기, 비도 투사체, 마무리 시 카메라 360° 회전 + 흔들림, 성공 시 적 밀림 / 실패 시 양쪽 분리. 성공/실패 각 5개 변형, 치명타 추가 모션 3개는 미구현.
- 시험값: 적 체력 100, 슬로모션 0.1배, 포커스 0.25초, 시간 초과 반격 8.
- 미구현: 적 AI·공격(적은 아직 정지), 체력 3 가드 패링 입력, 특수기 2 우클릭 지나가며 베기, Shift 대쉬/무적, 점프, 스탯 배분 화면.
- 검증: `StatRulesVerification.BuildSceneAndVerify` 배치 실행 → `unity-combat-build.log`에 FOUNDATION_BUILD_OK, STAT_RULES_OK. Play 모드 직접 조작과 IMGUI 한글 글꼴 표시는 미확인.

## 이동 기술 (roadmap S3) — 2026-10-07 (Claude)

설계 확인 절차 전에 구현됨. [roadmap.md](roadmap.md) S3 사후 확인 항목 답변 대기.

- PlayerDash: Shift, 실시간만. 4m/0.18초(시험값), 충전 시간 속도 스탯, 무적은 특수기 1 이상 0.3초(Health.GrantInvulnerability, 실제 시간).
- GuardParry 연결: 실시간 우클릭(체력 3). 0.3초 동안 Health.ParryCheck가 피해를 막고 쿨타임 50% 환급. 가드 자세 임시 표시.
- 특수기 2: 선택 중 우클릭 → 예약 타격 실행 후 적을 관통해 뒤로 2.5m 지나가며 베기. 성공률은 일반 타격과 같은 보정, 피해 ×2.5, 쿨타임 10초×기력 배율.
- EnemyTestAttack(디버그): T키, 0.6초 예고 후 4m 안 10 피해. S4 적 AI 구현 시 제거.
- HUD: 대쉬·베기·가드 쿨타임, 무적 표시, 선택 중 베기 성공률.
- 검증: unity-combat-build.log FOUNDATION_BUILD_OK, STAT_RULES_OK. Play 미확인.

### S3 수정 — 9차 답변 + 설계 확인 (2026-10-07)

- 지나가며 베기: 예약 없이 바로(선택 화면에서 예약 전에만 우클릭), 무조건 성공, 피해 ×4.5.
- 특수기 3 마무리: 무조건 성공(HUD 100%).
- 패링 성공 → 적 무방비 1.5초(실제 시간, 실시간 상태에서만 감소) + EnemyPose 기지개 자세. 무방비 중 4m 안 좌클릭 → 반격 선택(1타, 비도·베기·마무리 불가, 무조건 성공). 반격 후 또는 1.5초 경과 시 해제. 반격 선택을 시간 초과로 놓치면 페널티 없이 "반격 기회 놓침".
- 무방비 중에는 T 테스트 공격 불가.

## 사운드 (roadmap S8-4) — 2026-10-08 (Claude)

- 음원: Kenney Impact·RPG·Interface(CC0, 79개·645KB, `Art/Audio/Kenney/`, 팩별 License.txt) + `Editor/SoundSynth.cs` 합성 WAV(`Art/Audio/Generated/`): 베기 3종·회전 베기·대쉬, 적 예고음 5종(금속 긁힘 상승·북 3연타·높은 딱 2회·징·딱딱이), 큰 북, 승리·패배 오음계 가락, 배경음(타이틀 가야금풍+바람 44초, 전투 장단+선율 37초, 끝을 앞에 겹쳐 끊김 없는 반복).
- `Scripts/Audio/`: SoundBank(이름 → 클립·음량·음높이·분류, `Audio/SoundBank.asset`, `Editor/SoundBankBuilder.cs`가 채움), AudioService(16음성, 위치 소리는 입체음 60%, 배경음 반복·결과 시 줄임, 선택 화면 동안 저역 필터 900Hz), `Sound.Play` 호출.
- 연결: 교환 연출(베기·회전·타격·치명타 북·충돌·비도·지나가며 베기), BattleFlow(패링·선택 시 먹먹·시간 초과 피격·승리/패배 가락), 적(패턴별 예고음·휘두르기·돌진·도약 착지·적중 시 피격음), 투사체, 대쉬, 점프·착지, 발소리(AnimatorRig, 속도·몸집 반영), 소품 파괴, 입구 문, UI 버튼·휠 칸 이동·예약·불가.
- 설정: 음량 3개(전체·배경음·효과음) 일시정지 설정에 추가, 저장.
- 버그 수정: GameSettings에서 값을 처음 읽기 전에 바꾸면 이후 저장값이 덮어쓰던 문제(감도 설정에도 해당) — 바꾸기 전에 먼저 불러오도록 수정, 검사 추가.
- 검증: STAT_RULES_OK(소리 30종 클립 연결, 예고음 5종 서로 다름, 합성 길이, 배경음 스트리밍, 장면별 AudioService, Kenney 라이선스 문서, 음량 범위). 소리는 `html/audio/`에 복사해 roadmap.html에서 재생 가능. 실제 게임 속 음량 균형·예고음 구분은 Play 확인 필요.

## 백업 — 2026-10-08

- git이 설치돼 있지 않아 버전 관리 대신 압축 백업을 만들었다: `Backups/검협전_2026-10-08_1040_S8-3.zip`(Assets·Packages·ProjectSettings·md·html, Library 제외, 17.5MB). git 설치는 사용자 승인 대기.

## UI 교체 (roadmap S8-3) — 2026-10-08 (Claude)

- 패키지: `com.unity.ugui` 2.0.0(Unity 내장, TextMeshPro 포함) 추가, TMP 기본 자원(`Assets/TextMesh Pro/`) 가져옴.
- 글꼴(SIL OFL 1.1, Google Fonts 공식 저장소): Noto Sans KR 가변(본문, SDF 면 확장 0.22로 굵기 보정), 나눔손글씨 붓(제목). 둘 다 동적 TMP 글꼴 에셋(`Assets/Game/UI/`). OFL 문서는 `StreamingAssets/Licenses/`(빌드 자동 포함). 화면 크레딧 없음.
- `Editor/UIBuilder.cs`: 스킨(InkUISkin: 글꼴 2·코드로 그린 스프라이트 4 — 한지 판넬, 붓 획, 먹 원, 낙관) 생성 + 장면별 캔버스(1920×1080 기준) 배치. 이벤트 시스템 포함.
- 화면(`Scripts/UI/`): CombatHudView(붓 체력바·적 상태·거리·쿨타임 먹 원 5·무방비 알림·전투 기록·먹 번짐 배경), SelectionWheelView + WheelGraphic(원형 8칸·바깥 시간 고리·반복 색 띠·회전 베기·가운데 비도·낙관 슬롯·안내), ControlsHintView(첫 진입만 + F1), ResultView, PauseView(설정 슬라이더·전체화면), TitleView(타이틀·조작법·스탯 배분·확인 창). 값·버튼 동작은 기존 IMGUI와 같은 경로.
- 기존 IMGUI 스크립트(CombatHUD, PrototypeHUD, TitleMenu, ResultScreen, PauseMenu) 삭제.
- 수정 이력: 휠 고리가 안 보이던 원인 = 직접 만든 Graphic에 CanvasRenderer가 붙지 않음 → 명시적으로 추가 + 검사 추가. HUD 글자 가독성: 테두리 재질이 작은 글자를 뭉개 제거, 굵은 글씨 + 먹 번짐 배경으로 해결.
- 검증: STAT_RULES_OK(스킨, 글꼴 한글·화살표·●○ 생성, 동적 글꼴, HUD·휠(평소 숨김)·고리 그리기 부품·안내·결과·일시정지·이벤트 시스템, IMGUI 제거, OFL 문서, 타이틀 장면 TitleView). 미리보기 `html/s8-3-title.png`, `s8-3-alloc.png`, `s8-3-battle.png`.
- Play 확인 필요: 버튼·슬라이더 조작, 마우스 잠금/해제 전환, 휠 반응, 다른 해상도.

## 캐릭터·애니메이션 (roadmap S8-2) — 2026-10-08 (Claude)

- 에셋: Quaternius Universal Animation Library [Standard](CC0, 출처 표기 불필요 — 공식 FAQ·동봉 License.txt 확인). `Art/Quaternius/UAL/UAL1_Standard.fbx`(루트 모션 없는 판, 마네킹 + 43개 동작). `Editor/CharacterImport.cs`가 Humanoid로 가져오고 동작 목록을 `md/ual-clips.txt`에 기록.
- `Editor/CharacterBuilder.cs`: 애니메이터 컨트롤러를 코드로 생성(이동 블렌드 + 상태 13개, IK 패스 켬). 마네킹 플레이어 1.8m · 적 2.25m, 먹빛 몸, 코드로 만든 도포 자락·허리띠·삿갓(플레이어), 직검(술)·대도. 모델이 없으면 기존 블록 인형으로 생성.
- `Presentation/IFighterVisual.cs`(+ FighterCue 동작 신호 13종), `AnimatorRig.cs`(속도 → 이동 블렌드, 공중·착지 자동, 동작 신호 → 상태 전환, 오른손 Humanoid IK로 8방향 검 궤적, 검을 손 위치에 두고 날을 궤적 방향으로). FighterRig도 같은 계약 구현(신호 무시).
- ExchangeDirector: 블록 인형 전용 몸 기울임 대신 동작 신호 추가, 같은 방향 회전 베기는 캐릭터 루트를 360° 돌림. EnemyPose는 AnimatorRig가 있으면 신호로 위임. 승패 시 쓰러짐·승리 동작.
- 검증: STAT_RULES_OK(상태 14종·동작 연결·IK 패스, 두 캐릭터 Humanoid 아바타·루트 모션 끔·AnimatorRig·키·도포·허리띠·삿갓·직검/대도, 연출 리그 각 1개). 미리보기 `html/s8-2-preview-idle.png`, `-swing.png`(편집 모드라 IK 미반영).
- 알려진 한계: 막기·백스텝·투척·기지개는 팩에 전용 동작이 없어 대체 동작 사용(roadmap 대응표). 도포 자락은 골반 뼈에 붙은 단단한 원뿔대라 다리 동작에 따라 겹칠 수 있음. Play 확인 필요.

## 렌더·환경 (roadmap S8-1) — 2026-10-07 (Claude)

- URP 17.3.0(Unity 내장 패키지)을 manifest에 추가. `Editor/RenderSetup.cs`가 `Assets/Game/Rendering/`에 파이프라인·렌더러·먹선 재질·후처리 프로필을 만들고 그래픽·품질 설정에 지정(장면 생성 시 자동 실행).
- 먹선: `Rendering/InkOutline.shader`(깊이·법선 차이, 거리 60m까지 옅어짐)를 URP FullScreenPassRendererFeature로 후처리 전에 그림.
- 후처리: 채도 -28·대비 +8·따뜻한 색 필터, 블룸 0.35, 비네트 0.28, Neutral 톤매핑. 카메라 후처리 켬.
- 조명: 낮은 따뜻한 해(22°), 부드러운 그림자, 3색 주변광, 한지색 선형 안개 28~165m, 배경색 = 안개색.
- `Editor/GeneratedArt.cs`: 석판 바닥·돌담·나무 질감과 먼 산 고리 3겹 메시를 코드로 생성(`Art/Generated/`). 외부 다운로드 없음.
- 입구: 나무 기둥·들보·지붕판, 진입 후 목조 문 두 짝이 0.6초 동안 닫힘(ArenaGate가 투명 벽 + 문 회전 담당).
- 재질 전부 URP Lit/Unlit. 런타임 생성 효과(예고·잔상·파편·자국)는 Sprites/Default 유지(URP에서 정상 렌더 확인).
- 검증: 규칙 검사 STAT_RULES_OK(URP 지정, 먹선 셰이더 지원, 후처리 4종, 안개·배경, 먼 산, 모든 렌더러 URP 셰이더, 소품 충돌체 크기 유지, 카메라 후처리). 미리보기 `html/s8-1-preview.png`, `html/s8-1-preview-close.png`(그래픽 장치로 배치 렌더).
- 소품 모델(2026-10-08): Kenney Nature Kit 2.1에서 16개 FBX만 `Art/Kenney/NatureKit/`에 복사(License.txt 동봉, `Art/LICENSES.md` 기록). 라이선스 CC0 — 공식 지원 페이지와 동봉 License.txt 모두 "상업 무료, 출처 표기 의무 없음" 확인. 소품은 판정용 BoxCollider(크기 고정) + 자식 모델 구조로 바꿈, 모델 높이·너비·바닥을 상자에 맞춤(검사). 파괴 조각 크기는 충돌체 기준.
- 남은 일: Play 모드 프레임·문 닫힘·소품 엄폐 체감 확인.

## 게임 흐름 (roadmap S7) — 2026-10-07 (Claude)

설계 1.0 사용자 확인 후 구현. `Scripts/Flow/`, 가제 "검협전".

- 장면: `Assets/Game/Scenes/Title.unity`(새로, 빌드 0번) → `Foundation.unity`(빌드 1번). **이제 Title 장면에서 Play.** Foundation 단독 실행은 PlayerStats Inspector 값 사용.
- TitleMenu: 타이틀(시작·조작법·종료) · 스탯 배분(−/+, ●○, 남은 포인트, 0포인트 하드코어, 설명 자동 생성, + 버튼 마우스 오버 시 다음 레벨, 프리셋 균형/공격형/회피형, 초기화, 남은 포인트 확인 창).
- StatAllocation / StatDescriber / RunSetup(장면 간 전달 + PlayerPrefs에 마지막 스탯) / GameSettings(카메라·커서 감도, 전체화면).
- ArenaGate: 진입 후 입구 뒤 진입로(z=-26.5)에 붉은 벽 활성화. 진입 트리거와 겹치지 않는 위치.
- BattleFlow: RunRecord(진입부터 실제 시간·피해·성공률·최대 연속·패링), 끝 연출 0.5초 ×0.25 슬로모션 후 결과, 일시정지(실시간에서만, 시간 0·마우스 해제), Restart / GoToTitle.
- ResultScreen: 승리/패배·기록·사용 스탯, 다시 하기(R) · 스탯 다시 배분 · 타이틀로. PauseMenu: 계속·다시 시작·타이틀로·설정·종료.
- 카메라의 Esc 마우스 해제·좌클릭 재포획은 일시정지 메뉴로 대체. CombatHUD의 승리/패배 글자 제거.
- 설계와 다른 점: 교환 연출(≤1.5초)은 실제 시간으로 진행돼 일시정지와 맞지 않아, Esc를 선택 화면뿐 아니라 교환 중에도 무시.

## 점프·맵 파괴 (roadmap S6) + S5 성공 반응 수정 — 2026-10-07 (Claude)

설계 1.0·수정안 사용자 확인 후 구현.

- PlayerJump(`Scripts/Battle/`): Space, 실시간만. 도약 속도 4h/T, 중력 8h/T²(1.2m·0.55초 → 8.73m/s, 31.7m/s²). PlayerMovement에 중력·공중 조작 40%·Suspended(선택·교환 중 공중 정지) 추가. 대쉬는 지상 전용.
- 공중 베기: 공중에서 선택 진입 시 ↓↙↘ 성공률 +10%(StatTuning.airStrikeDownBonus), HUD 표시.
- 적: 휘두르기·도약 내려찍기는 착지 상태에만 맞음(groundOnly) → 점프로 회피. 공격 범위·추적/돌진 몸통에 걸린 소품 파괴. 도약 착지 시 GroundScar(30초) + 카메라 흔들림. 투사체는 소품에 막힘.
- `Scripts/World/`: DestructibleProp(조각 4~6개, 3초 후 소멸, 조각 레이어 9는 플레이어와 충돌 없음), PropBreaker, GroundScar. 씬에 돌기둥 8(반경 12m) + 바위 더미 6(반경 18m).
- S5 수정: 성공 반응을 백스텝 회피·옆 구르기·낮게 빠져나가기·맞받아치다 밀려남·젖혀 피하며 반격 헛베기(피해 없음)로 교체. 중간 타·비도·지나가며 베기 반응도 겨눔·비켜서기 자세로 변경(지나가며 베기 후 적은 몸을 돌리지 않아 후면 유지).

## 교환 연출 (roadmap S5) — 2026-10-07 (Claude)

설계 1.0 사용자 확인 후 구현. `Scripts/Presentation/`. BattleFlow의 임시 Swing/Separate/DashThrough/KnifeThrow/GuardPose 코루틴을 대체.

- ExchangeDirector: 준비 0.08 → 궤적 0.14(같은 방향은 몸 360° 회전 0.22) → 접촉(Apply 1회 · 히트스톱 0.06/0.1/0.05) → 반응. 마지막 타 반응이 분리(성공 3~4.5m, 실패 3m). 비도·지나가며 베기·허공 베기·가드 자세도 담당.
- SwordStance: 끝자세 이월, 실시간 2초 무공격 시 ↘ 복귀. 선택 화면에 "[검] 회전 베기" 표시.
- ReactionLibrary: 성공 5(밀림·옆 비틀·무릎·크게 튕김·젖힘), 실패 5(흘려냄·비켜 피함·몸으로 막음·힘겨루기 0.4초·맞부딪힘), 치명타 3(이중 베기·회전 찌르기·어깨 밀치기) 선택 규칙과 시간 상수.
- FighterRig: 몸통·오른팔·검을 코드로 움직이는 로우폴리 자세 출력(플레이어·적). 검 잔상 TrailRenderer.
- HitFeedback: 불꽃·파편 파티클(임시). 카메라 Shake 추가.
- StrikeOutcome에 반응용 기록(같은 방향 둘째·후면·좌우 막힘·반격) 추가. 판정 계산은 변경 없음.
- 지나가며 베기: 관통 중간에 피해, 적은 제자리 반응(바로 이어 공격).

## 적 AI (roadmap S4) — 2026-10-07 (Claude)

설계 1.0 사용자 확인 후 구현. `Scripts/Enemy/`.

- EnemyBrain: 대기(입구 전) → 추적(6m/s, 360°/초) → 예고(마지막 0.2초 방향·위치 고정) → 공격 → 후딜(회전 없음) → 추적. BattleFlow.EnemyMayAct가 false(선택·교환·무방비·사망)면 정지.
- EnemyMoveset: 휘두르기 / 돌진 베기 / 투척 / 도약 내려찍기 / 후퇴 사격 데이터와 선택 규칙(무작위 없음, 후퇴 사격 조건, 3연속 금지).
- AttackShape: 부채꼴·직선·원 판정. TelegraphView가 같은 값으로 바닥 붉은 예고를 그림(고정 구간은 진하게).
- EnemyProjectile: 직진 투사체, 적 정지 중 함께 멈춤, 피격은 공통 경로(패링·무적 적용).
- 연결: 패링 성공 → 공격 취소 + 무방비 후 0.3초 후딜. 교환에서 하나라도 성공 → 공격 취소 + 경직 0.4초, 전부 막힘 → 예고 이어서(최소 0.3초).
- 적 체력 220, EnemyTestAttack(T) 제거. HUD에 적 상태 표시.

## R8 Play 의견 2차 (2026-10-08, roadmap.md R8)

- 에셋: KayKit Character Animations Free(CC0) `Art/KayKit/` Rig_Medium 4개 FBX(`Editor/KayKitImport`, Humanoid, 동작 목록 `md/kaykit-clips.txt`).
- 이어 베기: `ComboRules.IsSpin`(같은 방향·같은 쪽 묶음), `VerticalRepeat`(↑↑·↓↓ 불가), `SwordStance.IsSpin`이 규칙을 따름. 휠에 "회전 베기 + 보정" 표시.
- 애니메이터: 이동 = FreeformDirectional2D(MoveX·MoveZ: 대기·걷기·조깅·달리기·옆으로 달리기 좌우·뒷걸음), 상태 추가 DodgeBack/Left/Right·BlockHit·Spinning·Slice·Chop·Stab·SpinAttack·Kick·JumpChop, Hit·HitHead·Throw를 KayKit으로. AnimatorRig: 다리 비틀기 흉내 제거, 적 공격 동작 신호(`Act`) 동안 IK 끄고 검은 쥔 손 방향(새끼→검지 마디)을 따름.
- 적: `EnemyMoveset` 10종(새 기술 5) · 예고·후딜 ×0.7 · 거리별 확률표(`EnemySkillBand`) · `Choose(…, roll, roll2)`. `EnemyBrain`: 밀쳐 차기 넉백(`BattleFlow.KnockPlayer`), 부채 투척 3발, 찌르기 돌진, 회전 베기, 합 공격 → `BattleFlow.BeginDefense`, 턴 교대(`OnExchangeFinished(…, turnCounter)` → 강제 합 공격). `EnemyPose.SetStrike(kind)`가 기술별 동작.
- 흐름: `BattleState.Defend` + `DefenseSession`(3칸 보여주기 → 입력, 속도 스탯 시간) → `ExchangeDirector.PlayDefense`(적 베기, 막음 = BlockHit·불꽃·튕김 / 맞음 = 피격·자국) → 모두 막으면 `BeginSelection(reward)` 반격. 재진입 `AttackReentry` 1.2초, 적 적응 `EnemyAdaptation`(선택 시작 때 `SelectionAdaptation`으로 고정). 실시간 피격 시 플레이어 움찔·자국(`PlayerHurtVisual`).
- 검사: CheckEnemy(확률 빈도 1000회 · 직전 절반 · 3연속 금지 · 후퇴 40% · ×0.7 · 10종), CheckPresentation(R8 회전·↑↑ 불가), CheckDefenseAndPacing(패턴·스탯 시간·입력·초과·적응·재진입), CheckCharacters(2D 이동·KayKit 상태·라이선스). 미리보기 `html/r8-*.png`.

## R7 캐릭터 모델 교체 (2026-10-08, roadmap.md R7)

- 에셋: Quaternius Universal Base Characters Standard(CC0) `Art/Quaternius/UBC/` — 남자 기본 모델 FBX, 머리 4종(짧은 머리·짧게 깎은 머리·수염·눈썹), 질감. 구성 기록 `md/ubc-report.txt`.
- 가져오기 `Editor/BaseCharacterImport`(Humanoid, 동작 없음 — UAL1·UAL2 동작 재타깃, 질감 읽기 허용). 입히기 `Editor/BaseCharacterDress`: 몸 메시를 옷(0)/피부(1) 두 부분으로 나눈 사본 `Generated/UBC_Male_Split.asset`(기본 자세 정점으로 목 경계 고리·손목 고리에서 자름), 수묵 톤 질감 사본 `Generated/UBC_*_Ink.png`, 머리 모양은 원래 회전·배율 그대로 머리뼈에 붙임, 깃·끝동은 실측 굵기 원뿔대(도포 색).
- CharacterBuilder: `UseBaseCharacter`(UBC 있으면 사용, 없으면 예전 마네킹) · `ModelHeight` 1.81 · 삿갓 높이 모델별. AnimatorRig는 키를 받아 겨눔 자세 배율 계산(`Configure(root, sword, height)`).
- 검사: 키·정면·옷/피부 두 부분·머리 모양 부착·UBC 라이선스. Play 모드 미리보기 `html/r7-face-*.png`.
- 옷 보강(사용자 승인): `BaseCharacterDress.Loosen` — 옷 부분 정점을 10회 고르게 다듬고 1.4cm 부풀림(근육 굴곡 제거, 발은 천 신발 모양). 피부와 맞닿은 정점 고정, 같은 위치 정점은 묶어 이음새 틈 방지.

## R6 검 이동 자세 · 적 피격 반응 (2026-10-08, roadmap.md R6 설계)

- 에셋: Quaternius UAL2 Standard(CC0, 압축 안 License.txt 확인) `Art/Quaternius/UAL2/` — 동작 42개 목록 `md/ual2-clips.txt`. 무료판에 옆·뒷걸음 동작은 없음.
- 애니메이터(CharacterBuilder): 막기 Guard = `Sword_Block`, 새 상태 Knockback = `Hit_Knockback`, 위 층 "Arms"(두 팔·손가락 마스크, Sword_Idle, IK 패스), 이동 상태 재생 방향 매개변수 `LocoDir`(뒷걸음 −1). `Clip()`은 첫 라이브러리 → 없으면 UAL2.
- 겨눔(`Presentation/Kamae`): 판정 8방향 → 실제 검술 자세 표(손 위치·칼 방향). ↓↙↘ 하단·와키가마에 / ↑↖↗ 상단·팔상 / ←→ 중단. `IFighterVisual.HoldStance`(HoldBlade 대체), AnimatorRig가 0.2초 보간 + 팔꿈치 힌트. 교환·가드·허공 베기·비도·지나가며 베기 끝에 겨눔으로 복귀.
- 하체(AnimatorRig): 캐릭터 기준 속도로 `Kamae.LegYaw` — 옆이면 모델(다리)을 최대 60° 이동 쪽으로, 상체는 척추·가슴에서 반대로 비틀어 적을 봄(손 IK 목표를 미리 같은 만큼 돌려 보정). 뒤로는 걷기 거꾸로.
- 몸 반응: AnimatorRig가 무시하던 `SetBody`를 척추에 덧입힘(40° 제한, 90° 넘는 회전은 애니메이션이 대신 — 구르기·회전 찌르기는 블록 인형만). `Wound(cut, tier)` — 칼 지나간 반대쪽으로 젖히며 칼 방향으로 비틀리는 움찔(22/34/44°, 0.06초 최대 후 감쇠) + `SlashMark`(가슴 뼈에 붙는 먹빛 붓 획, 1.5/2.5/3.2초). 단계 `Kamae.WoundTier`: 일반 성공 스침, 같은 방향 둘째 타·피해 15 이상 베임(Hit 동작), 치명타·마무리·반격·지나가며 베기 깊게(Knockback 동작) + 먹 튀김.
- 검증: CheckCharacters에 R6 4항, CheckKamaeAndWounds 5항. Play 모드 미리보기 `Editor/R6Preview`(그래픽 장치 필요). 발견·수정: 한 프레임 이동량으로 방향을 판정해 옆걸음 다리 회전 0° → 속도 기준으로.

## R5 검 자세 판정 (2026-10-08, roadmap.md R5 설계)

- 규칙(`Battle/`): SwordStance를 Presentation에서 Battle로 이동. 실시간 이동(캐릭터 기준 8방향)을 0.25초 유지하면 `StatTuning.moveStance` 표의 자세(기본: 이동 반대쪽), 공중 `airStance`(↑) 즉시, 대쉬는 PlayerDash.Dashed → 반대쪽 즉시, 멈춤 2초 → `restStance`(↘).
- 판정: `StatEffects.SwingSteps`(0~4칸) · `StanceBonus`(바꾸는 지점 하나: `stanceMatrix` 64칸이 차 있으면 상성표, 아니면 `stanceSwingBonus` {0, −0.10, 0, +0.05, +0.10}). StrikeContext.stanceBonus. CombatResolver.PreviewChance/Resolve가 시작 자세(첫 타 = 선택 시작 때 자세 `BattleFlow.SelectionStance`, 다음 타 = 앞 예약 방향 `SwingStart`)를 받음. 마무리·반격은 기존대로 확정 성공.
- 보이기: IFighterVisual.HoldBlade — AnimatorRig는 검을 약 0.2초에 걸쳐 자세로 옮김(블록 인형은 즉시). 선택 휠 칸마다 "크게 +10% / 넓게 +5% / 짧게 -10%"(값은 StanceBonus에서 읽음), 안내줄에 "검 ↗".
- 검사: CheckStance 9항. 미리보기 `html/s8-3-battle.png`(↗ 예약 후 ↙ 크게 +10% 등).

## review R1·R2·R4 적용 (2026-10-08, review-fix-plan.md 9절 1순위 안)

- R1 BattleClock: 메뉴 시간을 뺀 전투 시각·Δ. Health 시간 무적, PlayerDash 충전, 전투 기록, AnimatorRig 동작 신호가 사용. 메뉴 중 EnemyMayAct=false. 씬 시작·종료 시 초기화.
- R2 EnemyProjectile.Sweep: 이전→다음 위치를 투사체 반지름 간격으로 훑어 엄폐 → 몸 캡슐(CharacterController) 순서로 검사. 높이 반영, 빠른 이동 통과 없음.
- R4 선택 휠: 화면 가로 68%·세로 48%, 0.78배, 기준점 가운데. 가운데(적)·왼쪽(플레이어·검) 비움. 검사가 16:9·16:10에서 화면 안·조작 안내/쿨타임 원과 안 겹침을 계산 확인. 미리보기를 실제 포커스 카메라 구도로 갱신(`html/s8-3-battle.png`).
- R3: 이 문서 맨 위 "현재 상태" 신설, 과거 섹션은 이력으로 표시.

## Play 의견 반영 — 회전 베기 카메라 고정 (2026-10-08)

- 의견: 회전 베기(같은 방향 연속) 때 카메라가 몸과 같이 돌아 어지러움.
- 수정: ThirdPersonCamera.HoldFacing — 회전 베기 동안 포커스 구도를 회전 전 방향에 고정, 캐릭터와 검만 한 바퀴. 포커스 해제 시 고정 자동 해제.
- 특수기 3 마무리의 카메라 한 바퀴(PlaySpin)는 별개로 그대로 둠(사용자 확인 대기).

## 버그 수정 — 캐릭터가 뒤를 보고 달림 (2026-10-08, 사용자 Play 발견)

- 증상: A를 누르면 캐릭터가 오른쪽을 보며 뒷걸음으로 왼쪽 이동(모든 방향에서 몸이 진행 방향 반대).
- 원인: CharacterImport가 동작 회전을 원본 기준(keepOriginalOrientation=true)으로 구워 Quaternius 동작이 루트 −Z를 봄(발끝·몸 방향 일치도 −1.00). 플레이어·적 모두 해당.
- 수정: keepOriginalOrientation=false(몸 방향 기준). 대기·걷기·달리기 +Z 정면, 베기는 허리를 트는 자세로 앞쪽 유지.
- 검증 추가: CheckFacing — 플레이어·적의 대기/걷기/달리기 발끝·골반 앞 방향 ≥ 0.5, 베기 ≥ 0. STAT_RULES_OK.

## 버그 수정 — 전투장 진입 불가 (2026-10-07)

- 원인: CircularArena가 기본 원기둥의 CapsuleCollider를 사용. 스케일 (50, 0.5, 50)에서 반지름(25)이 높이보다 커서 반지름 25m 구 충돌체가 되어 입구 앞에 보이지 않는 벽이 생김.
- 수정: PrototypeBuilder에서 CapsuleCollider를 제거하고 원기둥 메시 MeshCollider로 교체.
- 검증 추가: 진입로(z=-29) → 중앙까지 플레이어 크기 캡슐 이동 경로 막힘 없음, 경로 위 바닥 높이 0. 배치 실행 결과 STAT_RULES_OK.

## 검증 결과 (2026-10-07 P1 당시 — 과거 기록)

- unity-build.log: FOUNDATION_BUILD_OK 및 종료 코드 0. C# 컴파일, 씬 생성 성공.
- html/foundation-preview.png: Unity Camera.Render 결과. 원형 맵/진입로/중앙 적 배치를 이미지로 확인.
- unity-verify.log 및 unity-verify-second.log: UnityEditor.Search.SearchDatabase의 ArgumentOutOfRangeException 발생. Play Mode 검증은 실패로 기록. 해당 예외를 숨기거나 테스트 성공으로 취급하지 않음.
- 현재 로그에서 게임 스크립트의 컴파일 오류는 발견되지 않았으나 직접 이동·락온·입구 통과 조작은 검증하지 못함.
- 처음 샌드박스 실행은 로컬 라이선스 IPC 연결 실패. 승인된 일반 실행으로 컴파일·씬 생성을 완료함.

## 다음 작업 (2026-10-07 당시 — 과거 기록, 지금은 맨 위 "남은 일" 참조)

1. Unity 에디터에서 검색 인덱스 초기화 예외와 직접 Play 조작 확인.
2. Play 모드에서 선택 휠·타이머·판정 직접 확인, 한글 표시 확인.
3. roadmap.md S3 사후 확인 → S4 적 AI 설계 확인 후 구현.
4. 적 행동에 필요한 거리·시간·수치의 미정 사항을 확인 후 구현.