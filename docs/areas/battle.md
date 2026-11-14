# 전투

## 경로
- 기획 방향: `docs/design/` (combat, vampire, units, map, campaign, progression 등). 기획서: `docs/systems/`, `docs/content/`, `docs/ui/`
- 구현 기록: `docs/dev/unity-port.md`, 전투 목표(승패) 구조 `docs/dev/battle-goals.md`
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
| 전투 카메라 연출 B 등급은 5개(흔들림·멈춤, 중요한 처치 줌, 시작 훑기, 승패 마무리, 아군 처치)만, 턴 전환 줌은 뺀다. 흔들림·멈춤은 피해 4 이상 큰 공격과 흡혈에만, 흔들림 옵션은 설정 화면이 생기면 | 시험 중 | 2026-11-09 | #97 "5개로 진행", "기본 공격에서는 흔들림이랑 히트스탑 제외" |
| 전투 카메라는 플레이어가 휠(커서 쪽 확대)·Q/E(회전)·WASD(이동)·R(맞춤)로 옮긴다. 연출은 그 시점 위에 더한다 (docs/art/battle-camera.md 11) | 시험 중 | 2026-11-11 | #117 "카메라 확대 및 회전기능을 불가피하게 추가해야 할거 같아. 맵이 커지고 캐릭터가 너무 작아져서" |
| 전투 재생 대기는 옛 HTML 데모의 2배(`BattleView.Pace`). 배속은 설정 화면이 생기면. 붉은 비네트는 뱀파이어 흡혈 모션에만 강하게 | 시험 중 | 2026-11-09 | #97 "모든 유닛의 행동 속도를 전반적으로 늦출 필요", "비네트는 흡혈하는 모션동안만" |
| UI 는 UI Toolkit(UXML·USS, docs/areas/ui.md #106), 한글 글꼴은 HTML 목록대로 OS 글꼴을 쓴다(글꼴 파일 내려받기는 사용자 허락 뒤) | 시험 중 | 2026-11-05 | 이슈 #14 |
| 병종 강화는 세 갈래 중 하나만 산다(나머지 둘은 잠김, 되돌릴 수 없음). 1번 = 스폰이 물려받는 능력치, 2·3번 = 물려받지 않는 것 (docs/systems/progression.md) | 시험 중 | 2026-11-09 | #99 "셋 중 하나 선택" |
| 스테이지는 3막 18장(2막에 갈래 하나), 1막은 8→12분으로 오르고 2막부터 한 장 15분 대형. 새 규칙 트리거·적 성향·문 열림·시작 시신·횃불 켜기·지킬 곳 둘을 넣고, 보스 단계는 정하지 않음. 수직 단면은 6장 장원·9장 은광 (계획서 https://claude.ai/artifact/PZ4o7U466D1E5fVzE7sJmT) | 시험 중 | 2026-11-10 | #113 "추천안대로 진행" |
| 적이 누구를 노릴지(표적 점수)는 화면에 보여 주지 않는다. 스테이지는 그 계산 없이도 풀리게 짠다(한 방에 죽는 유닛이 여럿 닿는 자리를 정답으로 요구하지 않기) | 시험 중 | 2026-11-11 | #113 "안 보여 줌(지금대로)" |
| 모든 장에 뱀파이어(변이)가 있어야 한다는 전제를 없앤다. 장은 제 목표로만 끝난다: 보스 처치 장만 뱀파이어 처치로 승리, 방어전·호위는 뱀파이어가 없어도 되고 있어도 잡는 것으로 끝나지 않는다 (docs/systems/goals.md 1-2·1-7). docs/design/combat.md "기본 조건: 보스 처치로 승리"·vampire.md 와 닿으니 docs/design 으로 옮길 후보 | 시험 중 | 2026-11-11 | #113 "스테이지 본연의 목표만 확실하게" |
| 아군 처치를 공격과 같은 피해로 바꾼다(즉사 아님, 공격 행동 소모, 죽으면 지금처럼 시신 없음). 화면 이름은 "아군 공격" (docs/systems/spawn.md 2) | 시험 중 | 2026-11-12 | #119 리뷰 "아군 처치가 즉사가 아닌 공격으로", "변이 방지가 너무 쉬워지지 않도록" |
| 기본 이동력을 1 로 낮춘다(아군 기본 병종·하수인 1, 기사·사냥꾼·웨어울프 2, 보스는 장마다). 첫 한 걸음은 거친 땅도 늘 갈 수 있다. 맵은 Gloomhaven 처럼 방·좁은 골목으로 촘촘하게 다시 짜고 1장부터 확인받는다 (docs/content/stages.md 1장, docs/systems/hex-grid.md 1) | 시험 중 | 2026-11-13 | #113 "가장 기본적인 유닛의 이동력을 1로 제한하고 (적, 아군 무관) 이에 기반하여 맵을 더 밀도 있게", docs/design/map.md |
| 뱀파이어는 보름달(달 6)이 되어야 깨어난다(`moonWake` 3 → 6). 깨면 경계 없이 바로 아군 쪽으로 온다(1장) | 시험 중 | 2026-11-13 | #113 "보름달이 꽉 찰 때가 되어야 뱀파이어가 나오면", "나오면 바로 팀쪽으로 달려오면" |
| 2~10장을 1장처럼 9×9~13×12 의 방·좁은 길목(짝수 열 틈 = 아래 한 칸만 닿음, 홀수 열 틈 = 아래 세 칸이 함께 침)으로 다시 짠다. 6장·9장 대형안은 버리고, 보스는 모두 이동 2·장마다 체력·공격을 낮춤 (docs/content/stages.md 3절) | 시험 중 | 2026-11-13 | #113 "전부 1장식으로 좁고 컴팩트하게 다시 작성해줘" |
| 광원(화톳불·햇빛)과 빛 받는 칸은 격자에 칠하지 않는다. 광원은 늘 같은 오브제와 빛 VFX(불티, 빛줄기)로만 알린다 (docs/art/battle-terrain.md 3-1) | 시험 중 | 2026-11-12 | #101 "광원 수준의 정보는 오브제 존재만으로 괜찮은거같아" |

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
- 함정: 승패는 `Battle.Outcome()` 한 곳에서만 정한다. `Vampire == null` 을 "전투가 끝났다" 로 쓰지 않는다(목표가 보스가 아닐 수 있다). 새 목표는 docs/dev/battle-goals.md 순서대로
- 함정: 전투 카메라의 멈춤·느린 화면은 `Time.timeScale` 을 쓴다(`BattleCamera`). 재생 대기를 새로 만들 때 `WaitForSeconds`(게임 시간)를 써야 멈춤에 같이 멈춘다. 카메라 쪽 타이머는 실제 시간(`unscaledTime`)이다.
