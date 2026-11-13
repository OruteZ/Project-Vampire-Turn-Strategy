# UI

## 경로
- 기획: docs/design/ui.md, docs/design/art.md
- 기획서: docs/ui/ (화면별. 장 선택 3 레이어 docs/ui/stage-select.md, 병영 docs/ui/upgrade.md, 설정 창 docs/ui/settings.md), docs/art/ui-style.md (UI 스타일: 색·글꼴·틀·버튼·배치 토큰, 15 고정 크기, 16 화면 배치 원칙), docs/art/ui-references.md (화면별 레이아웃 조사·요소별 레퍼런스), docs/art/campaign-map.md (장 선택 바탕 3D 원정 지도)
- 코드: Assets/_Project/Scripts/Game/Screens/ (`ToolkitKit.cs` 문서 만들기·버튼·틀·칩·값·툴팁·Synty 프리팹 다시 쌓기·그늘, `Ui.cs` 색 토큰, `Kit.cs` 아이콘 이름, `BattleHud.cs` 전투 화면, `CampFlow.cs`·`CampToolkit.cs`·`CampMarkers.cs` 장 선택·병영, `MenuToolkit.cs`·`MenuFlow.cs` 메인 메뉴, `SettingsToolkit.cs` 설정 창(값은 `Game/GameSettings.cs`), `Fx.cs` 나타남·맥박·번쩍임·검은 화면, `App.cs` 씬 전환). 배치·크기는 Assets/_Project/UI/ 의 UXML·USS (`Theme.uss` 공용, `Menu`·`Camp`·`Battle` 화면별, `Panel.asset` 패널 비율). 원정 지도는 `Game/CampaignMapView.cs` + 배치 표 `Game/CampaignMapLayout.cs`, Camp 씬에 구움 (campaign-map.md 10). 구현 기록 docs/dev/unity-port.md "전투 화면 달과 피"
- UI 스프라이트: `ArtCatalog.ui`, 장식 프리팹: `ArtCatalog.uiPrefabs` (이름으로 찾는다). 새 스프라이트는 이 배열에 더하고 `node tools/assets/scan.mjs`
- 아트: Assets/Synty/InterfaceDarkFantasyMenus, Assets/Synty/InterfaceDarkFantasyHUD, Assets/Synty/InterfaceCore (분류는 art-assets.md)
- 화면 검사: `tools/ui-audit/sweep.sh`, `Scripts/Editor/UiAudit.cs`, 기록 docs/dev/ui-audit.md

