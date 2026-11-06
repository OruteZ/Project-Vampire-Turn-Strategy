# UI

## 경로
- 기획: docs/design/ui.md, docs/design/art.md
- 기획서: docs/ui/ (화면별), docs/art/ui-style.md (UI 스타일: 색·글꼴·틀·버튼·배치 토큰), docs/art/ui-references.md (요소별 레퍼런스)
- 코드: Assets/_Project/Scripts/Game/Screens/ (`Ui.cs` 가 색 토큰·글꼴·공용 요소·틀). 달과 피 전투 HUD 는 `BattleHud.cs`, `GameFlow.styleTile` 을 켜야 쓴다 (꺼져 있으면 지금 데모 화면, UiTextParityTests 가 이쪽을 본다)
- UI 스프라이트: `ArtCatalog.ui` (이름으로 찾는다). 새 스프라이트는 이 배열에 더하고 `node tools/assets/scan.mjs`
- 아트: Assets/Synty/InterfaceDarkFantasyMenus, Assets/Synty/InterfaceDarkFantasyHUD, Assets/Synty/InterfaceCore (분류는 art-assets.md)
- 검토 스크린샷: docs/review/

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

## 열린 작업
- #24 UI 아트 테마·파이프라인 (5단계 목업 끝: docs/review/ui-style-tile-1~4.png. 6단계 방향 검토 차례)
- 목업에서 나온 것 (6단계에서 ui-style.md 에 반영할지 정한다)
  - 13-3 틀 물들이기: `Frame_Box_Medium_02` 를 `ally` 로 물들여도 붉은·녹색 기는 눈에 띄지 않았다 (ppu 배수 9). 틀은 그대로 써도 된다
  - 비네트 15% 가 생각보다 진하다 (선형 색 공간이라 낮은 알파가 진하게 섞임, 위쪽은 250px 까지 붉다). 8% 안팎이나 폭을 줄일 후보
  - `Button_DarkFantasyMenus_Basic`, `Button_DarkFantasy_ActionBar_Item`, `Input_DarkFantasyMenus_Hotkey_Prompt` 는 키트에 스프라이트로 없다(프리팹 쪽). 목업은 버튼·슬롯도 `Frame_Box_Medium_02` 로 그렸다. ui-style.md 6절 스프라이트 이름을 고쳐야 한다
  - 행동에 단축키가 없어 슬롯 밑에는 단축키 대신 행동 이름을 썼다
  - 붐비는 보드에서 머리 위 표시(체력 칸·이름·피해·표적 아이콘)가 이웃 유닛과 겹친다. 기존 보드 글자(`OverlayView` 의 "보름달 표적"·"다음 턴 보름달" TextMesh)와도 겹친다. 보드 글자를 뺄지 정해야 한다
  - 보드 카메라 영역(360/150/360/190 여백) 경계가 바탕과 색이 살짝 달라 사각형이 보인다
  - 만들지 않은 것: 전투 기록 펼치기, 버튼 호버·눌림·패널 등장·1단계 번쩍임 연출, 보름달 턴(해골 4개) 상태. 배치 단계는 예전 화면 그대로
  - CanvasScaler 기준을 1920×1080 으로 바꿔 메뉴·캠페인 화면도 전보다 약 17% 작게 보인다
- #16 한글 글꼴 파일 들이기 (막힘: 내려받기 허락)
- docs/ui/battle-hud.md, docs/ui/stage-select.md 는 제목만 있다. 7단계에서 채운다

## 맡길 때
- 모델: 테마·스타일·화면 기획서와 방향 검토는 opus, 목업·화면 구현은 sonnet
- 함정: Synty 키트 글꼴(LT Museum, Markazi, Pirata One)은 라틴 문자만 있다. 한글 글자는 #16 글꼴로
- 함정: 키트 장식은 대부분 악마 상징(해골, 오망성, 촉수)이다. 플레이어는 토벌대라서 어디에 쓸지는 테마가 정한다
- 함정: Synty 틀 스프라이트의 9-slice 테두리가 180~380px 라 그대로 쓰면 두껍다. `Image.pixelsPerUnitMultiplier` 로 줄인다 (ui-style.md 5절)
- 함정: Synty 틀 스프라이트에는 붉은·녹색 기가 구워져 있다. Image 색으로 물들이면 곱해져 남는다
