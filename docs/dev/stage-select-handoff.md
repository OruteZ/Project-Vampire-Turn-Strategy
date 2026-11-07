# 장 선택 개편 인계 (#43 → UI Toolkit 세션)

> 사용자(#43 채팅): "현재 다른 세션에서 UGUI + UIToolkit으로 수정을 이어나가고 있으니, 제시안을 해당 세션으로 넘기고 개발을 UI Toolkit + UGUI로 하도록 하자"
> 이 문서는 구현 전 설계 메모다. 요구사항은 기획서에만 있다. 이 문서와 기획서가 다르면 기획서를 따른다.

## 1. 읽을 기획서

| 무엇 | 문서 |
|---|---|
| 세 레이어(L1 지도, L2 장 정보, L3 병영), 칸 px·앵커, 표식, 키, 전투 뒤 돌아오기, 어느 UI 로 짓나 | docs/ui/stage-select.md (10절이 UI Toolkit + uGUI 나눔) |
| L3 병영 칸, 병종 탭(미해금 병종은 자물쇠만) | docs/ui/upgrade.md |
| 3D 원정 지도, 1차 범위(1-1), 카메라 셋, 데이터 | docs/art/campaign-map.md |
| 고정 크기 규칙, Expand, 가운데는 게임 | docs/art/ui-style.md 15·16 |
| 레퍼런스 리서치 | docs/art/ui-references.md |
| 결정 표 | docs/areas/ui.md (`시험 중`, 출처 #43) |

## 2. 지금 코드의 함정 (how 조사, 2026-11-07)

1. `GameFlow.ClearScreen(true)` 는 `Screen` 에 불투명하고 레이캐스트를 받는 Ink Image 를 켠다. 지도가 가려지고 `IsPointerOverGameObject()` 가 늘 참이 된다. 장 선택은 전투처럼 `ClearScreen(false)` 로 연다.
2. `BoardView.Clear()` 는 `Board`·`Stage` 를 그대로 둔다. 그래서 `BoardView.LateUpdate`·`FrameInto` 가 `viewCamera` 를 계속 옛 보드에 맞추고, 안개·주변광도 남는다. `Clear()` 에서 `Board = null`, `Stage = null` 로 만드는 것이 뿌리 수정이다 (`UiAudit` 의 board-cropped 검사도 이것으로 장 선택에서 꺼진다).
3. 지금은 행동마다 `ShowCamp()` 가 화면을 다시 짓고 `board.Show` 까지 다시 돈다(화살표 키마다 보드 전체). 지도는 장 선택에 들어올 때 한 번 짓고, 바뀌는 것(고른 지역, 화로, 스폿광, 카메라)만 고친다.
4. `BoardInput` 은 전투에서만 돈다(`Session == null` 이면 아무것도 안 함). 지역 클릭은 uGUI 표식 버튼이 받는다. 빈 땅 클릭은 uGUI·UI Toolkit 어디에도 포인터가 없을 때의 클릭이다.
5. 카메라는 지금 `viewCamera`(Main Camera)를 쓴다. 새 카메라를 만들면 `Popup.cs` 의 `Camera.main`, AudioListener 가 둘이 되는 것, Backdrop Camera 와의 depth 를 맞춰야 한다.
6. `UiTextParityTests.CampaignScreenShowsEveryGoldenState` 는 art 없이 카메라 없는 RectTransform 에 장 선택을 짓는다. 장 이름·금화가 글자로 있어야 한다. UI Toolkit 으로 옮기면 이 시험을 Label 을 보게 고친다. `tools/ui-audit/sweep.sh` 는 `gf.ShowCamp()` 를 부른다(인자 없는 public 으로 둔다).
7. 새 Synty 참조를 더하면 `node tools/assets/scan.mjs` 로 `required.tsv` 를 다시 뽑는다. 안 하면 `RequiredAssetsTests.ListCoversEverySyntyReference` 가 깨진다. scan 은 `git ls-files` 를 읽어서, 새 에셋은 git 에 올려야 보인다.
8. `ArtCatalog.asset` 을 채우는 에디터 코드는 없다. YAML 손편집 대신 `unity ... script eval` 의 `SerializedObject` 나 도구 모음 버튼 한 줄로 채운다 (CLAUDE.md).

## 3. 고른 설계 (architect 후보 둘을 합친 것)

같은 과제에 후보 둘을 따로 짰다. 둘은 지도와 카메라에서 같은 모양에 이르렀고, 상태를 어디에 둘지만 달랐다. 아래는 두 후보를 합친 것이고, UI 부분은 UI Toolkit 결정에 맞게 고쳤다.

| 덩어리 | 모양 |
|---|---|
| 레이어 상태 | 순수 상태 기계 하나(예: `CampNav`). 장 번호 하나(L1 초점 = L2 고름 = L3 에서 돌아갈 장), `info`(L2 열림), `barracks`(L3 열림) 두 값. L3 는 위에 덮는 값이라 "온 레이어" 기록이 따로 필요 없다. 확인 대기(진행 초기화·강화 사기)는 한 칸(`armed`)에 하나만, 2.5초 만료는 `Tick` 입력. 입력 하나 → 새 상태 + 할 일(`None`/`Redraw`/`Buy`/`Reset`/`March`/`Menu`). GameFlow 의 `resetArm`·`buyArm`·`buyUntil`·`bought`·`campSel` 은 지운다 |
| 들어오는 법 | `FromMenu()` (처음 고른 장 규칙, 지금 `CampaignScreen.DefaultStage`), `FromBattle(stage, firstClear)` (stage-select.md 5-3: 새 장이 열렸으면 L1, 아니면 L2). 둘 다 순수 함수라 EditMode 시험이 된다 |
| 지도 | `CampaignMapView` 하나. `Show(campaign)` 는 고정 지형을 처음 한 번 짓고 진행 층(화로·길 밝기)만 다시 짓는다. `Look(view, stage, emptyRect)` 가 카메라 0.3초 보간과 스폿광을 같이 움직여 표식과 카메라가 한 프레임 어긋나지 않게 한다. `ScreenPoint(stage)` 를 표식이 매 프레임 읽는다. 투영 옮김은 `BoardView.FitCamera` 의 off-centre 방법을 같이 쓴다 (`BoardView` 의 `Prop`·`AddLight`·`Hash` 를 internal static 으로) |
| 지도 데이터 | campaign-map.md 10 의 `CampaignMap.asset` 대신 배치 표는 C#(`CampaignMapLayout`), 프리팹만 `ArtCatalog.mapProps` 배열(이름으로 찾음, `uiPrefabs` 와 같은 모양). 자리 ±1.5 손맞춤을 C# 고침으로 할 수 있어서다. 배열은 도구 모음 버튼(지도 > 프리팹 목록 채우기)이 레이아웃의 이름 목록으로 채우고, 시험 하나가 목록과 레이아웃이 어긋나면 실패한다. 구현할 때 campaign-map.md 10절을 이 모양으로 고쳐 달라고 기획 쪽에 남긴다 |
| UI | UI Toolkit 문서 하나(UXML/USS)에 머리·진행 초기화·병영 버튼·L2 칸·L3 칸. 레이어 전환은 칸에 클래스를 붙이고 떼기. 내용(장 정보, 병종 탭)은 상태가 바뀔 때 다시 채운다. 지역 표식 10개는 uGUI 로 한 번 짓고 매 프레임 `anchoredPosition` 만 바꾼다(빈 곳 밖이면 숨김) |
| 캔버스 | `GameFlow.BuildCanvas` 와 `Panel.asset` 둘 다 Scale With Screen Size 1920×1080 Expand |

## 4. 순서 (단계마다 확인)

1. `BoardView.Clear` 뿌리 수정, Expand(Canvas·Panel 둘 다) → EditMode 전체, 메뉴·전투 sweep 6개 화면비.
2. 상태 기계 + 시험(키 걷기, 전투 뒤 돌아오기, 두 번 누르기 풀림) → EditMode.
3. 지도 레이아웃·프리팹 목록·scan → 목록 시험, `RequiredAssetsTests`.
4. 지도 짓기·카메라 → 지도 스크린샷(전경·지역·L3).
5. UI Toolkit 칸 + uGUI 표식, `UiTextParityTests` 고침, 새 Loc 키(stage-select.md 8) → EditMode.
6. GameFlow 연결 → verify-unity 로 실제 플레이(메뉴 → L1 → 표식 → L2 → 출정 → 전투 → 돌아옴 → B → L3 → 사기).
7. sweep 장 선택 장면 L1·L2(1·8·10장)·L3, 한·영 → `docs/review/stage-select-*.png`.

## 5. 정해 둔 기본값 (사용자가 바꿀 수 있음)

- 아직 해금되지 않은 병종 탭은 자물쇠만 보인다 (이름·강화 스포일러 막기). 방향성 검토에서 나온 것.
- Expand 는 전투·메인 메뉴에도 걸린다. 4:3·5:4 에서 UI 가 지금보다 13~16% 작아진다(추정, 잰 값 아님).
- ↑ 는 다음 장(지도에서 북쪽). 지금 화면과 반대다.
- 메인 메뉴 덩어리는 5:4 에서 위로 몰리지 않게 가운데 앵커로 옮긴다 (main-menu.md 후속).