## UI 아트 파이프라인
1. 테마 후보 2~3개 (기획 에이전트) → docs/art/ui-theme-candidates.md
2. 테마 확정 (사용자가 docs/design 에 씀)
3. 레퍼런스 요소별 수집 (기획 에이전트) → docs/art/ui-references.md. 이미지는 링크만
4. UI 스타일 기획서: 팔레트, 글꼴, 틀·버튼 종류와 상태, 아이콘, 중요도 3단계 표현 → docs/art/ui-style.md
5. 스타일 타일 목업: 전투 HUD 한 장을 Unity 에서 Synty 프리팹으로 만들고 스크린샷 (구현 에이전트)
6. 방향 검토: 다른 서브에이전트 → 사용자
7. 화면별 기획서 (docs/ui/*.md) 를 스타일 기준으로 다시 씀
8. 화면별 구현, 화면 하나에 이슈 하나. verify-unity 스크린샷으로 확인

## 결정
| 결정 | 상태(시험 중/확정 후보) | 날짜 | 출처(이슈·사용자 원문) |
|---|---|---|---|
| 위 8단계 파이프라인으로 UI 아트를 진행한다 | 시험 중 | 2026-11-06 | #24 "추천안으로 확정" |
| 테마는 에이전트가 후보를 먼저 내고 사용자가 고른다 | 시험 중 | 2026-11-06 | #24 |
| UI 바탕은 Synty Dark Fantasy UI 키트, 색·글꼴로 맞춘다 | 시험 중 | 2026-11-06 | #24 |
| 목업은 HTML 이 아니라 Unity 에서 Synty 프리팹으로 만든다 | 시험 중 | 2026-11-06 | #24 |
| UI 테마는 "달과 피": 아군은 달빛·쇠, 적은 피·악마 장식, 보름달 게이지가 화면 상징. 후보 A·B 는 커밋 851bfe8 docs/art/ui-theme-candidates.md | 시험 중 | 2026-11-06 | #24 "C 달과 피" |
| 보름달 직전 비네트는 8%·폭 80px (보름달 턴 15%) | 시험 중 | 2026-11-06 | #24 "추천대로" |
| 보드 위에 글자 라벨을 두지 않는다 (게이지·표적 아이콘·패널이 대신) | 시험 중 | 2026-11-06 | #24 "추천대로" |
| 화면에는 이름·숫자·아이콘만, 설명 문장은 툴팁으로 (ui-style.md 14). 레퍼런스는 Desperados III 임무 선택 | 시험 중 | 2026-11-07 | #33 "글보단 그림과 아이콘이 주가되고 설명문이 최대한 줄어들어야" |
| 모든 화면은 6개 화면비(16:9, 16:10, 4:3, 5:4, 21:9, 720p)에서 화면 밖·틀 밖·글자 넘침 0 (`tools/ui-audit/sweep.sh`) | 시험 중 | 2026-11-07 | #33 "레터박스 밖으로 튀어나가는 문제" |
| 머리 위: 방어는 체력 막대 가운데에 걸친 방패 아이콘 개수(방어 0 이면 막대만), 공격은 보드에 표시하지 않는다 (ui-style.md 8-1 7) | 시험 중 | 2026-11-09 | #100 "방어력은 체력바 위에 방패 아이콘의 개수로 표현하자. 방어력이 0일경우(대부분) 체력바만 있도록" |
| 머리 위 체력 막대는 늘 같은 길이(72px), 최대 체력이 크면 칸이 촘촘해지고 경계선은 얇게. 체력 숫자는 붙이지 않는다 (ui-style.md 8-1 1) | 시험 중 | 2026-11-09 | #100 "체력바의 길이는 항상 충분히 길게 유지하고 그 안의 칸이 좀 더 촘촘해지는 방식" |
| 호버·선택하면 머리 위에 이름만, 체력 숫자는 막대 위에 겹쳐서. 호버하지 않으면 숫자 없음. 보스도 같다 (ui-style.md 8-1 3, 8-5 6) | 시험 중 | 2026-11-10 | #110 "체력은 지금 보스 HP바처럼 "호버했을 때" 체력바 위에 표기 … 호버안했을시 숫자표기 x" |
| 화면 위 보스 막대를 없애고 보스 체력은 머리 위 전용 막대로 (Gloomhaven 참고, ui-style.md 8-5) | 시험 중 | 2026-11-10 | #110 "보스 체력바를 프롬 식으로 따로 띄우는 것 없애기. 대신 머리 위 체력바를 다르게 표현" |
| 농민 궁수·궁수 아이콘은 활 (`Inventory_Bows_01_Clean`) | 시험 중 | 2026-11-09 | #100 "활 아이콘으로 바꿔야 함" |
| 장 선택 지도(L1)에서 지역 이름 띠는 마우스를 올린 표식만 보인다. 키보드 초점은 배지만 커진다. L2 에서는 고른 장 이름이 고정 (stage-select.md 4-2 와 다름, 기획 쪽에 반영 요청) | 시험 중 | 2026-11-08 | #47 "1로 돌아가면 모든 이름은 호버기능으로" |
| 잠긴(플레이할 수 없는) 장은 장 정보(L2)로 가지 않는다: 표식 클릭·Enter 무시, L2 의 ↑/↓ 는 잠긴 장 앞에서 멈춤 (stage-select.md 7 "잠긴 장도 고를 수 있다" 와 다름) | 시험 중 | 2026-11-08 | #48 "플레이 불가능한 스테이지는 L2로 못넘어가게" |
| 적응형 비율 없음: UI 칸·버튼·글은 px 고정 크기, 한 점 앵커(모서리·변). 캔버스만 한 비율로 커지고 작아진다(CanvasScaler Expand, 1920×1080 보다 작아지지 않음, Match 값 안 씀). 화면을 채우는 것은 바탕(3D·흐린 배경·그늘)뿐. 화면비가 바뀌면 가운데 게임 화면만 더/덜 보인다 (ui-style.md 15) | 시험 중 | 2026-11-07 | 요청 #43 "적응형으로 커졌다 작아지는 모습은 굉장히 비정상적" |
| 화면 가운데는 게임(3D), UI 는 모서리·가장자리. 가운데 UI 는 결과·확인·규칙 창 같은 특정한 순간만. 한 레이어 UI 넓이 ≤ 30% (ui-style.md 16) | 시험 중 | 2026-11-07 | #43 "게임적 요소가 항상 중앙에 보이고 … 특정한 상황 아니면 사이드에" |
| 장 선택은 3 레이어: L1 지도(전경) → L2 장 정보(목표·출정) → L3 병영. 레이어가 바뀌면 오른쪽 칸과 카메라만 바뀌고 Esc 는 한 레이어 뒤로 (stage-select.md) | 시험 중 | 2026-11-07 | #43 요구 2 |
| 정보는 그 레이어 결정에 필요할 때만: 가진 금화는 병영(L3)에서만, 지도·장 정보에는 병영 버튼의 "고를 수 있는 병종 수" 배지만 (#99 배타 선택으로 강화 수 → 병종 수) (stage-select.md 6) | 시험 중 | 2026-11-07 | #43 "각 자원이나 정보는 각 Scene에서 필요한 경우에만" |
| UGUI 를 걷어내고 모든 게임 UI 를 UI Toolkit(UXML·USS)으로 짓는다. 3D 를 따라가는 표시(머리 위 체력, 지역 표식)도 UI Toolkit 패널 좌표로 옮긴다. Synty INTERFACE uGUI 프리팹은 그리지 않고 `ToolkitKit.Prefab` 이 그림 정보만 읽는다 | 시험 중 | 2026-11-09 | #106 "UGUI 폐기하고 UITOOLKIT으로 완전히 옮기고 싶어" |
| 장 선택 바탕은 Synty 프리팹을 코드가 데이터 표대로 놓아 짓는 3D 원정 지도. 장마다 지역 하나, 지역 표식은 고정 크기 UI(3D 점을 따라감), 이름은 가리킨·고른 지역만. 진행은 빛(클리어한 지역의 화로)으로 보인다 (campaign-map.md) | 시험 중 | 2026-11-07 | #43 "Synty 에셋을 사용해 3D맵을 제작 … 그 지역을 클릭하면 스테이지 정보화면으로" |
| 병영 강화 구매 확인은 두 번 누르기 대신 화면 가운데 작은 확인 창(이름·"구매하시겠습니까?"·가격·취소/구매). 산 뒤 번쩍임은 없다 (upgrade.md 6) | 시험 중 | 2026-11-08 | #87 "번쩍임 … 필요 없음. 정말 살까요 표기보다 --를 구매하시겠습니까? 하고 가운데 작은 레터박스" |
| 장 선택 칸 나타남·사라짐은 감속(ease-out) | 시험 중 | 2026-11-08 | #87 "UI 등장에 이징이 없음" |
| 전투 주 버튼(전투 개시·배치 완료·턴 종료)은 큰 원형 버튼, 고른 아군의 행동은 아래 화면 폭 80% 의 넓은 행동 바 판 안 큰 칸(1 이동·2 공격·3 대기·4 처치·5 치료/수습, 저절로 듣는 기술 칸). 칸은 아이콘만(이름은 툴팁), 원형 턴 종료는 판 안 오른쪽 끝, 배치 중에는 판 없이 아래 가운데 큰 "전투 개시" 원(2026-11-12 사용자 피드백). 레퍼런스 Baldur's Gate 3·Desperados III (battle-hud.md 3-4·3-6) | 시험 중 | 2026-11-11 | #118 "턴 종료 + 배치완료 버튼이 중앙 하단 큰 원형 버튼으로 있고 유닛 행동 선택할때도 행동 바가 있으면 좋겠어" |
| 메인 메뉴 바탕은 Synty 데모 씬 `Demo_Cathedral_01`(백작성) 실시간 장면, 메뉴는 왼쪽 세로 글자 목록 + 왼쪽 그늘. 레퍼런스 Desperados III·The Witcher 3 메인 메뉴 (main-menu.md 8~11) | 시험 중 | 2026-11-11 | #116 "싱티 데모씬 백작성 이쁘던데 그거쓰자", "UI도 바꿔" |
| 원정 지도는 실행 중 씬만 읽는다. 배치 표(`CampaignMapLayout`)는 굽기 입력일 뿐이고, 길·물·야영지·표식·화로 자리도 씬의 점으로 구워 손으로 옮긴다 (campaign-map.md 10-2) | 시험 중 | 2026-11-09 | #104 "씬을 진실로" |
| 원정 지도 카메라 이동은 손 코드 보간이 아니라 Cinemachine(구운 시점 + 브레인 블렌드)으로 한다. 시점은 Camp 씬에서 손으로 고친다 (campaign-map.md 구현 메모) | 시험 중 | 2026-11-08 | #57 "그 코드 밀어버리고 Cinemachine 패키지 설치해서 처리해줘" |

## 맡길 때
- 모델: 테마·스타일·화면 기획서와 방향 검토는 opus, 목업·화면 구현은 sonnet
- 함정: Synty 키트 글꼴(LT Museum, Markazi, Pirata One)은 라틴 문자만 있다. 한글은 `Resources/Fonts` 의 Hahmlet(+ Source Han Serif K 대체)로 그린다(ui-style.md 4)
- 함정: 키트 장식은 대부분 악마 상징(해골, 오망성, 촉수)이다. 플레이어는 토벌대라서 어디에 쓸지는 테마가 정한다. 문장(Crest) 28개 중 해골이 없는 것은 Large_03(마름모)·Banner_02(천) 정도다
- 함정: Synty 틀 스프라이트의 9-slice 테두리가 180~380px 라 그대로 쓰면 두껍다. USS `-unity-slice-scale` 로 줄인다(0.111px = 옛 ppu 9, 0.0714px = ppu 14) (ui-style.md 5절)
- 함정: UIDocument 의 뿌리와 템플릿 칸은 화면 전체를 덮고 기본으로 클릭을 받는다. 그대로 두면 EventSystem 이 늘 "UI 위" 라고 해 지도·보드 클릭이 안 된다. `ToolkitKit.Document` 가 Ignore 로 둔다
- 함정: 투영 옮김(`BoardView.FitCamera`)은 `p[0,2] = -(2·cx − 1)`. 부호가 반대면 화면 가운데에서 반대쪽으로 간다
- 함정: 원정 지도 카메라는 Cinemachine 이다. 빈 곳 맞춤에 컴포저 화면 위치를 쓰면 옆으로 덜 간다. 렌즈 시프트를 쓰고, 그러려면 Unity 카메라가 물리 카메라여야 한다 (campaign-map.md 구현 메모)
- 함정: 전투 HUD 는 상태가 바뀌면 통째로 다시 짓는다. 호버(칸·유닛·시체)만 바뀌면 `BattleHud.RebuildHover` 로 `Hover`·`Heads` 층만 다시 짓는다. 호버에 따라 달라지는 것(유닛·시체 패널, 머리 위 표시, 행동 막대)은 `DeployHover`/`FightHover` 에 둔다. 다른 곳에 두면 호버 때 갱신되지 않는다 (#104)
- 함정: 적 턴은 모델이 먼저 끝나고 화면이 나중에 재생된다. 재생 중 상태(보름달 턴)는 `BattleView.Played` 배너로 안다 (`BattleFlow.OnPlayed`)
- 함정: Synty 틀 스프라이트에는 붉은·녹색 기가 구워져 있다. Image 색으로 물들이면 곱해져 남는다
- 함정: 모든 화면이 같은 `Panel.asset`(Expand, 1920×1080)을 쓴다. 바꾸면 모든 화면이 함께 바뀌니 sweep 은 모든 화면에 돌린다. 레이아웃 전에도 패널 크기가 필요하면 `ToolkitKit.PanelSize()` 로 계산한다(첫 프레임엔 UI Toolkit 레이아웃이 아직 없다)
- 함정: 지도·보드 클릭을 막는 판단(`PointerBlocked`, `CampFlow.PointerOverUi`)은 `ToolkitKit.PointerOverUi` 하나로 본다. 화면 전체를 덮는 뿌리·층(`.layer`)은 picking-mode Ignore 여야 한다
- 함정: Input System 만 켠 이 프로젝트에서 UI Toolkit 런타임 패널은 EventSystem(+`InputSystemUIInputModule`)이 있어야 포인터를 받는다. 없으면 클릭·호버가 전혀 안 온다(2026-11-09 재어 봄). `ToolkitKit.Document` 가 없으면 만든다
- 함정: USS 는 선택자 구체성으로 이긴다. `.battle Label { margin: 0 }` 같은 넓은 규칙이 `.hint { margin-left }` 를 덮어 가운데 정렬이 깨졌다. 공용 초기화 규칙은 클래스 하나짜리보다 구체적이면 안 된다
- 함정: UI Toolkit 은 둥근 채움(Image.fillMethod Radial)이 없다. 보름달 위상은 `generateVisualContent` + Painter2D 로 그린다(`BattleHud.MoonPhase`: 오른쪽 반원 + 반타원 명암 경계)
- 함정: 다른 씬을 겹쳐(additive) 불러도 하늘·안개·주변광은 활성 씬 것만 쓴다. 메인 메뉴는 데모 씬을 `SetActiveScene` 한다. UI Toolkit 화면 뿌리에 `background-color` 가 있으면 뒤의 3D 가 전부 가려진다(USS 를 바꾼 뒤 에디터가 다시 읽지 않으면 `AssetDatabase.Refresh`)
- 함정: `BoardView.LateUpdate` 는 `Board` 가 남아 있으면 카메라를 보드에 다시 맞춘다(`Clear()` 뒤에도 남는다). 원정 지도가 카메라를 쓰는 동안 막아야 한다 (campaign-map.md 구현 메모)
- 함정: `FX_*`, `SM_Bld_Base_*` 프리팹은 PolygonDarkFantasy 와 PolygonGeneric 에 같은 이름이 있다. 이름으로 찾을 때 PolygonDarkFantasy 쪽을 쓴다
