# 다중 언어 지원 계획

> **#99 (2026-11-09) 이후:** HTML 데모(`docs/Prototype.html`), 정답 도구(`tools/golden/`), `Core/PrototypeData.cs`, HTML 비교 시험(`*ParityTests`, `OverlayTests`, `BattlePlaybackTests`)은 지웠다. 아래의 비교 방법·골든 이야기는 포팅 당시 기록이다. 지금 규칙 시험은 `Tests/EditMode/BattleRulesTests.cs`·`ExpansionRulesTests.cs` 이고 출하 데이터로 돈다.

> 요청: "게임 내 모든 스크립트와 언어에 다중언어 지원기능을 추가할거야. 배포된 오픈소스나 에셋중에 가용 가능한 검증된 모듈이 있는지 살펴보고 직접 작업하는 것과 비교하여 계획 작성해서 제출해줘" (2026-11-06)
> 결정: **Unity Localization(`com.unity.localization`)으로 한다** (사용자, 2026-11-06). 결정 전 비교는 2절과 부록에 남긴다.
> 상태: L0~L6 구현(2026-11-06). 결과는 6절, 결정 기록은 `docs/dev/localization-decisions.tsv`.

## 1. 지금 게임에 있는 문장

`git ls-files` 의 C# 문자열과 데이터 에셋에서 한글이 든 것을 셌다(스크립트: `tools` 없이 node 로 셈, 2026-11-06).

| 묶음 | 어디 | 개수 | 성격 |
|---|---|---|---|
| UI 고정 문장 | `Game/Screens/BattleScreen.cs` 126, `CampaignScreen.cs` 31, `MenuScreen.cs` 6, `GameFlow.cs` 2 | 약 165 | 버튼, 카드, 규칙 요약, 결과 창. 일부는 수치를 끼워 넣는다 |
| 전투 기록·안내 | `Core/Battle.cs` 49, `Core/Battle.Enemy.cs` 49, `Core/BattleSession.cs` 13 | 약 111 | 규칙 코드가 문장을 직접 만든다. 조사(`Korean.Josa`) 15곳 |
| 게임 데이터 | `Assets/_Project/Data` 의 병종 6(이름, 글자, 설명, 강화 2개의 이름·설명), 장 11(번호, 제목, 소개, 보스 이름), 스켈레톤 | 약 88 | 기획자가 편집기로 고친다 |
| 추가 목표 문장 | `Core/GameDataTypes.cs` `Bonus.Text` | 3 | 수치를 끼워 넣는다 |
| 에디터 도구 | `Editor/*.cs`, 데이터 검사 문장(`Core/DataSet.cs`), 인스펙터 툴팁 | 약 80 | 기획자용. 플레이어는 보지 않는다 |
| 시험 기준값 | `Core/PrototypeData.cs`, `tools/golden/out/*.json` | | HTML 데모의 한국어. 번역 대상이 아니다 |

번역 대상은 플레이어가 보는 약 370 문장이다(UI, 전투 기록, 게임 데이터, 추가 목표). 에디터 도구는 처음에는 빼는 것을 권한다(정해 주실 것 3).

### 걸리는 점

