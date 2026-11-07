# UI Toolkit 시험: 메인 메뉴 (#44)

> 질문(사용자): "UI 레이아웃 픽셀 위치가 하드코딩으로 박혀 있는데 … uGUI와 UI Toolkit을 혼합해서 써먹어 보는 것도 가능한가?"
> 시험: 메인 메뉴 하나를 UI Toolkit(UXML/USS)으로 다시 만들어 지금 uGUI 메뉴와 나란히 비교했다. 게임 기본은 uGUI 그대로다.

## 1. 켜는 법

- 도구 모음 (Tools) > UI > "메인 메뉴를 UI Toolkit 시험판으로" 를 켜면 다음에 메뉴를 그릴 때 시험판이 나온다 (PlayerPrefs `ui.toolkitMenu`).
- 화면비 검사: `VU_RUN=<id> ONLY=1-menu TOOLKIT=1 bash tools/ui-audit/sweep.sh` (`LOCALE=en` 도 된다).

## 2. 파일

| 무엇 | 파일 |
|---|---|
| 배치 | `Assets/_Project/UI/Menu.uxml` (25줄, 숫자 없음) |
| 크기·색·여백 | `Assets/_Project/UI/Menu.uss` (메뉴만), `Theme.uss` (색 토큰, 값 = `Ui.cs`) |
| 패널 | `Assets/_Project/UI/Panel.asset` (기준 1920x1080, 맞춤 0.5, uGUI Canvas 와 같음), `Runtime.tss` |
| 코드 | `Game/Screens/MenuToolkit.cs`: 문장·그림·클릭만 채운다. `ArtCatalog.panel`, `menuDocument` |
| 검사 | `Editor/UiAudit.cs` 의 `ToolkitReport`: UI Toolkit 요소도 화면 밖·글자 넘침·카드 밖을 본다 |

## 3. 비교 (잰 값)

| 항목 | uGUI (`MenuScreen.cs`) | UI Toolkit |
|---|---|---|
| 배치를 바꿀 때 읽는 파일 | C# 6.5 KB | USS 2.3 KB |
| C# 안의 배치 숫자 | 62 개 (0·1 뺌) | 4 개 (툴팁 위치 44, 지연 600, 가운데 0.5×2) |
| 배치 숫자가 있는 곳 | C# 전체에 흩어짐 | `Menu.uss` 33 개 px 값 한곳 |
| 코드 줄 수 (빈 줄 뺌) | 95 | 152 = 화면 고유 84 + 다른 화면도 쓸 부품 53 (프리팹 옮기기 34, OS 글꼴 19) + using 등 |
| 화면비 검사 6개 × 한·영 | 0 건 | 0 건 (검사가 잡는지: 카드를 밀고 긴 이름을 넣으면 6 건) |
| 플레이어 입력 | 됨 | 됨 (마우스 올림 → 고른 카드 바뀜, 0.6초 툴팁, 클릭 → 전투) |
| 그리는 순서 | | uGUI 배경(흐린 보드) 위에 UI Toolkit 메뉴가 바로 그려짐 (`Panel.sortingOrder` 10) |
| 시험 | EditMode 177/177, PlayMode 4/4 (시험판 꺼짐 상태) | 같음. 다만 `UiTextParityTests` 는 uGUI 메뉴만 본다 |

사진: `docs/review/ui-toolkit-menu-ugui.png`, `ui-toolkit-menu-toolkit.png`(마우스 올림·툴팁), `ui-toolkit-menu-toolkit-en-5x4.png`. 검사 기록: `docs/review/ui-audit/toolkit-menu-ko.txt`, `-en.txt`.

## 4. 해 보고 안 것 (함정)

- **Synty 프리팹은 그대로 못 넣는다.** INTERFACE 의 장식 프리팹(Crest, Section_Break)은 uGUI `Image` 묶음이다. `MenuToolkit.Prefab` 이 그 이미지들(스프라이트·색·크기·위치·좌우 반전)을 읽어 VisualElement 로 다시 쌓는다. 크레스트(이미지 9장)와 구분선(2장)이 uGUI 와 같게 나왔다. 다른 INTERFACE 프리팹에도 쓸 수 있다.
- **스프라이트 9분할은 그대로 된다.** 스프라이트 테두리를 UI Toolkit 이 쓴다. uGUI `pixelsPerUnitMultiplier 9` 는 USS `-unity-slice-scale: 0.111px` 로 같아졌다.
- **OS 글꼴을 직접 못 쓴다.** uGUI 의 `Font.CreateDynamicFontFromOSFont` 글꼴을 넘기면 글자가 안 그려진다. 같은 규칙(목록에서 설치된 첫 이름)으로 고른 뒤 이름(`FontAsset.CreateFontAsset(family, "Regular")`)으로, 안 되면(가변 글꼴) 글꼴 파일로 만든다.
- **같은 크기 값에서 UI Toolkit 글자가 약 10% 크다.** 제목 56 → 50, 카드 이름 32 → 29 로 맞췄다.
- **그림은 USS 가 아니라 C# 에서 넣는다.** USS `url()` 로 Synty 경로를 쓰면 `tools/assets/scan.mjs` 가 참조를 못 본다. 그래서 `ArtCatalog` 에서 이름으로 찾아 넣었다.
- **UIDocument 는 OnEnable 에서 트리를 만든다.** 꺼진 오브젝트에 붙이고 에셋을 넣은 뒤 켠다.
- **런타임 UI Toolkit 은 tooltip 을 안 띄운다.** 툴팁은 직접 만들었다 (`MenuToolkit.Tip`).
- 한 화면 안에서 섞지 않았다. 섞으면 그리는 순서와 클릭 막힘을 요소마다 맞춰야 한다.

## 5. 따로 찾은 것

- 다국어 표의 글꼴 목록 첫 이름 `Nanum Myeongjo` 는 Windows 에 설치된 이름(`NanumMyeongjo`)과 달라 건너뛰어진다. 지금 두 메뉴 모두 다음 이름 `Noto Serif KR` 로 그린다. 기획서(docs/ui/localization.md)가 바라는 글꼴인지 확인이 필요하다.

## 6. 추천

화면 단위로 섞는다. 새로 만들거나 크게 고치는 화면부터 UI Toolkit 으로 옮기고, 한 화면 안에서는 섞지 않는다. 보드 위에 뜨는 것(체력 바, 피해 숫자)은 uGUI 로 둔다.

- 옮길 때 같이 하는 것: 두 번째 화면을 옮길 때 `Prefab`·`OsFont`·`Tip`·`Value` 를 공용 `ToolkitKit` 으로 뺀다. 그 화면의 `UiTextParityTests` 를 UI Toolkit 요소를 보게 바꾼다.
- 결정이 나기 전까지 시험판은 꺼 둔다. 안 쓰기로 하면 `MenuToolkit.cs`, `Assets/_Project/UI/`, 도구 모음 스위치, `ArtCatalog` 의 두 칸을 지운다(`UiAudit.ToolkitReport` 는 남겨도 해가 없다).
