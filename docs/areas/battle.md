# 전투

## 경로
- 기획 방향: `docs/design/` (combat, vampire, units, map, campaign, progression 등). 기획서: `docs/systems/`, `docs/content/`, `docs/ui/`
- 웹 데모: `docs/Prototype.html` (포팅 출발점. docs/design 과 다르면 docs/design 을 따른다)
- 계획: `docs/dev/unity-port-plan.md`
- 데이터: `Assets/_Project/Data/` (편집기 사용법 `docs/guides/data-editor.md`)
- 코드: `Assets/_Project/Scripts/Core/` (규칙, Unity 참조 없음), `Assets/_Project/Scripts/Game/` (화면·입력)
- 테스트: `Assets/_Project/Tests/EditMode/`
- HTML 정답 도구: `tools/golden/prototype-golden.mjs` (설명은 `docs/dev/unity-port.md`)
- 씬: `Assets/_Project/Scenes/Main.unity` (Build Settings 0번)
- 검증: `.claude/skills/verify-unity/`

## 결정
| 결정 | 상태 | 날짜 | 출처 |
|---|---|---|---|
| 게임 수치·스테이지는 데이터 에셋(Assets/_Project/Data)에 두고 기획자용 편집기로 고친다. 에셋은 데이터 기획서(docs/content)를 구현한 것이고, 기획서가 비어 있는 동안 기본값은 HTML 데모 값이다 | 시험 중 | 2026-11-05 | 이슈 #1, docs/request/GameData_ScriptableObject_Editor_Request.md (#19) |
| 에디터 작업은 전부 Unity CLI 로 한다 | 시험 중 | 2026-11-05 | CLAUDE.md |
| UI 는 프리팹 대신 코드로 만든 uGUI, 한글 글꼴은 HTML 목록대로 OS 글꼴을 쓴다(글꼴 파일 내려받기는 사용자 허락 뒤) | 시험 중 | 2026-11-05 | 이슈 #14 |

## 열린 작업
- #14 P9 메뉴, 캠페인, 전투 UI

## 맡길 때
- 모델: 규칙 이식(P4)은 opus xhigh, 나머지 구현은 opus low (`~/.claude/rules/pstack-models.md`)
- 함정: 게임 수치는 데이터 에셋에서 고친다(docs/guides/data-editor.md). `Core/PrototypeData.cs` 는 HTML 에서 만든 생성 파일이고 HTML 비교 시험만 쓴다. HTML 이 바뀌면 `node tools/golden/prototype-golden.mjs data` 와 `csharp` 로 다시 만든다.
- 함정: 데이터를 처음 한 번 읽어 두는 static readonly 를 만들지 않는다. 데이터는 `GameData.Use` 로 바뀐다.
- 함정: `Rules`, `Role`, `Side` 는 Core 타입 이름이다. Game 쪽에 같은 이름의 메서드를 만들면 CS0119 가 난다.
- 함정: 에디터가 뒤에 있을 때 `wasPressedThisFrame` 은 에디터 입력 갱신에 속할 수 있어 놓친다. `BoardInput` 은 `isPressed` 의 바뀜으로 누름·뗌을 잡는다.
- 함정: `VampireSpawn.Core` 는 `noEngineReferences` 다. 규칙 코드에 `UnityEngine` 을 쓰면 컴파일이 깨진다. 화면에 필요한 것은 `Game` 쪽에 둔다.
