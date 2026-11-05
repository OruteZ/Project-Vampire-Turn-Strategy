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
