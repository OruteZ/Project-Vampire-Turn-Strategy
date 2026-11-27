# 화면별 씬 분리와 원정 지도 굽기 (#46)

> 사용자: "1번과 2번 전부 적용하자" — 1번: 화면을 씬으로 나눈다. 2번: 원정 지도를 사람이 Unity 에서 옮길 수 있게 굽는다.
> 구현 기록이다. 화면 요구사항은 각 기획서(docs/ui/*, docs/art/campaign-map.md)에 있다.

## 1. 씬

| 씬 | Build | 들어 있는 것 | 컨트롤러 |
|---|---|---|---|
| `Assets/_Project/Scenes/Menu.unity` | 0 (시작) | Main Camera, Directional Light, Board (흐린 바탕용), Menu | `MenuFlow` |
| `Assets/_Project/Scenes/Camp.unity` | 1 | Main Camera, Campaign Map View (구운 지도 층 `World Map`), Camp. 실행 중 `World.unity`(풍경)를 겹쳐 부른다 | `CampFlow` |
| `Assets/_Project/Scenes/Battle.unity` | 2 | Main Camera, Directional Light, Board, Game (BattleView·BoardInput), Overlay | `BattleFlow` |

- `Battle.unity` 는 옛 `Main.unity` 를 옮긴 것이다(GUID 그대로).
- 어느 씬에서 Play 를 눌러도 돈다. `Battle` 에서 바로 누르면 데모 1장으로 시작한다. `Camp` 는 지금 저장된 캠페인으로 L1 을 연다.

## 2. 씬을 넘는 것 (`Game/Screens/App.cs`)

- 캠페인 저장(`App.Campaign`), 메뉴에서 고른 카드, 장 선택의 마지막 상태(`App.Nav`)와 다음에 열 상태(`App.CampStart`), 시작할 전투(`BattleStage`·`Demo`·`Formation`·`StartNow`).
- 화면 전환은 `App.GoMenu()`, `App.GoCamp(nav)`, `App.GoBattle(stage, demo)` 이다. 씬이 바뀌면 앞 화면의 카메라·안개·보드·UI 가 같이 사라진다. 그래서 옛 `GameFlow.ClearScreen` 이 하던 손 정리가 없다.
- 데이터(`GameDataAsset`)는 처음 열린 씬의 컨트롤러가 `App.Ensure(data)` 로 한 번 싣는다.
- 공용: UI 문서 `ToolkitKit.Document` (패널 `Assets/_Project/UI/Panel.asset`, Expand, ui-style.md 15-1), 키 눌림 `KeyEdges`.

## 3. 원정 지도

- 풍경은 `Scenes/World.unity` (Synty `Demo_Cathedral_01` 복사본)다. `CampaignMapView.Show` 가 겹쳐 불러 활성 씬으로 두고 그 씬의 카메라·AudioListener 를 끈다 (메인 메뉴의 `MenuFlow` 와 같은 방식). 기획서는 docs/art/campaign-map.md.
- 지도 층은 `Camp.unity` 의 `World Map` 이다. `CampaignMapLayout`(C# 굽기 표)을 도구 모음 > 지도 > **지도 층 굽기** 로 굽는다 (`Editor/CampaignMapBaker.cs`). 없는 것만 더하므로 손으로 옮긴 점·시점이 남는다. 처음부터 다시 지으려면 `World Map` 을 지우고 누른다.
- 장은 이름(`Region s1` … `Region s20`)으로 찾는다. 자식 점 `Marker`(표식)·`Brazier`(클리어 화로)·`View`(가까운 시점이 보는 점). 지역 오브젝트를 옮기면 모두 따라간다.
- 카메라 시점은 `World Map` > `Cameras` 아래 Cinemachine 카메라다(전경 `Shot Road`·`Shot Castle`, 장마다 `Shot s1`…`s20`, 병영 `Shot Camp`). 각도는 시점마다 Follow 오프셋이 정한다. `CampaignMapView.Look` 은 쓸 시점 하나만 켜고 렌즈 시프트로 빈 곳 가운데에 맞춘다. 이동은 `Main Camera` 의 `CinemachineBrain` 블렌드다 (#57).
- 성 안 면(11~20장)에서는 `CampaignMapView` 가 세계 씬 `Cathedral` 의 렌더러 가운데 높이 5 위의 것을 끈다 (campaign-map.md 3).
- 실행 중에 짓는 것: 클리어한 장의 화로(`World Map/Progress`), 고른 장 스폿광, 병영 병종.

## 4. 검사

- PlayMode `BattlePlaybackTests` 는 `Battle` 씬을 읽는다.
- `tools/ui-audit/sweep.sh` 는 장면마다 `App.Go*` 로 그 씬을 연다.
- 인터랙션 확인은 검증 태스크 #60 (CLAUDE.md "검증 위임").
