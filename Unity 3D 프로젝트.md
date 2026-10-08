Unity 3D 프로젝트 

# Game Specification

## 1. 프로젝트 개요

- Engine: Unity
- Game Type: 3D
- Platform: PC
- Input: WASD 로 입력 + 좌클릭[확인] 
- Language: C#
- Target Resolution: 1920x1080 [설정에 따라 다르게]

## 2. 게임 목표

플레이어가 맵을 이동하며 적과 전투하고
스테이지의 목표를 달성하는 2D 게임을 제작한다.

현재 목표는 전체 게임을 완성하는 것이 아니라
플레이 가능한 Vertical Slice를 만드는 것이다.

---

## 3. 게임 흐름

Title
↓
Stage
↓
Battle
↓
Victory / Defeat
↓
Title

---

## 4. 플레이어

플레이어는 다음 기능을 가진다.

- 앞뒤좌우 이동
- 점프
- 공격
- 피격
- 사망

### 조작

W : 앞으로 이동
A : 좌측으로 이동
S : 뒤로 이동
D : 우측으로 이동

---

## 5. 플레이어 구조

Player
- PlayerController
- PlayerMovement
- PlayerCombat
- PlayerHealth
- 추가해야할 내용이 있을 시 상담
하나의 클래스가 모든 기능을 담당하지 않도록 한다.

---

## 6. Enemy

첫 번째 테스트 적은 1종만 만든다.

기능:

- Idle
- Chase
- Attack
- Hit
- Dead

상태 관리는 FSM 방식으로 구현한다.

---

## 7. Combat

플레이어 공격이 적의 HitBox와 충돌하면 데미지를 준다.

Player
→ Attack
→ Hit Detection
→ Enemy.TakeDamage()
→ HP 감소
→ HP <= 0
→ Dead

---

## 8. 코드 규칙

- MonoBehaviour에 모든 로직을 몰아넣지 않는다.
- 각 클래스는 하나의 주요 책임을 가진다.
- public 변수 사용을 최소화한다.
- Inspector 노출이 필요하면 [SerializeField]를 사용한다.
- 확장 가능한 구조를 우선한다.
- 의미 없는 Manager 클래스를 만들지 않는다.
- 주요 코드에는 설명 주석을 작성한다.

---

## 9. 구현 순서

다음 순서대로 구현한다.

### Phase 1
- [ ] Unity 프로젝트 구조 생성
- [ ] Scene 생성
- [ ] Player 오브젝트 생성

### Phase 2
- [ ] Player 이동
- [ ] Player 점프
- [ ] Camera Follow

### Phase 3
- [ ] Enemy 생성
- [ ] Enemy FSM
- [ ] Enemy Chase

### Phase 4
- [ ] Player 공격
- [ ] Damage System
- [ ] Enemy Death

### Phase 5
- [ ] Game Flow
- [ ] Victory
- [ ] Defeat

---

## 10. 작업 규칙

Astra는 작업 전에 현재 프로젝트 구조를 확인한다.

이미 존재하는 시스템을 불필요하게 다시 작성하지 않는다.

기능을 구현할 때:

1. 기존 구조 분석
2. 구현 계획 수립
3. 필요한 파일 생성/수정
4. 컴파일 오류 확인
5. 기능 연결 확인

한 번에 지나치게 많은 시스템을 구현하지 않는다.

불명확한 요구사항이 있다면 임의로 큰 시스템을 만들지 말고
가장 단순하고 확장 가능한 형태를 선택한다.