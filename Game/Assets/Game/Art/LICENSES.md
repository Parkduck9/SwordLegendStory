# 외부 에셋 출처 · 라이선스

| 에셋 | 출처 | 라이선스 | 사용 위치 | 확인일 |
|---|---|---|---|---|
| Kenney Nature Kit 2.1 (stone_tallA~J, rock_largeA~F) | https://kenney.nl/assets/nature-kit | CC0 1.0 (퍼블릭 도메인). 개인·교육·상업 무료, 출처 표기 의무 없음 | `Kenney/NatureKit/` — 전투장 돌기둥·바위 더미 | 2026-10-08 |

| Quaternius Universal Animation Library [Standard] (UAL1_Standard.fbx — 마네킹 + 43개 동작) | https://quaternius.itch.io/universal-animation-library (0원 다운로드) | CC0 1.0 (퍼블릭 도메인). 개인·교육·상업 무료, 출처 표기 불필요 | `Quaternius/UAL/` — 플레이어·적 몸과 애니메이션 | 2026-10-08 |

| Quaternius Universal Animation Library 2 [Standard] (UAL2_Standard.fbx — 검 막기·넉백·검 베기 등 42개 동작) | https://quaternius.itch.io/universal-animation-library-2 (0원 다운로드, 17MB) | CC0 1.0 (압축 안 `License.txt` 확인, 원본을 `Quaternius/UAL2/License.txt`로 보관). 출처 표기 불필요 | `Quaternius/UAL2/` — 막기(Sword_Block)·깊게 베임(Hit_Knockback) | 2026-10-08 |

| Quaternius Universal Base Characters [Standard] (Superhero_Male_FullBody.fbx · 머리 모양 4종 · 질감) | https://quaternius.itch.io/universal-base-characters (0원 다운로드, 122MB 중 20MB 사용) | CC0 1.0 (압축 안 `License_Standard.txt` 확인, `Quaternius/UBC/License.txt`로 보관). 출처 표기 불필요 | `Quaternius/UBC/` — 플레이어·적 몸(마네킹 대체), 머리·수염·눈썹 | 2026-10-08 |

| KayKit Character Animations 1.1 Free (Rig_Medium 근접 전투·기본·이동 4개 FBX) | https://kaylousberg.itch.io/kaykit-character-animations (0원 다운로드, 14MB 중 9.6MB 사용) | CC0 1.0 (압축 안 `License.txt` 확인, `KayKit/License.txt`로 보관). 출처 표기는 선택(의무 아님) | `KayKit/` — 옆으로 달리기·뒷걸음·회피·막기·피격·적 공격(베기·회전·찌르기·발차기·뛰어 내려베기)·던지기 | 2026-10-08 |

| Noto Sans KR (가변 굵기) | https://github.com/google/fonts/tree/main/ofl/notosanskr | **SIL OFL 1.1** — 무료·상업 배포 가능, 화면 크레딧 의무 없음. **배포 시 저작권 표시+라이선스 문서 동봉 필요** | `Fonts/` — 본문 UI | 2026-10-08 |
| 나눔손글씨 붓 (Nanum Brush Script) | https://github.com/google/fonts/tree/main/ofl/nanumbrushscript | **SIL OFL 1.1** — 위와 같음 | `Fonts/` — 제목·승리/패배 | 2026-10-08 |

| Kenney Impact Sounds · RPG Audio · Interface Sounds (79개 ogg) | https://kenney.nl/assets/impact-sounds · rpg-audio · interface-sounds | CC0 1.0 — 상업 무료, 출처 표기 의무 없음(동봉 License.txt) | `Audio/Kenney/` — 타격·충돌·패링·발소리·UI | 2026-10-08 |

- 합성 소리(베기·대쉬·적 예고음 5종·큰 북·승리/패배·배경음 2곡)는 `Editor/SoundSynth.cs`로 직접 생성(외부 음원 아님).
- OFL 문서 동봉: `Assets/StreamingAssets/Licenses/OFL-NotoSansKR.txt`, `OFL-NanumBrushScript.txt` — StreamingAssets는 빌드에 그대로 복사되므로 배포본에 자동 포함된다(검사로 확인). 글꼴 자체를 따로 판매·재배포하지 않는다.
- Quaternius 확인 근거: 공식 FAQ("can be used for free without the need for attribution in commercial, educational, and personal projects. All models are under the CC0 License", "No, attribution is not necessary")와 압축 파일 안 `License.txt`(CC0 1.0, 원본을 `Quaternius/UAL/License.txt`로 보관).
- 확인 근거: Kenney 공식 지원 페이지("all game assets ... public domain licensed (CC0) ... free to use, even in commercial projects", "Attribution is not required")와 압축 파일 안 `License.txt`(원본을 `Kenney/NatureKit/License.txt`로 함께 보관).
- Kenney 로고는 사용하지 않는다(공식 페이지 안내).
- 그 밖의 질감·먼 산 메시는 `Editor/GeneratedArt.cs`로 직접 생성(외부 에셋 아님).
