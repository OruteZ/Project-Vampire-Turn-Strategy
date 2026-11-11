# 스테이지 제작·시험 도구 (#113)

대형 스테이지 계획(https://claude.ai/artifact/PZ4o7U466D1E5fVzE7sJmT) 6절의 도구. 모두 메뉴 없이 Unity CLI `script eval` 이나 시험으로 돈다.

## 지도 검증기
- 자리: `Core/DataSet.cs` `ValidateLayout` (`Validate` 가 부른다). `GameDataAssetTests.ShippedAssetsAreComplete` 가 출하 데이터에 돌린다. 데이터 편집기도 같은 목록을 보인다.
- 보는 것: 문을 모두 연 상태에서 배치 구역과 안 이어진 칸, 배치 칸 < 인구, 예시 편성 인구 > 장 인구, 트리거 id 겹침·칸·값, 문 칸이 `#` 인지, 증원이 없는 트리거를 가리키는지, 증원 턴과 트리거를 같이 쓰는지, 경계의 깨는 거리.
- 손으로 돌리기: `unity command --project-path <트리> script eval --code 'return string.Join("\n", VampireSpawn.Core.GameData.Current.Validate());'`

## 봇 시뮬레이터
- 자리: `Core/StageSim.cs`. 예시 편성과 단순 봇(닿으면 체력 낮은 적부터 치고, 아니면 가장 가까운 깬 적에게 다가감, 방어전은 지킬 칸 근처에 머묾, 길잡이는 가까운 목적지로)으로 끝까지 둔다. 무작위가 없어 장마다 한 번이면 된다.
- `script eval --code 'return VampireSpawn.Core.StageSim.Report();'` → 장마다 끝(Slain·Held·Arrived·Wiped·Overrun·EscortFell·unfinished), 턴, 손실, 변이, 하수인 처치, 최대 적 수.
- 봇은 사람보다 약하다. 너무 빨리 끝남(봇이 3턴 안에 이김), 끝나지 않음, 판이 굳음만 거른다. 재미·난이도 판단은 사람 몫.
- `WavesTerrainTests.EveryShippedStageEndsUnderTheBot` 가 모든 장이 40턴 안에 끝나는지 본다.
- 2026-11-10 중형 10장: 봇은 5~9턴에 대부분 짐(s3 만 이김, s6 은 3턴에 지킬 칸을 내줌). 장 길이가 15분 목표의 절반 아래라는 계획서 진단과 같다.

## 플레이 기록
- 자리: `Game/PlayLog.cs`, `BattleFlow` 가 전투마다 만든다. 끝나면 `Application.persistentDataPath/playlogs/<장>-<시각>.json` 에 쓰고 콘솔에 경로를 찍는다(Windows: `%USERPROFILE%\AppData\LocalLow\<회사>\<제품>\playlogs`).
- 담는 것: 전체 실제 시간, 배치 시간, 턴마다 아군 생각 시간·적 차례 재생 시간·누적 손실·변이·달, 결과.
- 사용자 플레이 뒤 이 파일과 질문지(계획서 6절)를 주제 이슈 댓글에 모은다.

## 배속
- F 로 적 차례 재생 1·2·4배(`BattleView.speed`, PlayerPrefs `battle.speed`). docs/ui/battle-hud.md 4-5.

## 텍스트 플레이 (에이전트가 직접 두기)
- 자리: `Core/StageConsole.cs`. 판을 글자로 보고 명령으로 한 턴씩 둔다. 상태는 에디터에 남는다(도메인 다시 읽기 전까지).
- `script eval --code 'return VampireSpawn.Core.StageConsole.New("s6", "sword@6,4 doctor@9,6");'` (편성을 안 주면 예시 편성), `Do("go; m 13 6,3; a 13 2; h 16 14; b 15 21; end")` (m 이동, a 공격, x 처형, b 수습, h 치료, w 대기, p 놓기, r 회수, go 전투 개시·증원 배치 완료, end 턴 종료), `State()`, `Map()`, `Reach(id)`, `Threat()`(다음 적 차례에 각 칸을 칠 수 있는 적 수).
- 에이전트는 이것으로 "고민이 생기는가, 할 일 없는 턴이 있는가, 길이·난이도"를 본다. 연출·손맛·실제 걸리는 시간은 사람 몫(플레이 기록).
- 2026-11-11 6장·9장 1판을 이렇게 둬서 고쳤다(docs/content/stages.md 6-3).
