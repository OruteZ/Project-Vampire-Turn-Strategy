# Unity 포팅 구현

출처 docs/Prototype.html (커밋 8205db3)

계획은 `docs/systems/unity-port-plan.md`, 분야 안내서는 `docs/areas/battle.md` 다.

## HTML 과 같은지 확인하는 방법

`tools/golden/prototype-golden.mjs` 가 `docs/Prototype.html` 의 `<script>` 를 node `vm` 에서 화면 없이 그대로 돌린다. 여기서 뽑은 값이 정답(골든)이다. C# 테스트는 이 정답과 한 칸씩 비교한다.

| 명령 | 만드는 것 |
|---|---|
| `node tools/golden/prototype-golden.mjs data` | `tools/golden/out/data.json` (병종, 강화, 스켈레톤, 스테이지, 상수, 추가 목표 문장) |
| `node tools/golden/prototype-golden.mjs csharp` | `Assets/_Project/Scripts/Core/GameData.cs` (data.json 에서 생성) |

HTML 이 바뀌면 두 명령을 차례로 돌리고, `data.json` 과 `GameData.cs` 의 diff 를 같이 커밋한다.

## 게임 데이터 (P2)

- `GameDataTypes.cs` 는 손으로 쓴 타입이다. `UnitType`, `Upgrade`(`StatMod`, `Ability`), `EnemyStats`, `Boss`, `Bonus`(추가 목표 문장 `Text`), `Placement<T>`, `Stage`.
- `GameData.cs` 는 생성 파일이다. 손으로 고치지 않는다. `GameData.Types` 순서가 `UnitKind` 값과 같다.
- `Rules.cs` 는 HTML 상수 넷(`DRAIN_HEAL`, `MOON_MAX`, `EAT_RANGE`, `MOON_WAKE`)이다.
- 데모의 `reward` 는 HTML 에 없다. C# 에서는 0 이다. 데모 전투는 보상을 주지 않으므로 쓰이지 않는다.
- 지도는 문자열 줄 그대로 둔다. 지형 종류로 읽는 것은 육각 보드를 만드는 P3 에서 한다.
- `GameDataParityTests` 가 `data.json` 을 `JsonUtility` 로 따로 읽어 생성된 값과 비교한다. 생성기에 버그가 있으면 이 테스트가 잡는다.

## 육각 좌표와 이동 (P3)

- `Hex` 는 (열, 행) 값 타입이다. flat-top, odd-q 이웃 순서를 HTML `NB` 표와 똑같이 둔다. 도달 칸 목록의 순서가 이 순서로 정해지고, 적 AI 가 같은 점수일 때 그 순서로 고르기 때문이다.
- `Board` 는 스테이지 지도에서 만든다. `Walkable`, `Lit`, `Neighbors`, `Blocked`, `Reachable`, `PathTo`, `DistField` 가 HTML 의 같은 이름 함수와 같다.
- 유닛과 시신 위치는 `Board` 가 들고 있지 않는다. 호출하는 쪽이 칸마다 `Occupant`(빈 칸, 아군, 적, 시신)를 돌려주는 함수를 넘긴다. P4 의 전투 상태가 이 함수를 준다.
- `Reachable` 은 HTML `Map` 처럼 BFS 순서대로 (칸, 걸음) 목록을 돌려준다. 같은 편 유닛이 선 칸은 지나갈 수 있지만 목록에는 없다.
- `PathTo` 는 갈 수 없는 목표면 목표 한 칸만 돌려준다. HTML 이 그렇게 한다.
- `node tools/golden/prototype-golden.mjs hex` 가 `tools/golden/out/hex.json` 을 만든다. 11개 스테이지마다 칸별 이웃·빛·걷기·거리, 그리고 시작 배치와 첫 아군을 시신으로 바꾼 배치에서 유닛별 도달 칸·경로(칸 셋 중 하나를 목표로)·양쪽 거리장을 적는다. 칸은 `"열,행"`, 걸음이 붙으면 `"열,행:걸음"` 문자열이다 (`JsonUtility` 가 중첩 배열을 못 읽어서).
- `HexParityTests` 34개가 이것을 비교한다.

## 전투 규칙과 적 AI (P4)

