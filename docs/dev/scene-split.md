# 화면별 씬 분리와 원정 지도 굽기 (#46)

> 사용자: "1번과 2번 전부 적용하자" — 1번: 화면을 씬으로 나눈다. 2번: 원정 지도를 사람이 Unity 에서 옮길 수 있게 굽는다.
> 구현 기록이다. 화면 요구사항은 각 기획서(docs/ui/*, docs/art/campaign-map.md)에 있다.

## 1. 씬

| 씬 | Build | 들어 있는 것 | 컨트롤러 |
|---|---|---|---|
| `Assets/_Project/Scenes/Menu.unity` | 0 (시작) | Main Camera, Directional Light, Board (흐린 바탕용), Menu | `MenuFlow` |
| `Assets/_Project/Scenes/Camp.unity` | 1 | Main Camera, Directional Light, Campaign Map View (구운 지형 `Campaign Map`), Camp | `CampFlow` |
| `Assets/_Project/Scenes/Battle.unity` | 2 | Main Camera, Directional Light, Board, Game (BattleView·BoardInput), Overlay | `BattleFlow` |

- `Battle.unity` 는 옛 `Main.unity` 를 옮긴 것이다(GUID 그대로).
- 어느 씬에서 Play 를 눌러도 돈다. `Battle` 에서 바로 누르면 데모 1장으로 시작한다. `Camp` 는 지금 저장된 캠페인으로 L1 을 연다.

## 2. 씬을 넘는 것 (`Game/Screens/App.cs`)

- 캠페인 저장(`App.Campaign`), 메뉴에서 고른 카드, 장 선택의 마지막 상태(`App.Nav`)와 다음에 열 상태(`App.CampStart`), 시작할 전투(`BattleStage`·`Demo`·`Formation`·`StartNow`).
- 화면 전환은 `App.GoMenu()`, `App.GoCamp(nav)`, `App.GoBattle(stage, demo)` 이다. 씬이 바뀌면 앞 화면의 카메라·안개·보드·UI 가 같이 사라진다. 그래서 옛 `GameFlow.ClearScreen` 이 하던 손 정리가 없다.
- 데이터(`GameDataAsset`)는 처음 열린 씬의 컨트롤러가 `App.Ensure(data)` 로 한 번 싣는다.
- 공용: 캔버스 `App.Canvas` (Expand, ui-style.md 15-1), 키 눌림 `KeyEdges`.

## 3. 원정 지도

- `CampaignMapLayout`(C# 배치 표)을 도구 모음 > 지도 > **Camp 씬에 지도 굽기** 로 `Camp.unity` 의 `Campaign Map` 아래 실제 오브젝트로 굽는다 (`Editor/CampaignMapBaker.cs`). 재질은 `Assets/_Project/Art/Map/*.mat`.
- 구운 뒤에는 **씬이 지도다.** 소품·나무를 Unity 에서 옮기고 씬을 저장하면 그대로 남는다.
- 지역은 이름(`Region s1` … `Region s10`)으로 찾는다. 지역 오브젝트를 옮기면 표식·화로·스폿광·카메라 시점이 따라간다.
- 실행 중에 짓는 것은 진행에 따라 바뀌는 것뿐이다: 원정 길(밝은 길/어두운 길)과 클리어 화로.
- 함정: 길은 아직 배치 표의 점을 따른다. 지역을 크게 옮기면 길이 그 자리를 지나지 않는다. 그때는 `CampaignMapLayout.Road` 의 점도 옮긴다.
- 함정: 다시 굽기는 `Campaign Map` 전체를 새로 짓는다. 손으로 옮긴 것이 사라진다(버튼이 먼저 묻는다).
- 함정: 구울 때 쓰는 메시·재질은 에셋이어야 한다. 실행 중에 만든 것은 씬을 저장하면 사라진다. 지역 원판은 그래서 기본 원기둥을 납작하게 쓴다.

## 4. 검사

- PlayMode `BattlePlaybackTests` 는 `Battle` 씬을 읽는다.
- `tools/ui-audit/sweep.sh` 는 장면마다 `App.Go*` 로 그 씬을 연다.
- 인터랙션 확인은 사용자 체크리스트 `docs/checks/001_scene-split.md` (CLAUDE.md "검증 위임").
