# UI 레퍼런스 — 달과 피

출처: docs/design/ui.md, docs/design/art.md, docs/design/campaign.md, docs/design/progression.md, docs/design/combat.md, docs/design/vampire.md, docs/design/units.md (커밋 f7fc7f8). 테마 결정: docs/areas/ui.md 결정 표 "달과 피" (#24). 화면별 레이아웃 조사: #43 (요청 #43)

> 두 부분이다. 1~4절은 화면(Scene)마다 어디에 무엇을 두는지(레이아웃)를 작품별로 본 것과 거기서 뽑은 규칙이다 (#43). 5절은 UI 요소마다 무엇을 가져오고 무엇은 가져오지 않는지다 (#24). `docs/art/ui-style.md` 가 이 문서를 바탕으로 수치·배치를 정한다.
> 이미지는 저장소에 넣지 않는다. 링크는 실제로 열어 본 쪽만 적는다. 본 것은 우리 말로 줄여 적는다.

## 1. 참고 작품

| 작품 | 왜 보나 | 본 곳 |
|---|---|---|
| Desperados III | art.md 의 광원·시점 참고작. 지도 한 장에 임무를 고르는 화면, 작은 HUD | Interface In Game: https://interfaceingame.com/games/desperados-iii/ , Mission Select https://interfaceingame.com/screenshots/desperados-iii-mission-select/ , Chapter https://interfaceingame.com/screenshots/desperados-iii-chapter/ |
| Civilization VI | 지도가 화면 전체, HUD 는 네 모서리. 하위 화면이 옆 칸으로 열리고 지도는 계속 보인다 | Map https://interfaceingame.com/screenshots/sid-meiers-civilization-vi-map/ , Choose production https://interfaceingame.com/screenshots/sid-meiers-civilization-vi-choose-production/ |
| Civilization VII | UI 크기 조절과 고정 배치가 어긋날 때 생기는 문제 (반면교사) | https://blog.yuo.be/2025/02/09/civilization-vii-it-needs-some-love/ , https://game8.co/articles/featured/6362 |
| Baldur's Gate 3 | 전투 HUD 를 아래 가운데 한 덩어리로 모은다. 야영지(camp)가 메뉴가 아니라 3D 장소다 | HUD 번호 그림 https://www.gamepressure.com/baldurs-gate-iii/interface/zad9f7 , Camp https://bg3.wiki/wiki/Camp |
| 소녀전선 2: 망명 | 3D 모델이 화면 가운데, 메뉴·수치는 가장자리. 강화 화면도 강화할 물건(무기 모델)이 가운데 | 리뷰 스크린샷 https://www.reimarufiles.com/2025/01/13/girls-frontline-2-exilium-review-guns-and-girls-money-and-strategy , https://iopwiki.com/wiki/GFL2_Main_Screen , https://gamingonphone.com/reviews/girls-frontline-2-exilium-game-review |
| 파이어 엠블렘 인게이지·풍화설월 | 3D 디오라마 세계 지도 위 장 지점, 출격 준비 화면 | 세계 지도 그림 https://finalweapon.net/2022/12/26/fire-emblem-engage-details-world-map-fire-emblem-for-beginners-trailer , https://fireemblemwiki.org/wiki/World_map , https://fireemblemwiki.org/wiki/Battle_preparations |
| Unity uGUI 문서 | 캔버스 비율 맞추기 방식 (Expand) | https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/script-CanvasScaler.html , https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/HOWTO-UIMultiResolution.html |

#24 에서 본 작품(5절 요소별 표의 바탕):

| 작품 | 링크 | 왜 보나 |
|---|---|---|
| Into the Breach | https://store.steampowered.com/app/590380/ | 작은 화면에 전술 정보를 담는 가독성의 기준. 칸 단위 체력, 피해 미리 보기 |
| Gloomhaven (디지털판) | https://store.steampowered.com/app/780290/ | 육각 보드, 적 능력치 카드. docs/design/art.md 의 맵 아트 참고작 |
| Darkest Dungeon | https://store.steampowered.com/app/262060/ | 화면 위 가운데 횃불 게이지가 분위기와 규칙을 같이 바꾼다 → 보름달 게이지 |
| Darkest Dungeon II | https://store.steampowered.com/app/1940340/ | 같은 고딕 톤을 장식을 줄여 정리한 판 |
| Frostpunk | https://store.steampowered.com/app/323190/ | 화면 위 가운데 원형 게이지(기온)와 다가오는 위협 표시 |
| XCOM 2 | https://store.steampowered.com/app/268500/ | 화면 아래 가운데 행동 버튼 줄, 머리 위 칸 체력 |
| The Banner Saga | https://store.steampowered.com/app/237990/ | 원정대 느낌의 유닛 카드, 캠프 화면 |
| Diablo IV | https://store.steampowered.com/app/2344520/ | 다크 판타지 틀·글꼴을 절제해 쓰는 법, 보스 체력 막대 |

## 2. 화면별 레이아웃 읽기

자리는 1920×1080 화면을 9칸(모서리 넷·변 넷·가운데)으로 나눠 적는다.

### 2-1. 캠페인 지도 / 월드맵

| 작품 | 본 것 | 가져오는 것 |
|---|---|---|
| Desperados III Mission Select | 그림 지도가 화면 전체. 고른 임무 지점에 붉은 원 + 이름 띠. 임무 썸네일 줄은 왼쪽 위 작은 틀(화면 폭 약 3분의 1, 높이 약 6분의 1). 아래 왼쪽에 짧은 버튼 셋, 아래 오른쪽 끝에 "뒤로" 하나. 가운데에는 UI 가 없다 | 지도가 바탕 전체, 고른 지점은 지도 위 표식으로 강조. 버튼은 아래 모서리에 작게. 뒤로는 늘 같은 자리 |
| 파이어 엠블렘 인게이지 세계 지도 | 3D 디오라마. 숲은 작은 나무 덩어리, 성·마을은 축소 모형, 지점 사이는 땅 위 가는 길. 지점은 빛나는 원판 표식, 군대는 인물 하나로 지도 위에 선다. 비스듬한 위 시점(약 40~50°), 하늘은 화면 위 끝에만. 다음 장으로 바로 가는 단축키가 있다 | Synty 소품으로 축소 모형 지도를 짓는다. 지점 표식 + 땅 위 길. 우리 군의 위치(야영지)를 지도에 둔다. ↑/↓ 로 다음·앞 장 |
| 파이어 엠블렘 풍화설월 | 지도는 걸어 다니지 않는다. 어느 전투가 어디서 일어나는지만 보인다 | 지도는 고르는 곳이지 움직이는 곳이 아니다. 카메라 끌기·이동 없이 미리 정한 시점 몇 개만 |
| Civilization VI Map | 지도가 화면 전체. 위 끝 얇은 띠(왼쪽 재화, 오른쪽 턴·시간), 왼쪽 위 작은 추적 칸, 왼쪽 아래 미니맵, 오른쪽 아래 유닛 칸 + 다음 턴 버튼이 한 덩어리. 이름표는 지도 위 도시에 붙는다 | 네 모서리만 쓴다. 지도 위 이름표는 지점에 붙는 작은 표식 |

### 2-2. 임무 선택 → 브리핑

| 작품 | 본 것 | 가져오는 것 |
|---|---|---|
| Civilization VI Choose production | 도시를 고르면 오른쪽 끝에 세로로 긴 고정 폭 목록이 열린다. 고른 도시는 지도 가운데로 오고 지도는 그대로 보인다. 오른쪽 아래 도시 칸은 그대로 | 장을 고르면 오른쪽에 고정 폭 상세 칸이 열리고, 카메라가 그 지역을 남은 가운데로 옮긴다 |
| Desperados III Quest | 목표는 아이콘 + 한 줄, 긴 설명은 툴팁 (#33 에서 봄) | 브리핑 칸은 아이콘 줄. 장 소개 글은 툴팁 (ui-style.md 14) |
| Desperados III Chapter | 장 제목 띠만 화면 가운데에 크게(장 번호 + 이름 + 건너뛰기 키). 다른 UI 는 없다 | 가운데 UI 는 전환·결과 같은 특정한 순간에만, 짧게 |
| 파이어 엠블렘 출격 준비 | 유닛 고르기·소지품·지도 보기·승리 조건·전투 시작이 한 메뉴. 풍화설월은 흐린 전장 지도 위 가운데 고정 크기 창(왼쪽 명단, 오른쪽 상세). 전투 시작을 누르면 다시 열 수 없다 | 관리 화면(병영)은 "특정 상황"이라 큰 칸을 써도 되지만 고정 크기, 뒤에 세상이 보이게 |

### 2-3. 병영·강화

| 작품 | 본 것 | 가져오는 것 |
|---|---|---|
| 소녀전선 2 무기 강화 | 강화할 무기 3D 모델이 화면 가운데에 크게. 왼쪽 위 뒤로·홈 아이콘, 오른쪽 위 탭 둘, 왼쪽 아래 효과 설명, 오른쪽 아래 수치 두 줄과 강화 버튼 둘 | 강화하는 대상(병종)이 그림으로 보이고, 수치·버튼은 가장자리. 1차는 지도 위 야영지를 가운데에 두고, 병종 모델은 후속(upgrade.md) |
| 소녀전선 2 로비 | 3D 인물이 화면 대부분. 메뉴는 오른쪽 위에 글자 목록, 아래 오른쪽 아이콘 줄, 왼쪽 아래 작은 지휘관 레벨. UI 를 숨기는 버튼이 있다 | 메뉴 글은 한쪽 변에 붙인다. 가운데는 3D |
| Baldur's Gate 3 야영지 | 쉬기·동료 바꾸기·보관함이 메뉴가 아니라 3D 장소(천막·모닥불)에서 일어난다 | 병영을 지도 위 야영지(모닥불·깃발·수레)로 보이게 한다 |
| progression.md 의 스타크래프트 2 무기고 | (docs/design 이 가리킨 참고작. 이번에 열어 보지 않았다) | 병종 단위 강화 목록 |

### 2-4. 전투 HUD

| 작품 | 본 것 | 가져오는 것 |
|---|---|---|
| Baldur's Gate 3 | 초상·행동 칸·턴 종료(모래시계)가 아래 가운데 한 덩어리(화면 폭 약 절반, 높이 약 8분의 1). 미니맵은 오른쪽 위 모서리, 메뉴 버튼은 미니맵 둘레와 오른쪽 아래 끝. 화면 위 3분의 2 이상이 세상 | 지금 battle-hud.md 배치(아래 모서리 패널 + 아래 가운데 행동 줄)와 맞다. 덩어리 수를 늘리지 않는다 |
| 소녀전선 2 전투 | 화면 HUD 가 거의 없다. 체력·안정성은 유닛 머리 위, 위 가운데 작은 턴 띠 | 머리 위 표시(ui-style.md 8)를 1단계 정보 자리로 유지 |
| Desperados III HUD | 왼쪽 아래 초상, 아래 가운데 기술 칸, 단축키는 칸 안 (#33 에서 봄) | 그대로 |

### 2-5. 화면 크기·비율

| 출처 | 본 것 | 가져오는 것 |
|---|---|---|
| Unity CanvasScaler 문서 | Match Width Or Height 는 폭·높이 사이 비율로 캔버스를 키운다. Expand 는 캔버스가 기준 해상도보다 작아지지 않게 한 축을 늘린다. 기준 해상도는 UI 를 짠 크기 | Expand: 1920×1080 에서 맞춘 배치가 모든 화면비에서 그대로 들어간다 (ui-style.md 15) |
| Unity 다중 해상도 안내 | 요소는 가까운 모서리에 앵커. Scale With Screen Size 는 버튼·여백·글을 함께 키운다. 화면비가 섞이면 Match 0.5 를 권한다 | 모서리 앵커는 따른다. Match 0.5 는 따르지 않는다: 21:9 에서 캔버스 높이가 935 로 줄고 5:4 에서 폭이 1610 으로 준다. 고정 크기 칸을 쓰는 우리는 기준보다 작아지지 않는 쪽이 낫다 |
| Civilization VII 비평 | UI 크기 조절이 켜고 끄기 둘뿐이라 4K 에서 너무 크거나 너무 작다. 글자 크기만 따로 키우면 고정 배치끼리 겹친다. 지도 위 글이 지도와 섞여 안 읽힌다 | 크기는 화면 전체 한 비율로만 바꾼다. 글·칸을 따로 키우지 않는다. 지도 위 글은 바탕 칸 위에 |

## 3. 뽑은 규칙 (ui-style.md 15·16 으로 간다)

1. **가운데는 세상.** 다섯 작품 모두 화면 가운데를 지도·보드·3D 모델이 차지한다. UI 는 네 모서리와 위·아래 변에 붙는다.
2. **가운데 UI 는 순간뿐.** 장 제목 띠, 결과, 확인 창, 관리 창처럼 그 순간 할 일이 그것뿐일 때만 가운데를 쓴다. 그래도 고정 크기이고 뒤 세상이 보인다.
3. **한 자리에 한 덩어리.** 모서리 하나에 묶음 하나(Civ 오른쪽 아래 유닛+다음 턴, BG3 아래 가운데 HUD). 흩어진 작은 칸을 늘리지 않는다.
4. **고르면 옆 칸이 열린다.** 상세는 오른쪽 고정 폭 칸, 고른 것은 카메라가 남은 가운데로 데려온다 (Civ VI). 지도는 닫히지 않는다.
5. **지점은 지도 위 표식.** 목록 칸이 아니라 지도의 지점이 고르는 단추다 (Desperados III, 인게이지). 이름은 고른·가리킨 지점에만.
6. **정보는 쓰는 화면에만.** 소녀전선 2 는 강화 재화를 강화 화면에서, 인게이지는 출격 준비를 지도 다음 단계에서 보인다. 재화는 그 재화를 쓰는 레이어에만.
7. **크기는 한 비율로.** 화면비가 바뀌면 세상이 더/덜 보이고 칸 크기는 그대로. 칸·글을 따로 키우지 않는다 (Civ VII 반면교사).
8. **뒤로는 늘 같은 자리.** Desperados III "뒤로" 는 아래 오른쪽 끝, 소녀전선 2 는 왼쪽 위. 우리는 왼쪽 위 + Esc.

## 4. 가져오지 않는 것

- 지도 일러스트 한 장(Desperados III). 키트에 없다. Synty 3D 소품으로 짓는다 (docs/art/campaign-map.md).
- 지도 위를 군대가 걸어 다니기(인게이지 일부). campaign.md "선택지는 두지 않는다" 라 걸을 이유가 없다.
- 카메라 끌기·확대(Civ). 정한 시점 셋(전경·지역·야영지)만 둔다.
- 화려한 로비 인물·광고 띠(소녀전선 2).
- UI 크기 설정 켜고 끄기(Civ VII). 필요하면 나중에 한 비율 값 하나로.

## 5. 요소별 표 (#24)

### 5-1. 보름달 게이지 (화면 위 가운데, 1단계)

| 가져온다 | 가져오지 않는다 |
|---|---|
| Darkest Dungeon: 게이지 하나가 화면 분위기를 바꾼다. 달이 찰수록 화면 가장자리에 핏빛이 번진다 | Darkest Dungeon 의 숫자 0~100. 우리는 단계(칸)로 센다 |
| Frostpunk: 원형 게이지를 화면 위 가운데에 크게. 다음 단계까지 남은 턴을 원 아래 숫자로 | Frostpunk 의 여러 겹 정보(기온 숫자·예보). 게이지 하나에 숫자 하나만 |

- Synty: `Frame_Ring` 틀 + 안쪽 달 원. 칸 수만큼 테두리를 나눈다. 꽉 차면 `Greeble_DemonSkull` 계열을 둘레에 붙인다 (적의 상징이 아군 화면에 넘어오는 순간)
- 보름달 흡혈 대상 아군에게는 `Status_Targeted_01` 아이콘을 머리 위에. 이것이 이 게임의 유일한 행동 예고다 (combat.md)

### 5-2. 유닛 정보 패널 (선택·호버, 1단계)

| 가져온다 | 가져오지 않는다 |
|---|---|
| Into the Breach: 화면 구석 고정 위치, 체력은 칸(pip)으로, 이동력은 큰 숫자 하나 | Into the Breach 의 적 행동 화살표. 하수인·스폰은 행동 예고가 없다 (combat.md) |
| Gloomhaven: 적은 아이콘+숫자 줄로 스펙을 한눈에 (이동·공격·사거리·방어) | Gloomhaven 의 주도권(initiative) 숫자·턴 순서. 진영 턴이다 (ui.md) |

- 아군 패널: 장식 없는 `Frame_Box_Medium`, 쇠색. 왼쪽 아래
- 적 패널: 같은 크기, 핏빛 틀에 모서리 `Greeble_Spikes`. 오른쪽 아래. **이동력을 반드시 보인다** (ui.md "스펙에는 이동력도")
- 스폰 패널: 아군 틀 모양 + 보라색 + 원래 병종 이름. 방어 0·원거리 불가를 빗금으로 (units.md)
- 아이콘: `Stat_Health_01`(체력), `Stat_Speed_01`(이동), `Stat_Strength_01`(공격), `Status_Defense_01`(방어), 무기는 `ICON_SM_Wep_*`(창 `Pitchfork`·`Polearm`, 궁수 `Crossbow` 등)

### 5-3. 머리 위 표시 (보드 위, 1단계)

| 가져온다 | 가져오지 않는다 |
|---|---|
| XCOM 2: 머리 위 칸 체력 막대, 진영 색 | XCOM 2 의 명중률·엄폐 방패. 무작위가 없다 (overview.md) |
| Into the Breach: 피해 미리 보기 때 잃을 칸만 깜빡인다 | |

- Synty: `HUD_DarkFantasy_WorldSpace_NameAlly_01` / `NameEnemy_01` 을 줄여 칸 막대만 남긴다
- 평소엔 체력 칸만, 호버한 유닛만 이름·숫자까지 (3단계로 접음)

### 5-4. 피해 미리 보기 (공격 대상 호버, 1단계)

| 가져온다 | 가져오지 않는다 |
|---|---|
| Into the Breach: 결과를 보드 위에 바로 (깜빡이는 칸, "처치" 표시) | 확률 표시 |
| XCOM 2: 대상 옆 작은 말풍선에 결과 숫자 하나 | XCOM 2 의 큰 하단 확인 패널 |

- 대상 머리 위 칸 막대에서 잃을 칸이 깜빡이고, 옆에 `-5` 같은 숫자. 처치되면 `Status_Dead_01`. 그 처치로 스폰이 생기면 보라 `Status_Vampiric_01` (vampire.md "연쇄 붕괴의 위협"을 미리 보여 줌)

### 5-5. 행동 버튼·턴 종료 (화면 아래 가운데·오른쪽 아래, 1단계)

| 가져온다 | 가져오지 않는다 |
|---|---|
| XCOM 2: 선택 유닛의 행동을 아래 가운데 한 줄로, 단축키 표시 | 버튼마다 긴 설명 |
| Desperados III: 버튼은 작고, 호버해야 설명 | |

- Synty: `Button_DarkFantasy_ActionBar_Item` 줄, 단축키는 `Input_DarkFantasyMenus_Hotkey_Prompt`
- 턴 종료는 오른쪽 아래에 홀로, 가장 큰 버튼. 행동 안 한 아군이 남았으면 숫자 배지

### 5-6. 전황·전투 기록 (화면 가장자리, 2단계)

| 가져온다 | 가져오지 않는다 |
|---|---|
| Desperados III: 가장자리에 좁고 반투명, 보드를 가리지 않는다 | |
| Darkest Dungeon II: 장식 없이 글자 위계만으로 정리 | |

- 위 왼쪽: 턴 수, 아군 수, 적 수, 스폰 수. 작은 숫자 + 아이콘, 틀 없음
- 오른쪽 위: 전투 기록, 접을 수 있음. 아군 행동은 뼈빛, 적은 핏빛, 변이는 보라 한 줄

### 5-7. 지형·범위 표시 (보드 위)

| 가져온다 | 가져오지 않는다 |
|---|---|
| Gloomhaven: 이동 가능 칸은 칸 테두리만 밝힌다 | 적 위협 범위를 늘 깔아 두는 것 (ui.md "위협 범위는 직접 제공하지 않는다") |
| Desperados III: 키를 누르는 동안만 보이는 정보 → 적 이동력 숫자를 모든 적 머리 위에 한꺼번에 (플레이어가 위험 칸을 스스로 셈) | Desperados III 의 시야 원뿔 그 자체 |

- 이동 칸 은빛 테두리, 공격 가능 대상 핏빛 테두리, 광원 칸은 보드 아트(art-assets.md)가 이미 보이므로 UI 는 덧칠하지 않는다

### 5-8. 뱀파이어 보스 체력 (화면 위, 보름달 게이지 아래, 2단계)

| 가져온다 | 가져오지 않는다 |
|---|---|
| Diablo IV: 화면 위 가운데 긴 보스 막대, 이름을 위에 | 보스 막대의 단계 표시(phase)를 화려하게 |

- Synty: `HUD_DarkFantasy_Event_BossBar` 계열. 이 게임에서 악마 장식이 가장 많은 자리
- 보름달 게이지와 붙여 "적의 상징" 한 덩어리로 만든다

### 5-9. 메뉴 화면 (메인 메뉴·장 선택·병영)

| 가져온다 | 가져오지 않는다 |
|---|---|
| The Banner Saga: 원정대가 지나온 길로 장을 고른다 → 3D 원정 지도의 길 (2-1) | |
| Diablo IV: 메뉴는 문장·장식을 크게 써도 된다 (전투 중이 아님) | 메뉴 배경 영상 |

- Synty: `Crest_DarkFantasyMenus_*`, `Panel_DarkFantasyMenus_Popup`(확인 창), `Component_DarkFantasyMenus_Section_Break`
- 메뉴는 아군 쪽 화면이라 은빛 쇠가 바탕. 백작의 성(마지막 장)만 핏빛 문장
- 장 선택 화면의 배치는 2-1~2-3 을 따른다 (#43). 3D 지도는 docs/art/campaign-map.md

### 5-10. 글꼴 위계

| 가져온다 | 가져오지 않는다 |
|---|---|
| Darkest Dungeon: 제목만 장식 글꼴, 수치는 읽기 쉬운 굵은 글꼴 | 본문까지 장식 글꼴 |
| Into the Breach: 1단계 숫자는 크고 굵게, 설명은 작게 | |

- 제목 Nanum Myeongjo, 본문 IBM Plex Sans KR (#16). 영문 장식·숫자에만 키트 글꼴 LT Museum

## 6. 이번에 못 본 것

- gameuidatabase.com 은 봇 확인 창이 떠서 열지 못했다. Interface In Game 에는 Baldur's Gate 3·파이어 엠블렘·소녀전선 2·Civilization VII 화면이 없다. 이 작품들은 위 표의 다른 링크로 봤다.
- 소녀전선 2 의 장 선택·브리핑 화면, 풍화설월 세계 지도 그림, Civilization VII 화면 캡처는 직접 보지 못했다. 위 표에서 이 화면은 글 설명만 썼다.
