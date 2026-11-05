# 다중 언어 지원 계획

> 요청: "게임 내 모든 스크립트와 언어에 다중언어 지원기능을 추가할거야. 배포된 오픈소스나 에셋중에 가용 가능한 검증된 모듈이 있는지 살펴보고 직접 작업하는 것과 비교하여 계획 작성해서 제출해줘" (2026-11-06)
> 상태: 계획. 구현 전이다. 정할 것은 맨 아래 "정해 주실 것".

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

## 3. 직접 만들기와 비교

직접 만든다면: `Core` 안의 순수 C# 표(언어 → 키 → 문장, CSV 나 JSON), `{name}` 끼워 넣기, 조사 함수, 언어 고르기, 글꼴 목록. 표 편집은 CSV 로 한다.

| 기준 | Unity Localization | 직접 만들기 |
|---|---|---|
| 처음 드는 일 | 패키지 설치, 표 3개, 조사 형식 지정자, Addressables 설정 | 표 형식, 읽기, 끼워 넣기, 조사, 언어 고르기, 빠진 키 찾기 |
| 계속 드는 일 | 패키지 업데이트 따라가기 | 복수형, 가짜 번역, 에셋 언어별 바꾸기, 번역가 도구를 필요할 때마다 직접 |
| 번역가 작업 | 에디터 표, CSV, XLIFF, Google Sheets | CSV 만(도구를 더 만들지 않으면) |
| `Core` 에서 쓰기 | 못 쓴다. 키 + 값으로 넘겨야 한다 | 쓸 수 있다. 다만 키 + 값 구조는 어차피 필요하다(규칙과 문장을 떼어야 시험이 언어와 무관해진다) |
| 시험 | EditMode 에서 표 읽기를 확인해야 한다(시제품 단계 b) | 순수 C# 라 쉽다 |
| 빌드 | Addressables 빌드 단계 추가 | 추가 단계 없음 |
| 위험 | 비동기 API, Addressables 설정 실수 | 도구가 모자라 번역가 일이 늘고, 기능을 하나씩 다시 만든다 |

**권하는 안.** 두 가지를 합친다.
1. `Core` 는 문장 대신 **메시지 키 + 값** 을 낸다. 규칙 코드가 언어를 모르게 된다. 어느 방법이든 필요한 일이다.
2. 화면 쪽 문장 만들기는 **Unity Localization** 으로 한다. 공식 패키지이고 무료이며 공개 저장소 규칙에 맞는다. 번역가 도구(CSV, Sheets, XLIFF), 가짜 번역, 언어별 글꼴 바꾸기를 직접 만들지 않아도 된다.

직접 만들기가 이기는 곳은 "Core 에서 바로 쓸 수 있다" 하나다. 이 이점은 1번 구조로 없어진다. 다만 시제품 단계에서 EditMode 시험이나 Addressables 가 막히면 직접 만들기로 바꾼다. 그때도 1번 구조는 그대로 쓴다.

## 4. 단계 (작은 PR 로)

각 단계는 끝에 확인이 있다. 앞 단계 확인이 통과해야 다음으로 간다.

