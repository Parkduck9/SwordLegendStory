# 상속·다형성으로 만드는 가장 작은 전투 시스템

2026-10-08 · C# 콘솔 학습 예제 · .NET 9

예제 위치: `../Examples/SimpleBattle/` · 그림 설명: `../html/simple-battle-oop.html`

## 목적과 범위

사용자 요청: 객체지향 기본 구조, 상속, 가상함수, 파생 객체, 시스템 분리, Main에서 필요한 객체 연결을 아주 간단한 전투로 보여준다.

기존 Unity 게임 밖에 별도 콘솔 예제를 작성했다. Unity 씬에 연결한 기능이 아니다. 실시간 입력·애니메이션·확률·스탯 배분은 이 학습 예제의 범위에 포함되지 않는다.

## 파일별 역할

| 파일 | 역할 |
|---|---|
| Character.cs | 공통 캐릭터. 이름·HP 보관, 피격 처리, 가상 공격 피해 함수 |
| Characters.cs | 검사·마법사·슬라임. 공통 캐릭터를 상속하고 공격 피해 재정의 |
| BattleSystem.cs | 턴 순서·공격 실행·승패 처리. 실제 캐릭터 종류를 검사하지 않음 |
| Program.cs | Main. 사용할 실제 객체를 생성하고 전투 시스템에 전달 |
| SimpleBattle.csproj | .NET 9 실행 설정 |

## 핵심 개념

1. **객체:** `new Warrior("검사")`로 만든 실제 캐릭터 한 명. 같은 클래스로 만든 두 객체는 각자 HP를 가진다.
2. **캡슐화:** HP는 바깥에서 직접 바꾸지 못하며 `TakeDamage()`를 통해 바뀐다. 0 아래로 내려가지 않는다.
3. **추상화:** `Character`는 캐릭터들이 공통으로 제공하는 기능의 틀이다. abstract 클래스라 직접 생성할 수 없다.
4. **상속:** `Warrior : Character`는 공통 이름·HP·피격 기능을 물려받는다.
5. **다형성:** 전투 시스템은 `Character` 타입으로 호출하지만 실제 객체가 Warrior면 12, Mage면 18, Slime이면 6의 공격 피해를 계산한다.

`virtual`은 부모가 기본 구현(피해 5)을 제공하고 자식의 변경을 허용한다. `override`는 그 함수를 재정의한다. `abstract` 메서드라면 기본 구현 없이 자식에게 구현을 요구한다. 이 예제는 요청한 가상함수를 보여주기 위해 virtual을 사용한다.

## 한 번의 공격 흐름

Main에서 검사와 슬라임 생성 → BattleSystem.Fight 호출 → 공격자의 GetAttackDamage 호출 → 실제 자식의 override 실행 → 대상 TakeDamage 호출 → 결과 출력.

BattleSystem에 `if (attacker is Warrior)` 같은 종류별 분기는 없다. 검사 대신 마법사를 넣어도 전투 시스템 코드를 고치지 않는다. Main의 선택 분기는 사용할 객체를 정하는 역할이며 전투 계산 분기와 다르다.

## 전투 규칙

- 플레이어 먼저 공격, 살아 있는 적만 반격.
- 검사 HP50·피해12, 마법사 HP30·피해18, 슬라임 HP35·피해6.
- 한쪽 HP가 0이면 종료. 마지막 피해 로그는 실제 깎인 양을 표시.
- 출력은 `Action<string>`으로 받아 콘솔 의존을 Main에 둔다.
- 살아 있는 서로 다른 두 캐릭터를 전달하고, 공격 피해가 양수인 예제 전제다. 무피해·회복·도주 등을 추가할 때는 무승부/종료 조건도 설계해야 한다.

## 실행

작업 루트 `C:\Unity\1`에서:

```powershell
dotnet run --project Examples/SimpleBattle/SimpleBattle.csproj
dotnet run --project Examples/SimpleBattle/SimpleBattle.csproj -- mage
```

첫 실행은 검사 vs 슬라임, 두 번째는 마법사 vs 슬라임이다. Unity 내부에서 이 csproj를 실행할 필요는 없다.

## 새 캐릭터를 추가하는 방법

Character를 상속한 Archer 클래스를 만들고 생성자에서 HP를 정한다. GetAttackDamage를 override해 피해를 정한다. Main에서 `new Archer(...)`를 선택해 전달한다. BattleSystem은 그대로 쓴다.

규칙은 Character/파생 클래스, 진행은 BattleSystem, 대상 선택·출력 연결은 Main으로 분리했다. 클래스 수를 늘리는 것 자체가 좋은 설계라는 의미는 아니다. 이후 무기·스킬을 런타임에 교체해야 한다면 상속을 계속 늘리기보다 별도의 구성 요소로 분리할 수 있다.

## 검증

실제 빌드·실행 결과(2026-10-08):

- 검사: 피해 12 → 12 → 실제 잔여 HP만큼 11, 3턴 승리, 검사 HP38. 죽은 슬라임 반격 없음.
- 마법사: 피해 18 → 실제 잔여 HP만큼 17, 2턴 승리, 마법사 HP24. 같은 BattleSystem에서 다른 override가 실행됨.
- 두 실행 종료 코드 0. 기존 Unity 프로젝트 수정 없음.

외부 패키지 없는 예제로 `NuGet.Config`의 패키지 소스를 비워 두었다. 이 작업 환경에서는 사용자 NuGet 설정 읽기가 샌드박스에서 차단되어 복원만 승인된 환경에서 수행했고, 빌드·실행은 `--no-restore`로 확인했다.
