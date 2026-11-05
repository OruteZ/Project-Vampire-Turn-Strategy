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

## 캠페인 진행과 저장 (P5)

- `Campaign` 은 금화, 클리어한 장, 추가 목표를 받은 장, 산 강화를 순서대로 가진다. `StageOpen`, `UpgradesOpen`, `UnlockedTier`, `SpentGold`, `Buy`, `Respec`, `Finish` 가 HTML `stageOpen`, `upgradesOpen`, `unlockedTier`, `spentGold`, 병영 클릭 처리기, `finish` 와 같다. 추가 목표 보상은 처음 달성할 때 한 번만 준다.
- `Campaign.NewBattle(stage)` 가 전투를 만든다. 데모에는 강화를 넣지 않는다 (HTML `M==='camp'`).
- 저장 문자열은 HTML `JSON.stringify(C)` 와 글자까지 같다 (`ToJson`). 불러오기는 HTML `loadCamp` 처럼 숫자 `gold` 가 없으면 빈 캠페인으로 시작한다. `Game/CampaignStore` 가 `PlayerPrefs` 키 `pvs-campaign-v2` 에 쓴다. 브라우저 `localStorage` 저장은 옮겨 오지 않는다.
- 추가 목표 판정은 `Bonus.IsMet(턴, 흡혈 횟수, 스켈레톤 남음)` 이고 `Battle.BonusMet` 이 부른다.
- `node tools/golden/prototype-golden.mjs campaign` 이 `tools/golden/out/campaign.json` 을 만들고 `CampaignParityTests` 가 비교한다.

## 육각 보드와 지형 화면 (P6)

- `Game/BoardView` 가 `Board` 에서 칸마다 육각 기둥 메시를 만들고 지형 재질과 소품을 붙인다. 칸 위치는 HTML `px` 와 같은 배치(flat-top, 홀수 열이 반 칸 아래)이고, 0행이 카메라에서 멀다. `HexToWorld` 와 `TileTop` 을 유닛 화면이 쓴다.
- 무엇을 그릴지는 `ArtCatalog` 하나가 정한다. 경로 표는 `docs/areas/art-assets.md`.
- `Game/GameBootstrap` 은 메뉴가 생길 때까지 쓰는 시험용 진입점이다. `stageId` 의 보드를 띄운다. P9 에서 지운다.
- 스크린샷은 `docs/review/p6-<장>.png`.

## 유닛 모델과 전투 연출 (P7)

- `Game/BattleView` 가 `Battle` 하나를 화면에 붙인다. `Begin(battle)` 이 보드와 유닛을 그리고, `Run(b => b.명령())` 이 명령을 실행한 뒤 결과 이벤트를 차례로 재생한다. 기다리는 시간은 HTML `wait` 값 그대로이고 `speed` 로 빠르게 돌릴 수 있다. `Mismatches()` 는 재생이 끝난 뒤 화면(위치, 체력 막대, 눕기, 시신)과 규칙 상태의 차이를 돌려준다.
- `Game/UnitView` 는 모델 하나와 발밑 고리(아군은 병종 등급 색, 스켈레톤 뼈색, 뱀파이어 붉은색, 스폰 보라), 체력 막대, 애니메이터(`Art/Unit.controller`)를 가진다. 잠든 뱀파이어와 시신은 눕힌다.
- `Game/Popup` 은 피해·흡혈 숫자를 띄운다. 기본 레거시 폰트라 한글은 OS 폰트로 대신 그려진다. 한글 폰트는 P9 에서 들인다.
- 모델 표는 `docs/areas/art-assets.md`. 스크린샷은 `docs/review/p7-*.png`.
- `Tests/PlayMode/BattlePlaybackTests` 가 골든 기록 넷을 화면으로 재생하며 명령마다 `Mismatches()` 가 비었는지 본다.

## 칸 선택, 배치, 범위 표시 (P8)

