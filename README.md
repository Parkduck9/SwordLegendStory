# 검협전 (SwordLegendStory)

3D 실시간·턴제 무협 액션 프로토타입. 실시간으로 움직이다 3m 안에서 공격하면 슬로모션 속 8방향 휠로 연속 베기를 예약하고, 적의 합 공격은 붉게 빛난 순서대로 막는다.

## 실행

- Unity **6000.3.25f1** (URP)
- Unity Hub → Add → `Game` 폴더 → `Assets/Game/Scenes/Title.unity`를 열고 Play
- 처음 열 때 `Library`를 다시 만드느라 시간이 걸린다(저장소에는 올리지 않음)
- 장면은 코드로 생성: 메뉴 대신 `Editor/PrototypeBuilder.Build`, 검사 `Editor/StatRulesVerification.BuildSceneAndVerify`

## 문서

- `md/roadmap.md` · `html/roadmap.html` — 단계별 설계도와 체크리스트(작업 기준)
- `md/implementation-status.md` — 현재 상태(맨 위)와 변경 이력
- `md/game-plan.md` — 요구사항·결정 기록
- `Game/Assets/Game/Art/LICENSES.md` — 외부 에셋 출처·라이선스(모두 CC0 또는 SIL OFL 1.1)

## 외부 에셋

Kenney(CC0), Quaternius UAL·UAL2·Universal Base Characters(CC0), KayKit Character Animations(CC0), Noto Sans KR·나눔손글씨 붓(SIL OFL 1.1, 라이선스 문서 동봉). 자세한 내용은 `LICENSES.md`.
