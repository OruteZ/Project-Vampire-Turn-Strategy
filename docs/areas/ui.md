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
| 보름달 직전 비네트는 8%·폭 80px (보름달 턴 15%) | 시험 중 | 2026-11-06 | #24 "추천대로" |
| 보드 위에 글자 라벨을 두지 않는다 (게이지·표적 아이콘·패널이 대신) | 시험 중 | 2026-11-06 | #24 "추천대로" |

## 열린 작업
- #24 UI 아트 테마·파이프라인 (7단계: docs/ui/battle-hud.md 끝. 남은 화면별 기획서: stage-select, main-menu·upgrade 를 스타일 기준으로 다시)
- 8단계 전투 화면 구현 때 할 것 (방향 검토에서 나옴)
  - 적을 살펴볼 때 위협 칸(`Mark.Threat`)과 체크박스·안내 문구 지우기 (ui-style.md 8-4)
  - 보드 칸 표시 색을 8-2 토큰으로, 흡혈 범위 칸 따로 (`OverlayView`)
  - `OverlayView` 의 보드 글자 라벨 지우기 (8-1 의 6)
  - 전투 기록 조용하게 (10절), 보드 카메라를 전체 화면으로 그려 경계 사각형 없애기
  - 기준 해상도: 지금은 `styleTile` 이 켜져야 1920×1080, 꺼지면 예전 1600×900. 화면을 다시 만들면 1920×1080 하나로
  - docs/review/ui-style-tile-2.png 다시 찍기 (호버가 잡히지 않아 피해 미리 보기가 빠졌다)
  - 패널 경고 줄, "적 페이즈", "닫기" 를 Text 표 키로
- #16 한글 글꼴 파일 들이기 (막힘: 내려받기 허락)
- docs/ui/stage-select.md 는 제목만 있다. 7단계에서 채운다

## 맡길 때
- 모델: 테마·스타일·화면 기획서와 방향 검토는 opus, 목업·화면 구현은 sonnet
- 함정: Synty 키트 글꼴(LT Museum, Markazi, Pirata One)은 라틴 문자만 있다. 한글 글자는 #16 글꼴로
- 함정: 키트 장식은 대부분 악마 상징(해골, 오망성, 촉수)이다. 플레이어는 토벌대라서 어디에 쓸지는 테마가 정한다
- 함정: Synty 틀 스프라이트의 9-slice 테두리가 180~380px 라 그대로 쓰면 두껍다. `Image.pixelsPerUnitMultiplier` 로 줄인다 (ui-style.md 5절)
- 함정: Synty 틀 스프라이트에는 붉은·녹색 기가 구워져 있다. Image 색으로 물들이면 곱해져 남는다
