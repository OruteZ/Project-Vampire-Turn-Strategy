# UI 레퍼런스 — 달과 피

출처: docs/design/ui.md, docs/design/art.md, docs/design/combat.md, docs/design/vampire.md, docs/design/units.md (커밋 f7fc7f8). 테마 결정: docs/areas/ui.md 결정 표 "달과 피" (#24)

> 파이프라인 3단계 결과물. UI 요소마다 "어느 작품의 무엇을 가져오고, 무엇은 가져오지 않는지", 그리고 Synty 키트의 어떤 재료로 만들지 적는다. 4단계 UI 스타일 기획서(`docs/art/ui-style.md`)가 이 표를 바탕으로 수치·색·배치를 정한다.
> 이미지는 저장소에 넣지 않는다. 각 작품의 스토어 페이지 스크린샷으로 확인한다.

## 참고 작품

| 작품 | 링크 | 왜 보나 |
|---|---|---|
| Into the Breach | https://store.steampowered.com/app/590380/ | 작은 화면에 전술 정보를 담는 가독성의 기준. 칸 단위 체력, 피해 미리 보기 |
| Gloomhaven (디지털판) | https://store.steampowered.com/app/780290/ | 육각 보드, 적 능력치 카드. docs/design/art.md 의 맵 아트 참고작 |
| Desperados III | https://store.steampowered.com/app/610370/ | 보드를 가리지 않는 작은 HUD, 키를 누를 때만 보이는 정보. art.md 의 광원·시점 참고작 |
| Darkest Dungeon | https://store.steampowered.com/app/262060/ | 화면 위 가운데 횃불 게이지가 분위기와 규칙을 같이 바꾼다 → 보름달 게이지 |
| Darkest Dungeon II | https://store.steampowered.com/app/1940340/ | 같은 고딕 톤을 장식을 줄여 정리한 판 |
| Frostpunk | https://store.steampowered.com/app/323190/ | 화면 위 가운데 원형 게이지(기온)와 다가오는 위협 표시 |
| XCOM 2 | https://store.steampowered.com/app/268500/ | 화면 아래 가운데 행동 버튼 줄, 머리 위 칸 체력 |
| The Banner Saga | https://store.steampowered.com/app/237990/ | 원정대 느낌의 유닛 카드, 캠프 화면 |
| Diablo IV | https://store.steampowered.com/app/2344520/ | 다크 판타지 틀·글꼴을 절제해 쓰는 법, 보스 체력 막대 |

## 요소별 표

### 1. 보름달 게이지 (화면 위 가운데, 1단계)

| 가져온다 | 가져오지 않는다 |
|---|---|
| Darkest Dungeon: 게이지 하나가 화면 분위기를 바꾼다. 달이 찰수록 화면 가장자리에 핏빛이 번진다 | Darkest Dungeon 의 숫자 0~100. 우리는 단계(칸)로 센다 |
| Frostpunk: 원형 게이지를 화면 위 가운데에 크게. 다음 단계까지 남은 턴을 원 아래 숫자로 | Frostpunk 의 여러 겹 정보(기온 숫자·예보). 게이지 하나에 숫자 하나만 |

- Synty: `Frame_Ring` 틀 + 안쪽 달 원. 칸 수만큼 테두리를 나눈다. 꽉 차면 `Greeble_DemonSkull` 계열을 둘레에 붙인다 (적의 상징이 아군 화면에 넘어오는 순간)
- 보름달 흡혈 대상 아군에게는 `Status_Targeted_01` 아이콘을 머리 위에. 이것이 이 게임의 유일한 행동 예고다 (combat.md)

### 2. 유닛 정보 패널 (선택·호버, 1단계)

| 가져온다 | 가져오지 않는다 |
|---|---|
| Into the Breach: 화면 구석 고정 위치, 체력은 칸(pip)으로, 이동력은 큰 숫자 하나 | Into the Breach 의 적 행동 화살표. 하수인·스폰은 행동 예고가 없다 (combat.md) |
| Gloomhaven: 적은 아이콘+숫자 줄로 스펙을 한눈에 (이동·공격·사거리·방어) | Gloomhaven 의 주도권(initiative) 숫자·턴 순서. 진영 턴이다 (ui.md) |

- 아군 패널: 장식 없는 `Frame_Box_Medium`, 쇠색. 왼쪽 아래
- 적 패널: 같은 크기, 핏빛 틀에 모서리 `Greeble_Spikes`. 오른쪽 아래. **이동력을 반드시 보인다** (ui.md "스펙에는 이동력도")
- 스폰 패널: 아군 틀 모양 + 보라색 + 원래 병종 이름. 방어 0·원거리 불가를 빗금으로 (units.md)
- 아이콘: `Stat_Health_01`(체력), `Stat_Speed_01`(이동), `Stat_Strength_01`(공격), `Status_Defense_01`(방어), 무기는 `ICON_SM_Wep_*`(창 `Pitchfork`·`Polearm`, 궁수 `Crossbow` 등)

### 3. 머리 위 표시 (보드 위, 1단계)

| 가져온다 | 가져오지 않는다 |
|---|---|
| XCOM 2: 머리 위 칸 체력 막대, 진영 색 | XCOM 2 의 명중률·엄폐 방패. 무작위가 없다 (overview.md) |
| Into the Breach: 피해 미리 보기 때 잃을 칸만 깜빡인다 | |

- Synty: `HUD_DarkFantasy_WorldSpace_NameAlly_01` / `NameEnemy_01` 을 줄여 칸 막대만 남긴다
- 평소엔 체력 칸만, 호버한 유닛만 이름·숫자까지 (3단계로 접음)

### 4. 피해 미리 보기 (공격 대상 호버, 1단계)

| 가져온다 | 가져오지 않는다 |
|---|---|
| Into the Breach: 결과를 보드 위에 바로 (깜빡이는 칸, "처치" 표시) | 확률 표시 |
| XCOM 2: 대상 옆 작은 말풍선에 결과 숫자 하나 | XCOM 2 의 큰 하단 확인 패널 |

- 대상 머리 위 칸 막대에서 잃을 칸이 깜빡이고, 옆에 `-5` 같은 숫자. 처치되면 `Status_Dead_01`. 그 처치로 스폰이 생기면 보라 `Status_Vampiric_01` (vampire.md "연쇄 붕괴의 위협"을 미리 보여 줌)

### 5. 행동 버튼·턴 종료 (화면 아래 가운데·오른쪽 아래, 1단계)

| 가져온다 | 가져오지 않는다 |
|---|---|
| XCOM 2: 선택 유닛의 행동을 아래 가운데 한 줄로, 단축키 표시 | 버튼마다 긴 설명 |
| Desperados III: 버튼은 작고, 호버해야 설명 | |

- Synty: `Button_DarkFantasy_ActionBar_Item` 줄, 단축키는 `Input_DarkFantasyMenus_Hotkey_Prompt`
- 턴 종료는 오른쪽 아래에 홀로, 가장 큰 버튼. 행동 안 한 아군이 남았으면 숫자 배지

### 6. 전황·전투 기록 (화면 가장자리, 2단계)

| 가져온다 | 가져오지 않는다 |
|---|---|
| Desperados III: 가장자리에 좁고 반투명, 보드를 가리지 않는다 | |
| Darkest Dungeon II: 장식 없이 글자 위계만으로 정리 | |

- 위 왼쪽: 턴 수, 아군 수, 적 수, 스폰 수. 작은 숫자 + 아이콘, 틀 없음
- 오른쪽 위: 전투 기록, 접을 수 있음. 아군 행동은 뼈빛, 적은 핏빛, 변이는 보라 한 줄
- 지금 HTML 데모의 오른쪽 위 세 패널(아군 페이즈/전투 기록/규칙 요약)은 크고 보드와 겨룬다. 이 단계에서 줄인다

### 7. 지형·범위 표시 (보드 위)

| 가져온다 | 가져오지 않는다 |
|---|---|
| Gloomhaven: 이동 가능 칸은 칸 테두리만 밝힌다 | 적 위협 범위를 늘 깔아 두는 것 (ui.md "위협 범위는 직접 제공하지 않는다") |
| Desperados III: 키를 누르는 동안만 보이는 정보 → 적 이동력 숫자를 모든 적 머리 위에 한꺼번에 (플레이어가 위험 칸을 스스로 셈) | Desperados III 의 시야 원뿔 그 자체 |

- 이동 칸 은빛 테두리, 공격 가능 대상 핏빛 테두리, 광원 칸은 보드 아트(art-assets.md)가 이미 보이므로 UI 는 덧칠하지 않는다
- 지금 데모의 "적 위협 범위" 체크박스는 ui.md 와 부딪힌다. 4단계에서 "적 이동력 한꺼번에 보기" 키로 바꾸는 안을 쓴다

### 8. 뱀파이어 보스 체력 (화면 위, 보름달 게이지 아래, 1단계)

| 가져온다 | 가져오지 않는다 |
|---|---|
| Diablo IV: 화면 위 가운데 긴 보스 막대, 이름을 위에 | 보스 막대의 단계 표시(phase)를 화려하게 |

- Synty: `HUD_DarkFantasy_Event_BossBar` 계열. 이 게임에서 악마 장식이 가장 많은 자리
- 보름달 게이지와 붙여 "적의 상징" 한 덩어리로 만든다

### 9. 메뉴 화면 (메인 메뉴·스테이지 선택·강화, 2단계)

| 가져온다 | 가져오지 않는다 |
|---|---|
| The Banner Saga: 원정대가 지나온 길로 스테이지 선택, 유닛 카드는 초상 + 짧은 수치 | |
| Diablo IV: 메뉴는 문장·장식을 크게 써도 된다 (전투 중이 아님) | 메뉴 배경 영상·복잡한 3D 연출 |

- Synty: `Screen_DarkFantasyMenus_MainMenu`, `Crest_DarkFantasyMenus_*`, `Panel_DarkFantasyMenus_Popup`(강화 확인 창), `Component_DarkFantasyMenus_Section_Break`
- 메뉴는 아군 쪽 화면이라 은빛 쇠가 바탕. 백작의 성(마지막 스테이지)만 핏빛 문장

### 10. 글꼴 위계

| 가져온다 | 가져오지 않는다 |
|---|---|
| Darkest Dungeon: 제목만 장식 글꼴, 수치는 읽기 쉬운 굵은 글꼴 | 본문까지 장식 글꼴 |
| Into the Breach: 1단계 숫자는 크고 굵게, 설명은 작게 | |

- 제목 Nanum Myeongjo, 본문 IBM Plex Sans KR (#16). 영문 장식·숫자에만 키트 글꼴 LT Museum

## 4단계로 넘기는 열린 질문

- 보름달 게이지 칸 수와 핏빛 번짐 단계를 지금 보름달 규칙(docs/systems/)의 단계와 같게 할지
- "적 이동력 한꺼번에 보기" 키(7번)를 데모의 위협 범위 체크박스 대신 둘지
- 스폰이 원래 아군이었다는 것을 패널에서 얼마나 드러낼지 (원래 이름 / 초상 / 둘 다)
