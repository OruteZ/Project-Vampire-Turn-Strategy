# 전투 목표 구조

전투를 무엇으로 이기고 지는지(목표)를 스테이지 데이터 한 칸과 판정 함수 하나로 모은 구조. 목표는 `Boss`(보스 처치), `Defense`(방어전), `Escort`(호위). 규칙은 기획서 docs/systems/goals.md 가 정한다. 이 문서는 그 규칙을 어디에 넣는지만 정한다.

## 지금 모양

| 자리 | 하는 일 |
|---|---|
| `Core/GameDataTypes.cs` `enum Goal` | 목표 종류. 에셋에 숫자로 저장되니 끝에만 더한다 |
| `Stage.Goal`, `StageData.goal` | 스테이지마다 목표 하나. 칸이 없는 옛 에셋은 0 = `Boss` |
| `Stage.Spots`·`Hold`·`Escort` (`StageData.spots`·`hold`·`escort*`) | 지킬 칸 또는 목적지 칸(좌표 목록), 버틸 턴, 목표 유닛 수치·시작 칸 |
| `Role.Escort`, `Battle.Escort` | 목표 유닛. `Side.Ally` 라 플레이어가 움직이고 적이 노린다. `Battle.Allies`(싸우는 아군, 인구·배치·전멸 판정)에는 없다. 적 AI 의 표적 목록은 `Targets` |
| `Stage.Objective`, `Stage.AlsoWins`, `Kit.Objective` | 배치 화면 작전 요약·원정 지도 정보 창의 목표 문장과 "보스를 쓰러뜨려도 승리" 줄 |
| `Battle.Outcome()` | 끝난 까닭(`Ending`) 또는 진행 중 `null`. 승패를 정하는 곳은 여기 하나. `Battle.EndLine` 이 결과 창 문장 |
| `Battle.CheckEnd()` | `Outcome()` 을 물어 끝내는 곳. 판정 시점은 아래 표. 적 턴 끝에만 `dawn` 으로 버티기 승리를 본다 |

판정 시점. 목표가 바뀔 수 있는 상태 변화마다 한 번씩 묻는다.

| 시점 | 바뀌는 것 | 쓸 목표 |
|---|---|---|
| `Attack`, `Execute` 뒤 | 유닛이 죽음 | 보스 처치, 아군 전멸, 목표 유닛 죽음 |
| 적 행동 하나마다(`FullMoonAct`, `MeleeAct` 뒤) | 유닛이 죽음, 적이 움직임 | 지킬 칸을 적이 밟음 |
| `Move` 뒤 | 아군 위치 | 목표 유닛이 목적지에 닿음 |
| 적 턴 끝, `Turn++` 앞 | 턴 수 | N 턴 버티기 |

## 새 목표를 더할 때

1. `enum Goal` 끝에 값 하나
2. `Battle.Outcome()` 에 그 목표의 승리·패배 줄, 끝난 까닭은 `Ending` 끝에 더하고 `EndLine` 에 결과 문장
3. `Stage.Objective` 에 목표 문장(Text 표 key)
4. 목표에 수치·칸이 더 필요하면 `Stage` 와 `StageData` 에 칸을 더한다. 지킬 칸·목적지 칸은 `Stage.Spots`(좌표 목록)를 같이 쓴다
5. 목표 유닛이 필요하면 `Role.Escort` 를 쓴다. 다른 종류가 필요하면 `Unit.Side`·적 AI 의 `Targets`·배치(`Place`·`Recall`·`Relocate`)·인구 계산이 `Role` 을 어떻게 보는지부터 읽는다

`ExpansionRulesTests.EveryGoalHasAnObjectiveAndIsJudged` 가 모든 `Goal` 값으로 전투를 돌린다. 2·3 을 빠뜨리면 `ArgumentOutOfRangeException` 으로 실패한다.

## 함정

- `Battle.Allies` 는 싸우는 아군만이다. 목표 유닛까지 셀 곳(적의 표적, 치료 대상, 아군 턴의 선택)은 `Side.Ally` 로 본다
- Text 표의 `camp.objectiveBoss` 는 쓰지 않는다(`Stage.Objective` 가 `battle.defeatBoss` 를 쓴다). 표 항목은 에디터에서 지운다
