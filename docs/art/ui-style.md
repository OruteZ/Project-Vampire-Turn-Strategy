# UI 스타일 — 달과 피

> 출처: docs/design/ui.md, docs/design/art.md, docs/design/combat.md, docs/design/vampire.md, docs/design/units.md, docs/design/overview.md (커밋 f7fc7f8). 테마 결정: docs/areas/ui.md 결정 표 "달과 피" (#24)
> 목적: 모든 화면이 같이 쓰는 색·글꼴·틀·버튼·아이콘·배치 규칙을 정한다. 화면별 기획서(docs/ui/*.md)는 이 문서의 이름(토큰)으로 말한다.
> 바탕 근거: docs/art/ui-references.md

## 1. 기준

1. 기준 해상도 **1920×1080**. CanvasScaler 는 Scale With Screen Size, Match 0.5. (지금 코드는 1600×900. 바꾼다)
2. 화면 가장자리 안전 여백 **24px**. 이 안에만 UI 를 둔다.
3. 보드 영역: 위 **150px**, 아래 **190px**, 왼쪽·오른쪽 **360px** 를 뺀 가운데. 전투 화면 UI 는 보드 영역을 가리지 않는다. 툴팁·미리 보기·머리 위 표시만 예외다.
4. 모든 수치는 1920×1080 기준 px 이다.

## 2. 색

### 2-1. 토큰

| 토큰 | 값 | 쓰는 곳 |
|---|---|---|
| `bg.night` | `#12131a` | 화면 바탕, 보드 밖 |
| `bg.panel` | `#1a1c25` 불투명도 92% | 패널 안쪽 |
| `bg.panelDeep` | `#0f1016` 불투명도 92% | 패널 안 칸(수치 줄 바탕) |
| `text.1` | `#ebe3d2` (뼈빛) | 1단계 글자 |
| `text.2` | `#b9b2bf` | 2단계 글자 |
| `text.3` | `#7d7684` | 3단계 글자, 비활성 |
| `ally` | `#8fb0cc` (은빛 쇠) | 아군 틀·강조·이동 칸 |
| `ally.dim` | `#4a5d6e` | 아군 틀 기본 상태, 행동 끝난 아군 |
| `enemy` | `#c8304a` (피) | 적 틀·강조·공격 대상 |
| `enemy.dim` | `#6e1e2c` | 적 틀 기본 상태 |
| `spawn` | `#a650c4` (보라) | 스폰, 변이 경고 |
| `moon` | `#eadcae` (창백한 금) | 보름달 게이지의 찬 칸 |
| `moon.blood` | `#9e1b2f` | 보름달이 꽉 찼거나 다음 턴일 때 게이지·가장자리 번짐 |
| `light` | `#e3a84b` (횃불) | 광원 관련 글자·아이콘, 재화(금화·은·인구) 숫자 |
| `line` | `#3a3442` | 구분선, 비활성 틀 |

- 지금 코드 색(`Ui.cs`)과 대응: `Ink`→`bg.night`, `Bone`→`text.1`, `Muted`→`text.2`, `Steel`→`ally`, `Blood`→`enemy`, `Spawn`→`spawn`, `Sun`→`moon`, `Candle`→`light`. 병종 색(`Peasant`·`Knight`·`Hunter`)은 UI 에서 쓰지 않고 보드 위 유닛 받침에만 남긴다.

### 2-2. 진영 규칙

1. **한 패널에는 한 진영 색만** 쓴다. 아군 패널은 `ally`, 적 패널은 `enemy`, 스폰 패널은 `spawn`.
2. 진영이 없는 정보(턴 수, 규칙, 메뉴)는 `ally` 틀을 쓴다. 이 게임의 화면은 토벌대의 것이다.
3. 적 색 `enemy` 는 적 정보에만 쓴다. 아군 패널의 위험 경고(보름달 표적, 체력 낮음)도 `enemy` 글자로 쓰되 틀은 아군 틀 그대로 둔다.
4. `light` 는 진영이 아니다. 광원·재화에만 쓴다.

## 3. 중요도 3단계 (ui.md "중요도 차등")

| | 1단계: 지금 결정 | 2단계: 참고 | 3단계: 배경 |
|---|---|---|---|
| 무엇 | 선택 유닛·호버 대상의 체력·이동력·공격력, 피해 미리 보기, 보름달 게이지, 턴 종료 | 전황 수치(턴·아군·적·스폰), 보스 체력, 전투 기록 최근 3줄, 패널 안 부가 수치(사거리·방어) | 규칙 요약, 안내 문구, 전투 기록 지난 줄 |
| 글자 | `num.1` / `h.2`, `text.1` | `body`, `text.2` | `small`, `text.3` |
| 틀 | 진영 색 틀, 밝은 상태 | 장식 없는 얇은 틀 또는 틀 없음 | 틀 없음, 접혀 있음 |
| 자리 | 화면 아래 모서리·위 가운데 | 화면 위 모서리 | 버튼을 눌러야 펼침 |
| 움직임 | 바뀌면 0.2초 번쩍임 | 없음 | 없음 |

- 한 화면에 1단계 요소가 동시에 **5개를 넘지 않게** 한다. 넘으면 화면별 기획서에서 하나를 2단계로 내린다.

## 4. 글꼴

| 토큰 | 글꼴 | 크기 | 굵기 | 쓰는 곳 |
|---|---|---|---|---|
| `display` | Nanum Myeongjo | 56 | ExtraBold | 메뉴 제목, 승리·패배 |
| `h.1` | Nanum Myeongjo | 32 | Bold | 화면 제목, 팝업 제목 |
| `h.2` | Nanum Myeongjo | 22 | Bold | 패널 제목(유닛 이름) |
| `num.1` | IBM Plex Sans KR | 34 | Bold | 1단계 숫자(체력·이동력·보름달 남은 턴) |
| `num.2` | IBM Plex Sans KR | 20 | SemiBold | 2단계 숫자 |
| `body` | IBM Plex Sans KR | 17 | Regular | 본문, 버튼 글자 |
| `small` | IBM Plex Sans KR | 14 | Regular | 3단계, 단축키, 아이콘 밑 이름 |

1. 가장 작은 글자는 **14px**. 더 작게 쓰지 않는다.
2. 줄 간격 1.35, 패널 제목 아래 여백 8px.
3. 숫자 옆 단위·이름은 숫자의 절반 크기, `text.2`.
4. 키트 글꼴 LT Museum 은 영문 장식(메뉴 제목의 로마 숫자 장 번호 등)에만. 한글이 섞이는 자리에는 쓰지 않는다.
5. 글꼴 파일은 #16. 그 전에는 지금처럼 OS 글꼴을 쓴다.

## 5. 틀

모두 `Assets/Synty/InterfaceDarkFantasyMenus/Sprites/DarkFantasyMenus/` 의 9-slice 스프라이트를 `Image.Type = Sliced` 로 쓰고, Image 색으로 물들인다.

| 틀 토큰 | 스프라이트 | 색 (기본 / 강조) | 장식 | 안쪽 여백 |
|---|---|---|---|---|
| `frame.ally` | `Frame_Box_Medium_02` + `_Background` | `ally.dim` / `ally` | 없음 | 16 |
| `frame.enemy` | `Frame_Box_Medium_02` + `_Background` | `enemy.dim` / `enemy` | 위 두 모서리에 `Greeble_Spikes` 32px | 16 |
| `frame.spawn` | `Frame_Box_Medium_02` + `_Background` | `spawn` 60% / `spawn` | 없음. 왼쪽 위 모서리에 `Status_Vampiric_01` 아이콘 24px | 16 |
| `frame.boss` | `Frame_Bar_Medium_01` + `_Background` | `enemy` | 양 끝에 `Greeble_DemonSkull` 계열 48px | 가로 24, 세로 8 |
| `frame.menu` | `Frame_Box_Large_01` + `_Background` | `ally` | 메뉴 화면만 `Crest_DarkFantasyMenus_*` 를 위 가운데에 | 32 |
| `frame.popup` | `Frame_Box_Large_01` + `_Background` | `ally` / 위험한 확인은 `enemy` | 없음 | 32 |
| `frame.tip` | 틀 없음, `bg.panelDeep` 사각 + 1px `line` 테두리 | — | 없음 | 10 |

1. 틀 굵기는 화면에서 **6~10px** 로 보이게 `Image.pixelsPerUnitMultiplier` 를 맞춘다 (스프라이트의 9-slice 테두리가 180~380px 라 그대로 쓰면 너무 두껍다).
2. `_Background` 스프라이트는 `bg.panel` 로 물들인다.
3. 악마 장식(`Greeble_*`)은 `frame.enemy`, `frame.boss`, 그리고 보름달 게이지가 꽉 찼을 때(7-2)에만 쓴다. 아군 쪽에는 쓰지 않는다.
4. 예외: 보름달 게이지 고리 `Frame_Ring_01` 은 스프라이트 자체에 가시가 있다. 게이지는 진영이 없는 화면 상징이라 이 가시는 늘 둔다. 악마 장식으로 치지 않는다.

## 6. 버튼

| 버튼 토큰 | 바탕 | 글자 | 쓰는 곳 | 지금 코드 `Look` |
|---|---|---|---|---|
| `btn.primary` | `Frame_Box_Medium_02_Background` 를 `ally` 로 채움 + `Frame_Box_Medium_02` 틀 `text.1` | `body` Bold, `bg.night` | 턴 종료, 출전, 사기 | `Primary` |
| `btn.normal` | `Frame_Box_Medium_02` + `_Background`, `bg.panel` 채움 + `ally.dim` 틀 | `body`, `text.1` | 보통 행동 | `Normal`, `Choice` |
| `btn.ghost` | 틀 없음 | `body`, `text.2` | 나가기, 접기·펼치기, 취소 | `Ghost` |
| `btn.danger` | `Frame_Box_Medium_02` + `_Background`, `bg.panel` 채움 + `enemy.dim` 틀 | `body`, `enemy` | 아군 직접 처치 | `Danger` |
| `btn.slot` | `Frame_Box_Medium_02` + `_Background` 64×64, 아이콘 40px | 아래에 `small` 행동 이름 | 행동 줄 | — |
| `btn.roster` | `frame.ally` 축소판, 초상 + 이름 | `small` | 배치 화면 유닛 목록 | `Roster` |

### 상태

| 상태 | 바뀌는 것 |
|---|---|
| 기본 | 위 표대로 |
| 호버 | 틀이 강조 색(`ally`/`enemy`)으로, 밝기 +10%. 0.08초 |
| 눌림 | 크기 97%, 0.05초 |
| 선택됨 (`DangerOn`, `RosterOn` 등) | 틀이 강조 색 + 바깥 2px 빛(같은 색 40%) |
| 비활성 | 전체 불투명도 40%, 글자 `text.3`, 호버 반응 없음. 왜 비활성인지 호버 툴팁으로 (예: "인구가 모자랍니다") |

- 키보드 단축키가 있는 버튼은 버튼 오른쪽 아래에 18×18 `frame.tip` 칸을 두고 키를 `small` 로 쓴다 (ui.md 조작 환경: 키보드·마우스). 키트에 단축키 스프라이트는 없다.
- 행동 줄 슬롯의 단축키는 왼쪽부터 숫자 `1`~`N`. 턴 종료는 `Space`.

## 7. 보름달 게이지

### 7-1. 모양

1. 화면 위 가운데. `Frame_Ring_01` 지름 **112px**, 위 여백 24px.
2. 원 안을 `Rules.MoonMax` 칸(지금 6)으로 나눈 부채꼴. 찬 칸은 `moon`, 빈 칸은 `bg.panelDeep`. 칸 수는 데이터에서 읽는다.
3. 원 아래에 `num.2` 로 "달 {Moon}/{MoonMax}", 그 아래 `small` `text.2` 로 상태 한 줄:
   - 뱀파이어가 잠들어 있으면 "{MoonWake}에 관이 열림"
   - 다음 적 턴이 보름달이면 "다음 턴 보름달" (`moon.blood`)
   - 그 밖에는 비움
4. 뱀파이어가 잠든 동안 `MoonWake` 칸 경계에 작은 관 표시(`line` 색 짧은 막대)를 둔다.

### 7-2. 단계별 변화

| 상태 | 게이지 | 화면 가장자리 |
|---|---|---|
| 달 < MoonMax − 1 | 찬 칸 `moon` | 없음 |
| 다음 적 턴이 보름달 | 찬 칸이 `moon.blood` 로, 1.2초 주기로 밝기 ±10% 맥박 | 화면 네 가장자리에 `moon.blood` 비네트, 불투명도 8%, 폭 80px |
| 보름달 턴 (적 턴 중) | 전부 `moon.blood`, 둘레에 `Greeble_DemonSkull` 4개 | 비네트 15% |
| 보름달이 지나 0 으로 | 0.4초에 걸쳐 칸이 비워짐 | 비네트 0.4초에 사라짐 |

- 비네트 불투명도는 화면에 보이는 값이다. 선형 색 공간에서 섞이면 진해 보이니, 구현은 스크린샷으로 맞춘다.
- 보름달 표적 아군: 머리 위에 `Status_Targeted_01` 32px `enemy` 색, 칸 테두리 `enemy`. 이것이 이 게임의 유일한 행동 예고다 (combat.md, vampire.md).

## 8. 보드 위 표시

### 8-1. 머리 위 체력

1. 유닛 머리 위에 체력 칸 막대. 칸 하나 = 체력 1. 칸 크기 6×8px, 간격 1px. 체력이 12 를 넘으면 칸 하나 = 체력 2 로 바꾸고 막대 끝에 숫자를 붙인다.
2. 찬 칸 색: 아군 `ally`, 적 `enemy`, 스폰 `spawn`. 빈 칸 `bg.panelDeep`.
3. 평소에는 칸 막대만. 호버하거나 선택한 유닛만 막대 위에 이름(`small`)과 숫자. 이 유닛의 머리 위 표시는 다른 유닛 것보다 위에 그린다.
4. 바탕은 `HUD_DarkFantasy_WorldSpace_NameAlly_01` / `NameEnemy_01` 에서 이름 칸을 끄고 막대만 남겨 쓴다.
5. 칸 하나 = 체력 2 일 때 붙는 숫자는 막대 오른쪽 4px 에 붙인다.
6. 보드 위에 글자 라벨(예: "다음 턴 보름달", "보름달 표적")을 두지 않는다. 같은 정보는 보름달 게이지 상태 줄, 표적 아이콘, 아군 패널 경고 줄이 준다.

### 8-2. 칸 표시

| 표시 | 모양 |
|---|---|
| 이동 가능 칸 | 칸 테두리 `ally` 2px, 안쪽 채움 `ally` 8% |
| 공격 가능 대상 칸 | 칸 테두리 `enemy` 2px |
| 호버 칸 | 테두리 `text.1` 2px |
| 보름달 표적 칸 | 7-2 |
| 직접 처치 가능한 아군 칸 | 테두리 `enemy` 점선 |
| 흡혈 범위 칸 (다음 적 턴이 보름달일 때만) | 테두리 `moon.blood` 1px 실선, 안쪽 채움 `moon.blood` 5% |
| 광원 칸 | UI 로 덧칠하지 않는다. 보드 아트(docs/areas/art-assets.md)가 이미 보인다 |

### 8-3. 피해 미리 보기 (ui.md "계산 도움")

공격 대상을 호버하면:
1. 대상 머리 위 막대에서 잃을 칸이 0.5초 주기로 깜빡인다.
2. 막대 오른쪽에 `num.2` 로 "−{피해}", `enemy` (아군이 맞으면) 또는 `text.1` (적이 맞으면).
3. 처치되면 숫자 대신 `Status_Dead_01` 24px.
4. 그 처치로 스폰이 생기면 `Status_Vampiric_01` 24px `spawn` 을 하나 더. 확률 표시는 없다 (overview.md 무작위 없음).

### 8-4. 적 이동력 한꺼번에 보기

1. **Alt 키를 누르는 동안** 모든 적 머리 위 막대 위에 이동력 숫자를 `num.2` `enemy` 로, `Stat_Speed_01` 아이콘 20px 과 함께 보인다.
2. 위험한 칸을 칠해 주지는 않는다. 플레이어가 이 숫자로 스스로 센다 (ui.md "위협 범위는 직접 제공하지 않는다").
3. 지금 데모의 "적 위협 범위" 체크박스를 이것으로 바꾼다. 적을 살펴볼 때 칠하던 위협 칸과 그 안내 문구도 없앤다.

## 9. 아이콘

`Assets/Synty/InterfaceDarkFantasyHUD/Sprites/` 의 아이콘을 쓴다. 패널 안은 `_Clean`, 보드 위는 `_Stroke` (바탕에 묻히지 않게).

| 뜻 | 아이콘 |
|---|---|
| 체력 | `Icons_Stats/ICON_DarkFantasy_Stat_Health_01` |
| 이동력 | `Icons_Stats/ICON_DarkFantasy_Stat_Speed_01` |
| 공격력 | `Icons_Stats/ICON_DarkFantasy_Stat_Strength_01` |
| 방어력 | `Icons_Status/ICON_DarkFantasy_Status_Defense_01` |
| 사거리 | `Icons_Stats/ICON_DarkFantasy_Stat_Accuracy_01` |
| 보름달 표적 | `Icons_Status/ICON_DarkFantasy_Status_Targeted_01` |
| 스폰·변이 | `Icons_Status/ICON_DarkFantasy_Status_Vampiric_01` |
| 처치 | `Icons_Status/ICON_DarkFantasy_Status_Dead_01` |
| 병종 무기 | `Icons_Weapons/` : 농민 창병 `ICON_SM_Wep_Pitchfork_01_DarkFantasy`, 농민 궁수·궁수 `ICON_SM_Wep_Crossbow_01_Arrow_01_DarkFantasy`(키트에 활이 없어 화살), 검사 `ICON_SM_Wep_Sword_01_DarkFantasy`, 기사 `ICON_SM_Wep_Shield_Heater_01_Dungeon`, 사냥꾼 `ICON_SM_Wep_Crossbow_01_DarkFantasy`, 스켈레톤 `ICON_SM_Wep_Bone_01_Dungeon`, 뱀파이어 `ICON_SM_Wep_Scythe_01_DarkFantasy` |

- 크기: 패널 안 수치 옆 24px, 행동 줄 40px, 보드 위 20~32px.
- 색: 아이콘은 그 수치의 진영 색으로 물들인다. 광원·재화 아이콘만 `light`.

## 10. 전투 화면 배치

```
┌──────────────────────────────────────────────────────────────┐
│ [전황: 턴·아군·적·스폰]       ( 보름달 )        [전투 기록 ▾] │
│  2단계, 틀 없음            [ 뱀파이어 체력 막대 ]   2단계, 접힘 │
│                                                              │
│                                                              │
│                         보 드 영 역                          │
│                                                              │
│                                                              │
│ ┌──────────────┐                           ┌──────────────┐  │
│ │ 아군 패널     │    [행동 줄: 슬롯 ×N]     │ 적 패널       │  │
│ │ frame.ally   │                           │ frame.enemy  │  │
│ └──────────────┘ [규칙 ?]     [나가기]      └──────────────┘ [턴 종료] │
└──────────────────────────────────────────────────────────────┘
```

| 자리 | 요소 | 크기 | 단계 |
|---|---|---|---|
| 위 가운데 | 보름달 게이지 (7) | 지름 112 | 1 |
| 보름달 아래 | 뱀파이어 체력 `frame.boss`. 뱀파이어가 깨어난 뒤에만 | 560×40 | 2 |
| 왼쪽 위 | 전황: `num.2` 숫자 + 아이콘 20px 네 개를 가로로, 틀 없음 | 높이 48 | 2 |
| 오른쪽 위 | 전투 기록: 접힌 상태 최근 3줄, 펼치면 320×400 `frame.tip`. 글자는 `body` `text.2`, 줄마다 한 줄로 넘치면 말줄임. 진영 색은 줄 앞 아이콘 16px 에만 | 폭 320 | 2·3 |
| 왼쪽 아래 | 아군 패널: 선택한(없으면 호버한) 아군. 이름 `h.2`, 체력·이동력 `num.1`, 공격·방어·사거리 `num.2`, 경고 줄 | 340×190 | 1 |
| 오른쪽 아래 | 적 패널: 호버한 적(뱀파이어 포함). 같은 구성, `frame.enemy`. 스폰은 `frame.spawn` | 340×190 | 1 |
| 아래 가운데 | 행동 줄 `btn.slot` | 슬롯당 64, 간격 8 | 1 |
| 오른쪽 아래 끝 | 턴 종료 `btn.primary` 200×56. 행동하지 않은 아군이 있으면 왼쪽 위에 숫자 배지 | 200×56 | 1 |
| 왼쪽 아래 끝 | 규칙 요약 `btn.ghost` "?" (펼치면 `frame.popup`), 나가기 `btn.ghost` | | 3 |
| 아래 가운데 위 | 안내 문구 한 줄 `small` `text.3` (지금 화면 맨 아래 문구) | | 3 |

- 지금 데모의 오른쪽 세 패널(아군 페이즈·전투 기록·규칙 요약)은 위 배치로 나눈다: 페이즈 이름은 턴 종료 버튼 위 `small` 글자로, 전투 기록은 오른쪽 위, 규칙 요약은 "?" 버튼으로.
- 패널이 비었을 때(선택·호버 없음) 아군·적 패널은 숨긴다.

## 11. 메뉴 화면 공통

1. 바탕은 `bg.night` 위에 그 장의 보드 아트를 흐리게(블러 + 어둡게 60%) 깐다. 메인 메뉴는 첫 장 보드.
2. 틀은 `frame.menu`. 장식은 위 가운데 `Crest` 하나만.
3. 마지막 장(백작의 성)과 관련된 카드·문장만 `enemy` 색. 나머지 메뉴는 `ally`.
4. 강화·구매 확인은 `frame.popup` (`Panel_DarkFantasyMenus_Popup` 바탕).
5. 장 사이 구분선은 `Component_DarkFantasyMenus_Section_Break`.

## 12. 움직임

| 무엇 | 시간 |
|---|---|
| 버튼 호버·눌림 | 0.08초 / 0.05초 |
| 패널 나타남·사라짐 | 0.15초, 아래에서 8px 올라오며 |
| 1단계 수치가 바뀜 | 0.2초 번쩍임(밝기 +30% → 원래) |
| 보름달 맥박 | 1.2초 주기 |
| 화면 전환 | 0.3초 검게 |

- 이보다 긴 연출은 넣지 않는다. 한 전투 10~20분 동안 거의 모든 턴에 깊게 생각해야 하므로(combat.md), UI 가 생각을 끊지 않게 한다.

## 13. 스타일 타일 목업 (파이프라인 5단계)

구현 에이전트가 이 문서만 보고 만든다. 새 씬을 만들지 않고, 데모 전투 화면을 이 스타일로 바꾼 상태를 찍는다.

1. 담을 것: 보름달 게이지("다음 턴 보름달" 상태), 뱀파이어 체력 막대, 전황, 접힌 전투 기록, 아군 패널(보름달 표적이 된 아군 선택), 적 패널(스폰 호버), 행동 줄, 턴 종료, 피해 미리 보기 한 번, Alt 이동력 보기 한 번.
2. 스크린샷 1920×1080: `docs/review/ui-style-tile-1.png`(평소), `-2.png`(피해 미리 보기), `-3.png`(Alt).
3. 확인할 것: 틀을 물들였을 때 스프라이트에 원래 들어 있는 붉은·녹색 기가 아군 틀에서 거슬리는지. 거슬리면 보고하고, 스타일 기획서에서 다른 틀 스프라이트로 바꾼다.

## 정한 세부 (docs/design 에 없는 것)

- **기준 해상도 1920×1080.** 근거: PC 패키지 게임의 가장 흔한 해상도. 지금 1600×900 은 HTML 데모 크기였다.
- **보름달 칸 수 = `Rules.MoonMax`.** 근거: 지금 규칙(적 턴·죽음마다 1, MoonMax 에서 보름달)을 그대로 보이게 한다. 수치가 바뀌어도 UI 가 따라간다.
- **적 위협 범위 체크박스를 Alt 이동력 보기로 바꾼다(8-4).** 근거: ui.md "위협 범위는 직접 제공하지 않는다", "플레이어가 위험한 타일을 스스로 계산할 수 있어야 한다". 이동력 숫자는 ui.md 가 공개하라고 한 스펙이다.
- **스폰 패널은 원래 병종 이름 + "스폰" 을 쓴다** (예: "기사 · 스폰"). 근거: units.md 의 "원본의 공격력과 체력을 계승"과 vampire.md 의 "주력 유닛일수록 더 강한 적"을 패널에서 바로 읽게 한다. 초상은 보드 모델이 이미 원래 병종이라 따로 두지 않는다.
- **1단계 요소 동시 5개 이하.** 근거: ui.md "많은 정보가 한 화면에 있더라도, 중요도 차등에 따라 분리되어 보이게". 하나의 화면에서 첫눈에 구분되는 강조는 몇 개를 넘으면 다 같아 보인다.
- **최소 글자 14px.** 근거: 1080p 모니터에서 한글 명조·고딕이 뭉개지지 않는 크기.

- **방향 검토(6단계, 커밋 564f2f9 목업) 반영.** 보스 체력 2단계(1단계 6개 → 5개), 흡혈 범위 칸 따로, 버튼·슬롯은 `Frame_Box_Medium_02`(키트에 버튼 스프라이트 없음), 슬롯 단축키 1~N, 전투 기록 조용하게, 위협 칸 제거(ui.md "위협 범위는 직접 제공하지 않는다").
- **비네트 8%·80px, 보드 위 글자 라벨 없음.** 사용자 결정 (#24 "추천대로").

## 열린 질문 (사용자가 정할 것)

없음.