- `Core/BattleSession` 이 HTML 의 클릭 상태(`S.selected`, `S.inspect`, `S.mode`, `S.deployType`, `S.showThreat`)와 `onHex`, `renderOverlay`, `threatHexes`, `hintText` 를 옮긴 것이다. `Click(칸)` 이 배치·선택·이동·공격·처치를 HTML 과 같은 순서로 고르고, `Overlay()` 가 칸마다 표시 종류(`Mark`)를, `Moon()` 이 보름달 표적 선과 글을, `Hint` 가 아래 안내 문장을 준다. 배치 거절·회수 안내는 `Flash` 로 잠깐 바뀐다(HTML `flashHint`, 1.8초).
- `Game/BoardInput` 이 Input System 마우스를 보드 평면에 쏘아 칸을 고른다. HTML `hexAt` 처럼 가장 가까운 칸의 중심이 한 칸 반지름 안일 때만 칸으로 친다. 배치 단계에서 아군을 누르고 6px 넘게 끌면 끌기가 되고, 놓은 칸에 따라 옮기기·바꾸기·회수가 된다. `Escape` 는 선택을 푼다. 재생 중(`BattleView.IsBusy`)에는 클릭을 받지 않는다(HTML `S.busy`).
- `Game/OverlayView` 는 표시를 메시 하나(정점 색, `Art/Shaders/Overlay.shader`)로 그린다. 색·투명도·테두리 두께·반지름은 HTML CSS `.ov-*` 와 `renderOverlay` 의 반지름을 SVG 단위(R=30) 그대로 옮겼고 점선도 같은 간격이다. 보름달 표적 글은 `TextMesh`.
- 행동을 마친 아군은 발밑 고리가 어두워진다(HTML `.tk.done`).
- `node tools/golden/prototype-golden.mjs overlay` 가 `tools/golden/out/overlay.json` 을 만든다 (`tools/golden/overlay.mjs`). 13개 판(11개 장, 강화판 4·9장)에서 실제 `onHex` 로 배치·선택·이동·공격·처치 모드·위협 범위·Escape·턴 종료를 누르고, 누를 때마다 `#lOv` 의 칸과 글, 안내 문장, 선택 상태를 적는다. `OverlayTests` 가 같은 순서로 `BattleSession` 을 눌러 매번 비교한다.
- 아직 없는 것: 토큰의 `일어나는 중`·`관 속` 글, 끌기 중 유닛을 들어 올리는 그림자. 버튼(전투 개시, 턴 종료, 처치, 대기, 위협 범위)은 P9 UI 가 `BoardInput.Run`·`Act` 로 부른다.

## 메뉴, 캠페인, 전투 UI (P9)

- `Game/Screens/GameFlow` 가 HTML 의 화면 전환(`show`, `renderMenu`, `renderCamp`), 전투 버튼 처리기, `finish` 를 옮긴 것이다. 씬 `Main` 의 `Game` 오브젝트에 붙고, 재생하면 메뉴부터 뜬다. 시험용 진입점 `GameBootstrap` 은 지웠다.
- 화면은 프리팹 없이 코드로 만든 uGUI 다. HTML 이 `innerHTML` 을 다시 쓰듯 상태가 바뀔 때마다 `MenuScreen`, `CampaignScreen`, `BattleScreen` 이 그 부분을 다시 만든다. 색은 HTML CSS 변수 그대로(`Ui`). 문장은 HTML 템플릿과 같은 글자다.
- 진행 초기화·강화 초기화는 두 번 눌러야 하고, 전투 중 나가기는 2.5초 안에 한 번 더 눌러야 한다. 결과 창의 같은 편성으로 다시·편성부터 다시는 `Battle.StartFormation` 을 쓴다. 캠페인 승리 보상은 `OverInfo.From` 이 `Campaign.Finish` 로 계산하고 `CampaignStore` 에 저장한다.
- 3D 카메라는 보드 칸 영역(`BoardArea`)에만 그리고, 화면 전체는 배경 카메라가 지운다. 배경 카메라가 없으면 보드 밖 글자가 지워지지 않은 화면 위에 섞여 깨진다.
- 한글 글꼴은 HTML 의 글꼴 목록 그대로 OS 글꼴에서 고른다(본문 IBM Plex Sans KR → Apple SD Gothic Neo → 맑은 고딕, 제목 Nanum Myeongjo → Noto Serif KR → 바탕). 파일을 저장소에 넣지 않았다. Windows 에서는 글꼴을 따로 깔지 않으면 맑은 고딕·바탕으로 그려진다.
- `node tools/golden/prototype-golden.mjs strings` 가 `tools/golden/out/strings.json` 을 만든다 (`tools/golden/strings.mjs`). 메뉴 둘, 캠페인 셋, 데모 전투 19개 상태(배치, 선택, 처치 모드, 적·뱀파이어·시신·스폰·보름달 표적 보기, 턴마다), 결과 창 여섯(데모 승·패, 1장 첫 클리어·재도전·패배, 2장 첫 클리어)과 그 뒤 캠페인 화면을 HTML 로 그려 글 덩어리를 뽑는다. `UiTextParityTests` 가 같은 상태의 Unity 화면 글(`Text`)과 덩어리 단위로 비교하고, 결과 뒤 캠페인 저장 문자열도 비교한다.
- HTML 과 다른 점: 적 턴 동안 전투 기록과 수치는 재생이 끝난 뒤 한 번에 바뀐다(HTML 은 적 하나가 움직일 때마다 바뀐다). 달 수치 옆 달 그림, 스크롤 막대 모양, 마우스를 올렸을 때의 테두리 색은 옮기지 않았다.
