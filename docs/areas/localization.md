# 다중 언어

## 경로
- 기획 방향: `docs/design/` (언어 방향 없음. 사용자 결정을 기획서가 "정한 세부" 로 쓴다)
- 기획서: `docs/ui/localization.md` (지원 언어, 언어 줄, 배지, 글꼴, 영어 용어·문체)
- 계획과 결정 기록: `docs/dev/localization-plan.md`, `docs/dev/localization-decisions.tsv`
- 패키지: `com.unity.localization` 1.5.14 (Addressables 2.x 가 따라 들어온다)
- 표: 문자열 표 하나 `Text` (`Assets/_Project/Localization/Tables/`), 언어 `Locales/ko.asset`(기준, 프로젝트 언어), `en.asset`(빈 칸은 한국어로). 설정 `Localization Settings.asset`
- 코드: `Core/Message.cs`(키 + 값), `Game/Loc.cs`(`Loc.T`, `Loc.Text`, 조사 `JosaFormatter`), `Editor/SourceText.cs`(데이터 편집기의 한국어 문장 칸), 언어 줄 `Game/Screens/MenuScreen.cs`, 글꼴 `Game/Screens/Ui.cs`
- 시험: `Tests/EditMode/LocalizationTableTests.cs`(모든 키에 모든 언어, 언어 이름·글꼴), `SmokeTests.CoreHasNoKoreanText`, HTML 비교 시험은 한국어 표로 돈다(`KoreanLocale.cs`)

## 결정
| 결정 | 상태 | 날짜 | 출처 |
|---|---|---|---|
| 다중 언어는 Unity Localization 패키지로 한다 | 시험 중 | 2026-11-06 | 사용자 원문 "Unity Localization으로 작업하자", 이슈 #23 |
| 처음은 한국어·영어. 언어를 더할 수 있는 구조로 | 시험 중 | 2026-11-06 | 사용자 원문 "한국어 영어로 세팅, 이후 다중언어로 확장 가능하도록 구조 조성", 이슈 #23 |
| 에디터 도구는 한국어만 | 시험 중 | 2026-11-06 | 사용자 원문 "에디터 언어는 한국어 온리", 이슈 #23 |
| 번역은 기계 초안 뒤 사람 검토 | 시험 중 | 2026-11-06 | 사용자 선택, 이슈 #23 |
| 언어 방향은 docs/design 에 적지 않고 L1 기획서가 정한 세부로 쓴다 | 시험 중 | 2026-11-06 | 사용자 선택, 이슈 #23 |

## 열린 작업
- 영어 표는 기계 초안이다. 사람 검토가 남았다(결정 4). 검토할 사람은 기획서 열린 질문
- 전투 머리 통계 이름표 중 14자를 넘는 것: `moon.nextTurn` "Full Moon Next Turn"(보드 위 표시와 같은 키)
- 결과 창(승리·패배)의 영어 화면은 실제 게임에서 아직 보지 않았다. 표와 한국어 비교 시험만 확인
- PlayMode 시험은 이번 작업에서 돌리지 않았다

## 언어 더하기
1. `Window > Asset Management > Localization Tables` 또는 Locale Generator 로 Locale 을 만들고 `LocaleName` 을 그 언어 이름으로, `SortOrder` 를 순서로 둔다. 폴백은 ko
2. `Text` 표에 그 언어 열이 생긴다. 모든 칸(문장, `*.glyph` 배지, `font.body`, `font.display`)을 채운다
3. `LocalizationTableTests` 가 빈 칸을 잡는다. 언어 줄은 Locale 목록에서 자동으로 생긴다

## 맡길 때
- 모델: 표 행 옮기기·화면 문자열은 opus low(또는 sonnet), 구조 변경은 opus
- 함정: `string + Message` 는 조용히 컴파일되어 화면에 키 이름이 나온다. Message 값(이름, 설명, 안내)은 늘 `Loc.Text(...)` 로 감싼다. UI 비교 시험이 잡는다
- 함정: 에디터(재생 아님)에서는 도메인 다시 읽기 뒤 `SelectedLocale` 이 null 이다. `Loc.Current` 가 프로젝트 언어(ko)로 대신한다
- 함정: Core 는 `noEngineReferences` 라 패키지를 못 쓴다. Core 는 `Message` 만 낸다. 한글 리터럴을 Core 에 넣으면 `CoreHasNoKoreanText` 가 실패한다(`DataSet.cs` 검사 문장은 에디터용이라 뺐다)
- 함정: 데이터 문장(병종·강화·장 이름 등)은 에셋이 아니라 표에 있다. 데이터 편집기 "고른 항목을 기본값으로" 는 수치만 되돌리고 문장은 되돌리지 않는다. 새 장을 복제하면 문장 칸이 비어 있다
- 함정: josa 형식 지정자는 낱말 뒤에 조사를 붙여 낱말째 쓴다(`{0:josa(이|가)}`). 「」 같은 기호 뒤의 조사에는 쓰지 못해 고정 조사를 둔다
- 함정: HTML 비교 시험은 한국어 표로 고정해 돈다. 기준값(`tools/golden/out`)은 한국어다. `Core/PrototypeData.cs` 는 이제 문장 없이 생성된다