| 단계 | 하는 일 | 확인 | 규모(추정) |
|---|---|---|---|
| **L0 시제품** (버리는 브랜치) | 별도 worktree 에 패키지를 깔고 넷을 본다: (a) 6000.3·우리 asmdef 와 함께 컴파일, (b) EditMode 시험에서 표 문장을 바로 읽기, (c) 조사 형식 지정자 `{name:josa(이,가)}`, (d) 영어 표로 바꿨을 때 OS 글꼴로 그려지기 | 넷 모두 통과면 Unity Localization, 하나라도 막히면 직접 만들기로 정하고 이유를 남긴다 | 반나절 |
| **L1 기획서** | `docs/ui/localization.md`: 지원 언어, 언어 고르는 곳(메뉴·설정), 처음 언어(OS 언어), 언어별 글꼴, 글자 배지 규칙. CLAUDE.md "역할과 문서 흐름" 대로 기획 에이전트가 쓴다 | 정해 주실 것 1~4 가 기획서에 들어감 | 1~2 시간 |
| **L2 Core 메시지 키** | `LogEntry`, `BattleEvent` 의 `Text`, 거절 사유, 안내를 `MessageId + 값` 으로 바꾼다. 한국어로 그리는 렌더러를 두어 지금과 같은 글을 만든다. 조사도 렌더러로 옮긴다 | HTML 비교 시험 전부(BattleParity 44, Overlay 14, UiTextParity 34)가 바뀌지 않고 통과 | 하루 |
| **L3 표 만들기** | 패키지 설치. 표 셋(UI, 전투, 게임 데이터). 지금 문장에서 한국어 표를 **스크립트로 뽑아** 넣는다(손으로 옮기지 않는다). 조사 형식 지정자 | 뽑은 한국어 표로 그린 글이 L2 와 같음(같은 시험) | 하루 |
| **L4 화면 연결** | 화면 코드의 문자열을 표 키로 바꾼다. 데이터 에셋의 이름·설명은 키로 두고 편집기에서 한국어를 표에 쓴다 | UiTextParity 34 통과, "표에 없는 키" 시험 추가(빨강 확인) | 하루 |
| **L5 언어 바꾸기** | 언어 고르기 UI, 저장(PlayerPrefs), 처음엔 OS 언어, 언어별 글꼴 | 실제 화면에서 언어를 바꾸면 메뉴·전투·결과 창이 바뀜(스크린샷) | 반나절 |
| **L6 두 번째 언어** | 영어 표(초안 번역 뒤 사람이 검토), 가짜 번역 | 모든 키에 모든 언어가 있음(시험), 가짜 번역으로 화면 넘침 확인 | 번역 분량에 따름 |

L2 는 규칙 비교 시험이 안전망이다. 그래서 L3 보다 먼저 한다.

## 5. 정해 주실 것

1. **지원 언어.** 한국어 말고 무엇을 넣을지. 권하는 처음 묶음은 한국어(기준)와 영어다. 일본어·중국어는 글꼴 파일이 필요할 수 있다.
2. **기획 방향.** 다중 언어는 화면 기능이라 기획서를 거친다. 지원 언어와 언어 고르는 곳을 `docs/design/` 에 적어 주시면 기획 에이전트가 기획서(L1)를 쓴다. 적지 않으시면 위 권하는 묶음으로 기획서를 쓰고 "정한 세부" 로 남긴다.
3. **에디터 도구.** "모든 스크립트" 에 기획자용 편집기와 데이터 검사 문장도 넣을지. 권하는 것은 처음에는 빼는 것이다(플레이어가 보지 않고, 기획자가 한국어를 쓴다).
4. **번역 방식.** 사람 번역가, 기계 초안 뒤 검토, 또는 둘 다. 번역가가 Google Sheets 를 쓰면 Unity Localization 의 Sheets 가져오기를 L6 에 넣는다.

## 출처

- [Unity 6.3 패키지 목록: Localization](https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/packages-all/pack-safe/com-unity-localization)
- [Localization 1.5 매뉴얼](https://docs.unity3d.com/Packages/com.unity.localization@1.5/manual/index.html)
- [Creating a Custom Formatter (Localization 1.3)](https://docs.unity.cn/Packages/com.unity.localization@1.3/manual/Smart/Creating-a-Custom-Formatter.html)
- [I2 Localization (Asset Store)](https://assetstore.unity.com/packages/tools/localization/i2-localization-14884)
- [Unity Sheets Localization (OpenUPM)](https://openupm.com/packages/com.jelewow.unity-sheets-localization/)
- [FomTarro/Localization](https://github.com/FomTarro/Localization), [megurte/UnityLocalization](https://github.com/megurte/UnityLocalization)
