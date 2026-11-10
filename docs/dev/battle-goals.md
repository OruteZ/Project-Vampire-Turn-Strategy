# 전투 목표 구조

전투를 무엇으로 이기고 지는지(목표)를 스테이지 데이터 한 칸과 판정 함수 하나로 모은 구조. 지금 목표는 `Boss`(보스 처치 승리, 아군 전멸 패배) 하나다. 방어전·목표 유닛 호위의 규칙은 기획서 docs/systems/goals.md 가 정한다. 이 문서는 그 규칙을 어디에 넣는지만 정한다.

## 지금 모양

| 자리 | 하는 일 |
|---|---|
| `Core/GameDataTypes.cs` `enum Goal` | 목표 종류. 에셋에 숫자로 저장되니 끝에만 더한다 |
| `Stage.Goal`, `StageData.goal` | 스테이지마다 목표 하나. 칸이 없는 옛 에셋은 0 = `Boss` |
| `Stage.Objective` | 배치 화면 작전 요약·원정 지도 정보 창에 뜨는 목표 문장 |
| `Battle.Outcome()` | 승리 `true`, 패배 `false`, 진행 중 `null`. 승패를 정하는 곳은 여기 하나 |
| `Battle.CheckEnd()` | `Outcome()` 을 물어 끝내는 곳. 판정 시점은 아래 표 |

판정 시점. 목표가 바뀔 수 있는 상태 변화마다 한 번씩 묻는다.

| 시점 | 바뀌는 것 | 쓸 목표 |
|---|---|---|
| `Attack`, `Execute` 뒤 | 유닛이 죽음 | 보스 처치, 아군 전멸, 목표 유닛 죽음 |
| 적 행동 하나마다(`FullMoonAct`, `MeleeAct` 뒤) | 유닛이 죽음, 적이 움직임 | 지킬 칸을 적이 밟음 |
| `Move` 뒤 | 아군 위치 | 목표 유닛이 목적지에 닿음 |
| 적 턴 끝, `Turn++` 앞 | 턴 수 | N 턴 버티기 |

## 새 목표를 더할 때

1. `enum Goal` 끝에 값 하나
2. `Battle.Outcome()` 에 그 목표의 승리·패배 줄. 아군 전멸 패배를 그대로 둘지는 목표마다 정한다
3. `Stage.Objective` 에 목표 문장(Text 표 key)
4. 목표에 수치·칸·유닛이 필요하면 `Stage` 와 `StageData` 에 칸을 더한다. 지킬 칸·목적지 칸은 지도 문자(`Board.Terrain`) 로 둘지 좌표 목록으로 둘지 기획서를 보고 정한다
5. 목표 유닛이 필요하면 `Role` 에 값을 더할지, 아군에 표시를 붙일지 정한다. `Unit.Side`·적 AI 의 대상 고르기(`Battle.Enemy.cs`)·배치(`Place`·`TrimToCaps`)·인구 계산이 모두 `Role` 을 보므로 거기부터 읽는다

`ExpansionRulesTests.EveryGoalHasAnObjectiveAndIsJudged` 가 모든 `Goal` 값으로 전투를 돌린다. 2·3 을 빠뜨리면 `ArgumentOutOfRangeException` 으로 실패한다.

## 아직 보스 목표만 아는 곳

목표를 더할 때 같이 고친다.

- 결과 창 승리 문장 `over.wonLine`("N턴에 {보스}를 쓰러뜨렸다", `BattleHud`)
- 전투 시작 기록 `log.start`(보스 이름, `Battle.Start`)
- `Move` 로 전투가 끝나는 경우. 지금은 `Move` 로 끝나는 목표가 없어서 `BattleFlow` 가 이동 재생 뒤 결과 창을 여는지 확인하지 않았다
- Text 표의 `camp.objectiveBoss` 는 이제 쓰지 않는다(`Stage.Objective` 가 `battle.defeatBoss` 를 쓴다). 표 항목은 에디터에서 지운다
