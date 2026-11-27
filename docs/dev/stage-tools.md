# 스테이지 제작·시험 도구 (#113)

대형 스테이지 계획(https://claude.ai/artifact/PZ4o7U466D1E5fVzE7sJmT) 6절의 도구. 모두 메뉴 없이 Unity CLI `script eval` 이나 시험으로 돈다.

## 지도 검증기
- 자리: `Core/DataSet.cs` `ValidateLayout` (`Validate` 가 부른다). `GameDataAssetTests.ShippedAssetsAreComplete` 가 출하 데이터에 돌린다. 데이터 편집기도 같은 목록을 보인다.
- 보는 것: 문을 모두 연 상태에서 배치 구역과 안 이어진 칸, 배치 칸 < 인구, 예시 편성 인구 > 장 인구, 트리거 id 겹침·칸·값, 문 칸이 `#` 인지, 증원이 없는 트리거를 가리키는지, 증원 턴과 트리거를 같이 쓰는지, 경계의 깨는 거리, 묶음(`group`)에 경계가 아닌 적이 있는지.
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

## 에디터 없이 두기 (`tools/stage`, 2026-11-17)
- 규칙 코드(`Scripts/Core`)는 Unity 를 참조하지 않아서 .NET 콘솔 프로그램으로 그대로 돈다. 에디터 왕복(한 수에 3~5초) 없이 0.2초에 한 수, 여러 프로세스로 동시에 돌릴 수 있다.
- `tools/stage/export.cs`: Unity `script eval --file` 로 지금 데이터 에셋을 `tools/stage/data.json` 에 내보낸다. `tools/stage/import.py`: `data.json` 을 다시 에셋에 넣는 eval 스크립트(`scratch/import.cs`)를 만든다. 넣은 뒤 다시 내보내 같아야 한다.
- `tools/stage/sp` (처음 쓸 때 빌드):
  - `sp new <판> <장> ["sword@3,6 ..."] [강화 id,...]`, `sp play <판> "go; m 6 3,5; a 6 4; auto; end"`: 한 수씩 두기. 판은 `plays/<판>.play` 글 파일(첫 줄 장|편성|강화, 그 뒤 명령)이고 부를 때마다 처음부터 다시 재생한다. `auto` 는 아직 안 친 아군이 닿는 가장 약한 적을 친다.
  - `sp replay <판>...`: 기록을 다시 돌려 결과 한 줄(끝, 턴, 손실, 변이, 추가 목표).
  - `sp bot [장|all] [편성] [강화]`: 단순 봇(`StageSim`).
  - `sp mc <장,장|all> [편성|comp:명세] [강화]`: **한 수 앞 봇.** 턴마다 무작위 수 순서 `SP_SAMPLES`(200)가지를 적 차례까지 돌려 보고 가장 나은 턴을 고른다. `SP_SEEDS`(8)판의 승수·평균 손실을 낸다. `SP_SAVE=<머리말>` 이면 가장 손실이 적은 이긴 판을 `plays/` 에 남긴다.
  - `comp:knight:2 archer:1 pspear:* parcher:*`: 편성 만들기. 숫자는 그 수만큼, `*` 는 인구가 찰 때까지 번갈아. 근접은 적 쪽 배치 칸부터 선다.
  - `sp validate`, `sp form <장> <명세>`, `sp info <판> threat|reach <id>`.
- `tools/stage/matrix.sh <이름> <장,...>`: 편성 여섯 가지(예시, 검사 쌓기, 궁수 쌓기, 기사 쌓기, 연금술사 둘 + 섞기, 고르게 섞기)를 한꺼번에 돌려 `scratch/<이름>.tsv`. `tools/stage/doc.py <이름>` 이 stages.md 8절 표를 만든다.
- 한 수 앞 봇은 잠든 뱀파이어도 깬 것과 같은 위협으로 센다(2026-11-20). 달이 죽음으로만 차서(full-moon.md), 잠든 쪽을 0 으로 세면 마지막 하나를 안 잡고 버틴다. `builds` 줄 끝 칸은 흡혈 횟수.
- 한 수 앞 봇은 두 턴 뒤를 계획하지 못한다(미끼로 흡혈을 헛되게 하기 같은 수). 사람 대신이 아니라 장·편성을 서로 견주는 잣대다. 그래도 에이전트가 손으로 둔 것보다 잘 둔다(7장: 손 14턴, 봇 9턴).
- `tools/stage/loop.sh <이름> [20] [씨앗]`: 장마다 무작위 빌드(병종 2~4가지 + 강화 갈래)를 만들어 한 판씩(`sp builds <장> <n> <씨앗>`, `SP_FORCE=<병종>`·`SP_BAN=<병종>` 으로 넣기·빼기). `tools/stage/analyze.py <이름>...` 이 장 안에서 견준 병종·강화·조합별 승률 차를 낸다. `tools/stage/tut.sh <이름>` 은 해금 장에서 그 병종을 넣은 빌드와 뺀 빌드를 견준다. 400판 한 번의 오차는 ±0.2 쯤이라 같은 값으로 여러 번 모은다.
- `builds` 줄 끝 두 칸(2026-11-27, #150): `idle` = 아군 턴 가운데 그 턴과 뒤따르는 적 턴에 아무도 죽지 않고 아군이 공격(`a`)하지 않은 턴 수, `firstSpawn` = 첫 스폰이 일어난 턴(없으면 0). 도구가 턴마다 판을 보고 센다(규칙 코드는 그대로). `tools/stage/tension.py <이름>...` 이 장마다 승률, 스폰 판 비율, 스폰 판 / 없는 판 승률, 이긴 판 평균 턴, 대기 턴과 그 비율을 표로 내고, 둘째 표로 기사가 든 판 / 안 든 판 승률을 낸다(stages.md 10)(`tools/stage/<이름>.tsv` 를 먼저, 없으면 `scratch/`). 결과 읽는 법은 `docs/systems/spawn.md`·`full-moon.md` 의 측정 절.
