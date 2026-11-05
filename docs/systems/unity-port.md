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
