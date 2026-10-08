# AI 작업 안내 (Codex · Claude 공통)

이 저장소에서 작업하는 AI 에이전트는 아래 규칙을 따른다. 사람 개발자에게도 같은 내용이 인계 기준이다.
처음이라면 [`md/handoff.md`](md/handoff.md)(현재 상태·다음 계획)부터 읽는다.

## 1. 반드시 지킬 규칙 (사용자 결정)

1. **한국어:** 사용자에게 하는 답변, 작업 중 진행 설명, md·html 문서, 새로 쓰는 코드 주석은 모두 한국어. 코드 식별자는 영어.
2. **설계 먼저:** 새 기능·규칙 변경은 바로 코드를 쓰지 않는다.
   설계도 + 체크리스트를 `md/roadmap.md`와 `html/roadmap.html`에 쓰고 → 사용자 확인 → 구현 → 규칙 검사 → 문서 갱신 → 사용자 Play 확인.
   선택지가 있으면 해결법 2~3개를 장단점·한줄평·순위와 함께 제시한다(사용자가 선호하는 방식).
3. **문서 동기화:** 단계가 끝날 때마다 아래 문서를 함께 갱신한다(사용자가 매번 확인함).
   `md/roadmap.md` · `html/roadmap.html` · `md/implementation-status.md`(맨 위 "현재 상태") · `md/game-plan.md`(결정 기록 표) · `md/technical-design.md` 첫 줄 · `html/index.html`·`html/design.html` 상태 띠 · `Game/Assets/Game/Art/LICENSES.md`(에셋을 바꿨을 때) · `md/handoff.md`
4. **외부 에셋:** **무료이고 출처 표기 의무가 없는 것**(CC0, 또는 SIL OFL처럼 화면 크레딧 불필요)만 쓴다. 쓰기 전에 공식 페이지와 압축 안 License 파일로 확인하고 `LICENSES.md`에 기록한다. 라이선스 문구를 못 찾으면 쓰지 않는다.
5. **승인이 필요한 일:** 파일 다운로드(이름·출처·크기를 알리고), 커밋·푸시, 프로그램 설치, 파일 삭제는 사용자에게 먼저 묻는다. git 설치는 사용자가 보류한 적이 있다.
6. **수치는 시험값:** 밸런스 값은 `StatTuning`·`EnemyMoveset` 데이터에 모아 두고, 사용자 Play 의견으로 조정한다. 임의로 상성·수치를 지어내지 않는다.

## 2. 프로젝트

- Unity **6000.3.25f1** · URP 17.3 · uGUI + TextMeshPro · Legacy Input Manager
- Unity 프로젝트: `Game/` · 시작 장면 `Game/Assets/Game/Scenes/Title.unity`(빌드 0) → `Foundation.unity`(빌드 1, 전투장)
- **장면은 코드로 생성한다.** 장면 파일을 손으로 고치지 말고 `Editor/PrototypeBuilder.cs`(및 `CharacterBuilder`·`UIBuilder`·`SoundBankBuilder`)를 고친 뒤 다시 생성한다.
- 네임스페이스 `SwordPrototype.*` — `Battle`(규칙·순수 로직) · `Enemy` · `Presentation`(연출·애니메이션 출력, 규칙 없음) · `World` · `Flow` · `UI` · `Audio` · `Editor`
- 규칙 계층(Battle)은 연출을 모른다. 연출은 판정 결과를 재생만 한다(재계산 금지). 선택 화면 표시 확률 = 실제 판정 확률(같은 함수).

## 3. 빌드·검사 (배치 실행)

Unity 에디터가 이 프로젝트를 열고 있으면 배치 실행이 안 된다(먼저 닫는다). `UNITY`는 설치 경로에 맞게 바꾼다(예: `C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe`).

| 목적 | 명령(`-projectPath Game` 기준) | 성공 표시 |
|---|---|---|
| 장면 생성 + 전체 규칙 검사 | `UNITY -batchmode -nographics -projectPath Game -executeMethod SwordPrototype.Editor.StatRulesVerification.BuildSceneAndVerify -logFile verify.log` | 로그에 `STAT_RULES_OK` (실패는 `[스탯 규칙 실패] …`) |
| 동작 모음 다시 가져오기 | `…CharacterImport.ConfigureAndReport` / `…KayKitImport.ConfigureAndReport` / `…BaseCharacterImport.ConfigureAndReport` | `UAL_REPORT_OK` / `KAYKIT_REPORT_OK` / `UBC_REPORT_OK` |
| Play 모드 미리보기(실제 동작·IK) | `UNITY -batchmode -projectPath Game -executeMethod SwordPrototype.Editor.R6Preview.Run -logFile r6.log` (**-nographics 없이**) | `R6_PREVIEW_OK`, `html/r6-*.png`·`r7-*`·`r8-*` 갱신 |
| UI 미리보기 | `…ScenePreview.CaptureS83` (-nographics 없이) | `S83_PREVIEW_OK`, `html/s8-3-*.png` |

- 새 규칙·기능을 넣으면 `StatRulesVerification`에 검사를 추가한다(통과 항목은 로그에 안 남고 실패만 남는다).
- `Library/`는 저장소에 없다. 새 PC에서 처음 열면 몇 분 동안 다시 만든다.

## 4. 작업 습관

- 사용자 표현: "진행해" = 설계 확인됨, 구현 시작. "추천대로" = 각 항목 1순위로.
- 설계 확인 질문은 한 번에 모아서, 정할 것만 짧게.
- 큰 작업 중에는 짧은 진행 상황을 한국어로 알린다.
- 미리보기 사진은 `html/`에 저장하고 roadmap.html에 붙인다.
- 커밋 메시지는 한국어, BOM 없는 UTF-8.