- **규칙 코드가 문장을 만든다.** `Core` 는 `noEngineReferences` 라 Unity 패키지를 쓸 수 없다. 어떤 방법을 고르든 `Core` 는 완성된 한국어 대신 "메시지 키 + 값"(예: `log.killed {name}`)을 내고, 화면 쪽이 언어에 맞춰 문장을 만들어야 한다. 이것이 가장 큰 일이다.
- **조사.** "기사가 / 검사가 / 뱀파이어를" 처럼 받침에 따라 조사가 바뀐다. 다른 언어에는 없는 규칙이라 한국어 전용 형식 지정자가 필요하다.
- **글자 배지.** 병종 글자("기", "창")는 한 글자 배지다. 다른 언어에서는 무엇으로 쓸지 정해야 한다(예: 영어 "K"). 데이터에 언어별 값이 필요하다.
- **글꼴.** 지금은 OS 글꼴 목록(맑은 고딕 등)으로 한글을 그린다(`Game/Screens/Ui.cs`). 언어마다 글꼴 목록이 필요하다. 일본어·중국어를 넣으면 빌드에 글꼴 파일을 넣어야 할 수 있다(#16 과 같은 허락 문제).
- **시험.** HTML 비교 시험(전투 기록, UI 문장)은 한국어 문장을 글자까지 비교한다. 한국어를 기준 언어로 두고, 시험은 한국어로 그린 결과를 비교하게 남긴다.

## 2. 쓸 수 있는 검증된 모듈

| 후보 | 출처·상태 | 비용·라이선스 | 이 저장소에 맞나 |
|---|---|---|---|
| **Unity Localization** (`com.unity.localization`) | Unity 공식. 6000.3 용 released 1.5.8~1.5.13 이 문서에 있고, 이 프로젝트 에디터의 레지스트리에는 1.5.14 가 있다(`package list --scope available`, 설치 미리 보기 성공) | 무료. UPM 패키지라 저장소에는 `manifest.json` 한 줄만 들어간다 | **맞다.** 공개 저장소에 파일을 넣지 않는다 |
| I2 Localization | Asset Store. 최신 2.8.22 f6(2024-09-24), Unity 6 지원 표기 없음 | 유료(정가 $45) | **맞지 않다.** 유료 에셋이라 Synty 처럼 저장소에 못 넣고, 클론하는 사람마다 사야 한다 |
| Lean Localization 등 Asset Store 무료 에셋 | Asset Store | 무료지만 Asset Store 약관이라 공개 저장소에 다시 배포할 수 없다(추정: 표준 Asset Store EULA) | **맞지 않다.** I2 와 같은 문제 |
| GitHub 소형 라이브러리 (FomTarro/Localization, megurte/UnityLocalization, Setsuodu Localization 등) | 개인 프로젝트 | 검색 결과에 라이선스가 보이지 않았다 | **권하지 않는다.** 유지 보수와 라이선스를 확인하지 못했다 |
| Jelewow Unity Sheets Localization | OpenUPM. Google Sheets 를 Unity Localization 표로 가져오는 편집 도구 | 라이선스 미확인 | 나중에 번역가 작업을 Google Sheets 로 할 때만. Unity Localization 자체에도 Google Sheets 가져오기가 있다 |

Unity Localization 이 주는 것(공식 문서):
- 언어별 문자열 표(String Table)
- Smart Strings: 자리 표시자, 복수형, 그리고 `FormatterBase` 를 이어 만드는 사용자 형식 지정자. 조사 형식 지정자를 여기에 붙인다
- 언어별 에셋 표(글꼴, 그림)
- 가짜 번역(pseudo-localization). 번역 전에 글이 길어지거나 빠지는 곳을 찾는다
- XLIFF·CSV·Google Sheets 가져오기·내보내기

알려진 비용: 표를 Addressables 로 싣는다. 빌드 때 Addressables 빌드 단계가 붙는다. 의존 패키지는 설치 때 `package add` 결과로 확인한다(미리 보기에는 나오지 않았다).

## 3. 구조

```
Core (언어 모름)                      Game (Unity Localization)                  화면
Battle, BattleSession      ──►   Message(Id, 값들)   ──►   Loc.Text(msg)   ──►   Text
 LogEntry, BattleEvent, 거절 사유       표 키 = Id, Smart String 으로 값 끼움
GameData (데이터 에셋)      ──►   이름·설명 키(unit.knight.name …)  ──►  Loc.Text(key)
```

- **Core 는 문장을 만들지 않는다.** 전투 기록, 안내, 거절 사유는 `Message`(메시지 id 와 값 목록)로 낸다. 값에는 유닛·장 같은 대상 키도 들어간다. Core 는 `noEngineReferences` 그대로다.
- **Game 의 `Loc` 하나가 문장을 만든다.** `LocalizationSettings.StringDatabase` 에서 지금 언어의 표를 읽고 Smart String 으로 값을 끼운다. UI 를 코드로 만들기 때문에 `LocalizeStringEvent` 컴포넌트 대신 이 함수를 부른다. 언어가 바뀌면 `LocalizationSettings.SelectedLocaleChanged` 를 받아 지금 화면을 다시 그린다(화면은 이미 상태가 바뀔 때마다 다시 만든다).
- **표 셋.**

| 표 | 내용 | 키 예 |
|---|---|---|
| `UI` | 화면 고정 문장 | `battle.endTurn`, `camp.reset.armed` |
| `Battle` | 전투 기록·안내·거절 사유 | `log.killed`, `hint.selectAlly` |
| `Content` | 게임 데이터 문장 | `unit.knight.name`, `unit.knight.glyph`, `upgrade.knight_def.desc`, `stage.s1.brief`, `boss.s1.name` |

- **게임 데이터.** 데이터 에셋은 이름·설명을 직접 갖지 않고 키를 id 에서 만든다(`unit.<key>.*`, `stage.<id>.*`, `upgrade.<id>.*`). 게임 데이터 편집기는 기준 언어(한국어) 표의 그 칸을 보여 주고 고친다. 기획자 작업 흐름은 그대로다.
- **조사.** Smart String 사용자 형식 지정자 `josa` 를 만든다. 한국어 표에 `{name:josa(이|가)} 쓰러졌다.` 처럼 쓴다. 다른 언어 표는 쓰지 않는다.
- **언어 고르기.** 패키지의 기본 고르기 순서를 쓴다. 저장한 언어(`PlayerPrefLocaleSelector`)가 먼저이고, 없으면 OS 언어(`SystemLocaleSelector`), 그것도 없으면 한국어다.
- **글꼴.** 지금처럼 OS 글꼴 이름 목록을 쓴다. 목록을 `UI` 표의 `font.body`, `font.display` 칸에 언어별로 둔다. 언어를 바꾸면 글꼴도 바뀐다. 글꼴 파일을 들이는 일은 #16 과 같다.
- **시험.**
  - HTML 비교 시험은 한국어로 고정해 돈다. 한국어 표로 그린 글이 지금 기준값과 같아야 한다.
  - "모든 키에 모든 언어가 있다" 시험을 더한다.
  - "Core 가 한글 문자열을 갖지 않는다" 시험을 더한다(문장을 다시 Core 에 넣는 실수를 막는다).
- **빌드.** 표는 Addressables 로 실린다. 플레이어 빌드 전에 Addressables 빌드가 돌아야 한다. 이 프로젝트는 아직 플레이어 빌드를 하지 않았으므로 L6 에서 빌드 확인을 처음 한다.

## 4. 단계 (작은 PR 로)

각 단계는 끝에 확인이 있다. 앞 단계 확인이 통과해야 다음으로 간다. 규모는 모두 추정이다.

| 단계 | 하는 일 | 확인 | 규모 |
|---|---|---|---|
| **L0 위험 확인** (버리는 worktree) | 패키지 1.5.14 를 깔고 넷을 본다: (a) 우리 asmdef 와 컴파일, 설치가 바꾸는 패키지 목록, (b) EditMode 시험에서 표 문장을 바로 읽기(`WaitForCompletion`), (c) `josa` 형식 지정자, (d) 언어를 바꾸면 OS 글꼴로 그려지기 | 넷의 결과를 이 문서에 적는다. 막히는 것이 있으면 L2 전에 사용자에게 알린다 | 반나절 |
| **L1 기획서** | `docs/ui/localization.md`: 지원 언어, 언어 고르는 곳(메뉴 오른쪽 위, 설정), 처음 언어, 언어별 글꼴, 글자 배지 규칙. 기획 에이전트가 쓴다 | 5절의 정할 것이 기획서에 들어감 | 1~2 시간 |
| **L2 Core 메시지** | `LogEntry`, `BattleEvent`, `CommandResult.Reason`, `BattleSession.Hint`, `Bonus.Text` 를 `Message` 로 바꾼다. 아직 패키지 없이 Game 쪽 임시 한국어 렌더러로 지금 글을 만든다. 조사도 렌더러로 옮긴다 | HTML 비교 시험 전부(BattleParity 44, Overlay 14, UiTextParity 34, Campaign 4)가 바뀌지 않고 통과. "Core 에 한글 없음" 시험 | 하루 |
| **L3 패키지와 표** | 패키지 설치, Localization Settings, 한국어·영어 Locale, 표 셋. 한국어 표는 지금 문장에서 **스크립트로 뽑는다**(L2 의 렌더러 문장과 UI 코드 문자열, 데이터 에셋). `josa` 형식 지정자. L2 의 임시 렌더러를 `Loc` 로 바꾸고 지운다 | 같은 HTML 비교 시험이 한국어 표로 통과 | 하루 |
| **L4 화면과 데이터 연결** | 화면 코드의 문자열을 `UI` 표 키로 바꾼다. 데이터 에셋의 이름·설명을 `Content` 키로 옮기고 편집기를 맞춘다 | UiTextParity 34 통과, "모든 키·모든 언어" 시험(빨강 확인), 데이터 편집기로 이름을 고치면 화면이 바뀜 | 하루 |
| **L5 언어 바꾸기** | 언어 고르기 UI, 저장, OS 언어, 언어별 글꼴 | 실제 화면에서 언어를 바꾸면 메뉴·캠페인·전투·결과 창이 모두 바뀜(스크린샷) | 반나절 |
| **L6 영어와 빌드** | 영어 표(번역 방식은 5절), 가짜 번역으로 화면 넘침 찾기, Addressables 포함 플레이어 빌드 | 영어 빈칸 0, 가짜 번역 스크린샷에서 잘린 글 없음, 빌드한 게임에서 언어 바꾸기 | 번역 분량에 따름 |

L2 를 패키지보다 먼저 하는 이유: 지금의 HTML 비교 시험이 그대로 안전망이 된다. 규칙과 문장을 떼는 큰 변경을 패키지 설치와 섞지 않는다.

## 5. 정한 것 (사용자, 2026-11-06)

1. **지원 언어.** 한국어(기준)와 영어. 나중에 언어를 더할 수 있게 만든다: 언어 목록·글꼴은 Locale 과 표에서만 정하고 코드에 언어를 박지 않는다.
2. **기획 방향.** `docs/design/` 에는 적지 않는다. L1 기획서가 이 절을 "정한 세부" 로 쓴다.
3. **에디터 도구.** 번역하지 않는다. 기획자용 편집기·데이터 검사 문장은 한국어만.
4. **번역 방식.** 기계 초안 뒤 사람 검토. Google Sheets 연동은 넣지 않는다.

## 6. 구현 결과 (2026-11-06)

계획과 달라진 것:
- **표는 셋이 아니라 하나(`Text`).** 키에 이미 접두사(`log.`, `unit.`, `battle.`)가 있어 표를 나눌 까닭이 없었다. `Loc.T(key)` 하나로 부른다.
- **L2 와 L3 를 합쳤다.** 임시 한국어 렌더러를 쓰고 지우는 대신 Core 를 Message 로 바꾸면서 바로 표로 그렸다. HTML 비교 시험이 그대로 안전망이었다.
- **L0 은 버리는 worktree 대신 작업 브랜치에서 했다.** 새 worktree 는 Synty 390 MB 를 다시 들인다. 패키지 설치는 커밋 하나라 되돌리기 쉽다.

| 단계 | 결과 | 확인 |
|---|---|---|
| L0 | 패키지 1.5.14 가 우리 asmdef 와 컴파일된다(Addressables, Scriptable Build Pipeline, Profiling Core 가 따라온다). EditMode 에서 표 문장을 바로 읽는다. `josa` 형식 지정자가 받침을 따른다(기사가, 백작을, 스폰(궁수)을) | eval, EditMode |
| L1 | `docs/ui/localization.md` | 기획 에이전트 |
| L2·L3 | 전투 기록·띠·안내·거절 사유·추가 목표·보름달 표시가 Message 키. 72 행 | EditMode 160/160, 한국어 글이 HTML 기준값과 같다 |
| L4 | 데이터 문장 98 행을 에셋에서 표로(키는 id 에서), 화면 리터럴 128 개를 표로. 데이터 편집기 문장 칸. Core 한글 없음 시험 | EditMode 161/161 |
| L5 | 메인 메뉴 언어 줄, 언어별 글꼴, 저장. 모든 키·모든 언어 시험 | EditMode 163/163. 실제 게임을 마우스로 몰아 메뉴→캠페인→전투→메뉴가 영어로 바뀌고 다시 켜도 유지됨(스크린샷 `verify-artifacts/loc-L5-live/`) |
| L6 | 영어 294 키 빈칸 0. 영어 화면에서 넘친 "Enemy Threat Range" 를 "Threat Range" 로 줄였다. Windows 플레이어 빌드에 Addressables 내용(`StreamingAssets/aa`, 표 번들)이 함께 들어갔다. 빌드한 게임은 켜져서 장면을 싣고 25초 동안 오류가 없었다. 빌드한 게임 화면의 언어 바꾸기는 눈으로 보지 않았다 | `LocalizationTableTests`, `Builds/loc` (git 밖), Player.log |

## 부록. 직접 만들기와 비교 (결정 전 자료)

| 기준 | Unity Localization | 직접 만들기 |
|---|---|---|
| 처음 드는 일 | 패키지 설치, 표 3개, 조사 형식 지정자, Addressables 설정 | 표 형식, 읽기, 끼워 넣기, 조사, 언어 고르기, 빠진 키 찾기 |
| 계속 드는 일 | 패키지 업데이트 따라가기 | 복수형, 가짜 번역, 에셋 언어별 바꾸기, 번역가 도구를 필요할 때마다 직접 |
| 번역가 작업 | 에디터 표, CSV, XLIFF, Google Sheets | CSV 만 |
| Core 에서 쓰기 | 못 쓴다(메시지 구조로 해결) | 쓸 수 있다 |
| 빌드 | Addressables 빌드 단계 추가 | 추가 단계 없음 |

## 출처

- [Unity 6.3 패키지 목록: Localization](https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/packages-all/pack-safe/com-unity-localization)
- [Localization 1.5 매뉴얼](https://docs.unity3d.com/Packages/com.unity.localization@1.5/manual/index.html)
- [Creating a Custom Formatter (Localization 1.3)](https://docs.unity.cn/Packages/com.unity.localization@1.3/manual/Smart/Creating-a-Custom-Formatter.html)
- [I2 Localization (Asset Store)](https://assetstore.unity.com/packages/tools/localization/i2-localization-14884)
- [Unity Sheets Localization (OpenUPM)](https://openupm.com/packages/com.jelewow.unity-sheets-localization/)
- [FomTarro/Localization](https://github.com/FomTarro/Localization), [megurte/UnityLocalization](https://github.com/megurte/UnityLocalization)