- `Battle` 은 단계 상태 기계다 (`Phase` 배치·아군·적·종료). 명령은 배치 단계의 `Place`, `Recall`, `Relocate`, `ClearAllies`, `UseSample`, `Start` 와 아군 단계의 `Move`, `Attack`, `Execute`, `Wait`, `EndTurn` 이다. 단계에 맞지 않거나 HTML 화면이 허락하지 않는 명령은 `CommandResult.Ok == false` 와 이유를 돌려주고 상태를 바꾸지 않는다. 인구·은 부족 이유는 HTML 안내 문장 그대로다.
- 규칙은 화면을 모른다. 명령마다 `BattleEvent` 목록을 돌려준다. 기록, 배너, 걸음 경로, 팝업, 피격, 안내(`hint`)는 HTML 의 `log`, `showBanner`, `walk`, `popup`, `hitFx`, `flashHint` 호출과 같은 순서이고 골든과 비교한다 (`InPrototype`). 사망(`Died`), 시신(`CorpseLeft`), 기상(`Rose`), 관 열림(`Woke`), 체력 변화(`HpChanged`)는 HTML 이 `render()` 로 보여 주던 순간을 화면에 알리려고 더한 것이라 HTML 짝이 없고 비교에서 뺀다. 화면(P7)은 이 목록을 차례로 재생하고, 기다리는 시간은 화면이 정한다.
- 유닛 종류는 `Role`(아군, 스켈레톤, 뱀파이어, 스폰)이다. `Type` 은 아군의 병종, 스폰이면 원래 병종이다. 전투 상태는 밖에서 읽기만 할 수 있다 (`IReadOnlyList`, `internal set`). `Start` 때 편성을 `StartFormation` 에 남긴다 (HTML `lastFormation`, 같은 편성으로 다시). `MoonNext()` 와 `BonusMet` 은 화면과 캠페인이 쓰도록 공개한다.
- `EndTurn` 은 적 페이즈 전체를 한 번에 계산한다. HTML 은 중간에 기다리지만 그동안 입력을 받지 않으므로 결과가 같다.
- 적 AI(`Battle.Enemy.cs`)는 HTML `meleeAct`, `targetScore`, `pickTarget`, `fullMoonAct`, `pickEat`, `fixEatTarget` 을 그대로 옮겼다. 표적 점수는 체력에 0.5 를 곱해서 실수다. 같은 점수면 목록에서 먼저 나온 쪽, 도달 칸은 BFS 순서를 따른다.
- 아이디는 유닛과 시신이 한 카운터를 함께 쓴다 (HTML `uid`). C# 은 전투마다 1부터 시작하고, HTML 은 페이지 전체에서 이어진다. 아이디는 화면에 나오지 않고 순서만 의미가 있어 차이가 없다. 골든은 전투마다 HTML `uid` 를 1로 돌려 맞춘다.
- HTML 동작을 하나 그대로 둔다. 적이 이동한 뒤 창벽에 맞아 죽어도, 그 자리에 인접한 아군을 한 번 공격한다 (HTML `meleeAct` 가 `moveTo` 뒤 생사를 보지 않음). 고칠지는 기획 결정이다.
- `node tools/golden/prototype-golden.mjs battle` 이 `tools/golden/out/battles.json` 을 만든다 (`tools/golden/battle.mjs`). 정책 넷(턴 종료만, 공격적, 보름달 대응, 강화 전부를 산 캠페인)으로 11개 장(강화판은 10개 장)을 끝까지 돌려 43개 기록, 1447개 명령을 남긴다. 이동·공격·처치는 HTML `onHex` 를, 끌어 옮기기는 `endDrag` 를 그대로 부른다. 대기·회수·모두 회수·예시 편성은 HTML 옆 패널 처리기가 한 줄이라 하네스가 같은 줄을 쓴다. 배치 단계에서 바꾸기, 빈 칸으로 옮기기, 구역 밖으로 빼기, 모두 회수, 예시 편성을 모두 지난다. 명령마다 상태 문자열(`Battle.Snapshot()` 과 같은 형식, 승패·글자·원래 이름 포함)과 이벤트 열을 적는다.
- `BattleParityTests` 가 43개 기록을 재생하고 명령마다 상태와 이벤트를, 끝에 80줄 전투 기록을 비교한다. `BattleRulesTests` 는 기록에 없는 거절·피해 최소 1·조사를 본다.
