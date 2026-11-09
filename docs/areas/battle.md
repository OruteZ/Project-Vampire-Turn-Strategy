# 전투

## 경로
- 기획 방향: `docs/design/` (combat, vampire, units, map, campaign, progression 등). 기획서: `docs/systems/`, `docs/content/`, `docs/ui/`
- 계획: `docs/dev/unity-port-plan.md`
- 데이터: `Assets/_Project/Data/` (편집기 사용법 `docs/guides/data-editor.md`)
- 코드: `Assets/_Project/Scripts/Core/` (규칙, Unity 참조 없음), `Assets/_Project/Scripts/Game/` (화면·입력)
- 테스트: `Assets/_Project/Tests/EditMode/`
- 씬: `Assets/_Project/Scenes/Battle.unity` (컨트롤러 `BattleFlow`). 메뉴·원정은 `Menu.unity`(Build 0)·`Camp.unity`, 씬을 넘는 상태는 `App.cs` (docs/dev/scene-split.md)
- 검증: `.claude/skills/verify-unity/`

## 결정
| 결정 | 상태 | 날짜 | 출처 |
|---|---|---|---|
| 게임 수치·스테이지는 데이터 에셋(Assets/_Project/Data)에 두고 기획자용 편집기로 고친다. 에셋은 데이터 기획서(docs/content)를 구현한 것이고, HTML 데모와 그 비교 시험은 #99 에서 지웠다 | 시험 중 | 2026-11-05 | 이슈 #1, 요청 #19, #99 "HTML프로토타입은 이제 완전히 삭제" |
| 병종·하수인은 코드 enum 이 아니라 데이터 key 로 찾는다. 새 병종·하수인은 에셋 복제 + UnitLook 으로 더한다(docs/guides/data-editor.md "새 병종·하수인 더하기") | 시험 중 | 2026-11-09 | #99 "수정이 용이한 방향으로" |
| 에디터 작업은 전부 Unity CLI 로 한다 | 시험 중 | 2026-11-05 | CLAUDE.md |
| 전투 카메라 연출은 후보 A 등급(변이·연쇄 변이·적 턴 따라가기·보름달)만 먼저 넣는다. 기본 시점에서 평행 이동·당기기만 한다 (docs/art/battle-camera.md, 코드 `Game/BattleCamera.cs`) | 시험 중 | 2026-11-09 | #97 "A등급으로 시작" |
| 전투 카메라 연출 B 등급은 5개(흔들림·멈춤, 중요한 처치 줌, 시작 훑기, 승패 마무리, 아군 처치)만, 턴 전환 줌은 뺀다. 흔들림은 끌 옵션이 생길 때까지 약하게 | 시험 중 | 2026-11-09 | #97 "5개로 진행" |
| 전투 재생 대기는 옛 HTML 데모의 2배(`BattleView.Pace`). 배속은 설정 화면이 생기면. 붉은 비네트는 뱀파이어 흡혈 모션에만 강하게 | 시험 중 | 2026-11-09 | #97 "모든 유닛의 행동 속도를 전반적으로 늦출 필요", "비네트는 흡혈하는 모션동안만" |
| UI 는 프리팹 대신 코드로 만든 uGUI, 한글 글꼴은 HTML 목록대로 OS 글꼴을 쓴다(글꼴 파일 내려받기는 사용자 허락 뒤) | 시험 중 | 2026-11-05 | 이슈 #14 |
| 병종 강화는 세 갈래 중 하나만 산다(나머지 둘은 잠김, 되돌릴 수 없음). 1번 = 스폰이 물려받는 능력치, 2·3번 = 물려받지 않는 것 (docs/systems/progression.md) | 시험 중 | 2026-11-09 | #99 "셋 중 하나 선택" |

## 맡길 때
- 모델: 규칙 이식(P4)은 opus xhigh, 나머지 구현은 opus low (`~/.claude/rules/pstack-models.md`)
- 함정: 게임 수치는 데이터 에셋에서 고친다(docs/guides/data-editor.md). Core 는 에셋을 못 읽어서 `GameData.Loader` 로 받는다: 에디터는 `Editor/DataAssetsBuilder` 가, 재생은 `App` 이 넣는다. 시험은 출하 데이터로 돈다.
- 함정: 병종·하수인을 이름 key(`"knight"`)로 코드에서 가르지 않는다. 행동 차이는 `Ability`·능력치로 낸다. `Ability` 는 에셋에 숫자로 저장되니 끝에만 더한다.
- 함정: 에디터에서 Text 표 항목을 코드로 더하면 Localization 이 Addressables 에 한국어 이름 그룹(`Localization-String-Tables-한국어`)을 새로 만든다. 그 그룹과 `AddressableAssetSettings.asset` 변경은 되돌린다(#99).
- 함정: 데이터를 처음 한 번 읽어 두는 static readonly 를 만들지 않는다. 데이터는 `GameData.Use` 로 바뀐다.
- 함정: `Rules`, `Role`, `Side` 는 Core 타입 이름이다. Game 쪽에 같은 이름의 메서드를 만들면 CS0119 가 난다.
- 함정: 에디터가 뒤에 있을 때 `wasPressedThisFrame` 은 에디터 입력 갱신에 속할 수 있어 놓친다. `BoardInput` 은 `isPressed` 의 바뀜으로 누름·뗌을 잡는다.
- 함정: `VampireSpawn.Core` 는 `noEngineReferences` 다. 규칙 코드에 `UnityEngine` 을 쓰면 컴파일이 깨진다. 화면에 필요한 것은 `Game` 쪽에 둔다.
- 함정: MonoBehaviour 에 `Reset()` 이름의 메서드를 만들지 않는다. 에디터에서 `AddComponent` 때 Unity 가 불러 필드가 비어 있는 채 실행된다 (`BattleCamera.Snap`).
- 함정: 전투 카메라의 멈춤·느린 화면은 `Time.timeScale` 을 쓴다(`BattleCamera`). 재생 대기를 새로 만들 때 `WaitForSeconds`(게임 시간)를 써야 멈춤에 같이 멈춘다. 카메라 쪽 타이머는 실제 시간(`unscaledTime`)이다.
