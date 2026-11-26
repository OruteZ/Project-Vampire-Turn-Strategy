# 유닛 상세 창

> 출처: docs/design/ui.md, docs/design/units.md, docs/design/vampire.md (커밋 f7fc7f8). 이슈 #127
> 스타일: docs/art/ui-style.md (토큰 이름으로 쓴다). 함께 보는 기획서: docs/ui/battle-hud.md (유닛 패널 3-2, 편성 2-2), docs/ui/upgrade.md (병영), docs/ui/stage-select.md (장 정보)
> 레퍼런스: 아래 "레퍼런스" 절
> 목적: 유닛 하나(병종·적)를 자세히 보는 창이 어디서 열리고, 무엇을 담고, 어떻게 닫히는지 정한다. 기술 설명 문구도 여기서 정한다.

## 요구사항

### 1. 무엇인가

1. 화면 가운데에 뜨는 고정 크기 창 하나다. 모든 화면(전투·배치·장 정보·병영)에서 같은 창을 쓴다.
2. 읽기만 한다. 창 안에서 사거나 명령하지 않는다(강화 구매는 병영에 그대로).
3. 화면에는 이름·숫자·아이콘만 둔다는 원칙(ui-style.md 14)은 그대로다. 이 창은 규칙 요약 창처럼 "설명을 읽으려고 여는 곳" 이라 문장과 항목 이름을 쓴다. ui-style.md 16-2 의 "가운데 UI 를 두는 순간" 표에 "유닛 상세 창" 한 줄을 더한다.
4. 두 가지 판이 있다. 구성은 같고 값만 다르다.

| 판 | 열리는 곳 | 값 |
|---|---|---|
| 유닛 판 | 보드 위 유닛 (전투·배치) | 그 유닛의 지금 값: 지금 체력/최대 체력, 산 강화가 반영된 능력치, 지금 상태 |
| 병종 판 | 편성 줄, 장 정보의 출전 병종 칸, 병영 | 그 병종의 값: 산 강화가 반영된 능력치(체력은 최대만). "지금 상태" 구역 없음 |

### 2. 여는 법

| 화면 | 대상 | 여는 법 | 판 |
|---|---|---|---|
| 전투 (아군 페이즈), 배치 | 보드 위 유닛: 아군, 하수인, 스폰, 뱀파이어, 호위 대상 | 유닛 패널(battle-hud.md 3-2) 머리 줄 오른쪽 끝의 정보 버튼. 또는 그 유닛을 호버(없으면 선택)한 채 `I` | 유닛 판 |
| 배치 | 편성 줄의 병종 | 줄 오른쪽 아래의 정보 버튼. 또는 줄을 호버한 채 `I` | 병종 판 |
| 장 정보 (L2) | 출전 병종 칸 | 칸 클릭. 또는 칸을 호버한 채 `I` | 병종 판 |
| 병영 (L3) | 고른 병종 | 병종 머리 줄(upgrade.md 3) 오른쪽의 정보 버튼. 또는 `I` | 병종 판 |

1. 정보 버튼: 틀 없는 40×40, 아이콘 `Inventory_Notes_01` 22px `text.2`(호버 `text.1`), 안쪽 오른쪽 아래 단축키 칸 "I". 툴팁 `detail.open`. 전투 HUD 는 0.55배 판이라 그 판에서는 64×64(화면 35px)로 그린다. 자리: 유닛 패널의 체력·이동 줄 오른쪽 끝(이름 줄은 긴 이름이면 두 줄이 되므로 피한다), 편성 줄 오른쪽 아래, 병영 병종 이름 바로 뒤.
   - 전투 유닛 패널은 호버한 동안만 있으므로, 호버한 유닛(적 포함)의 버튼은 마우스로 누를 수 없다. 버튼은 `I` 키 안내를 겸하고, 누를 수 있는 것은 선택한 아군·배치 중 살펴본 유닛의 패널이다.
   - 배치 중 `I`: 편성 줄 위면 그 병종, 보드 유닛 위면 그 유닛, 아니면 살펴본 유닛, 그것도 없으면 지금 고른 병종.
2. 편성 줄의 오른쪽 클릭(그 병종 모두 회수, battle-hud.md 2-2 7)과 보드 클릭은 그대로다. 상세 창은 정보 버튼과 `I` 로만 연다.
3. 열 수 없는 때: 적 페이즈 재생 중(정보 버튼 비활성, `I` 무시), 결과 창·구매 확인 창·규칙 요약 창이 떠 있을 때.
4. 열지 않는 것: 시신(시신 패널이 일어날 스폰의 수치를 이미 보인다), 증원 카드, 잠긴(미해금) 병종 탭.

### 3. 닫는 법

1. Esc, `I`, 창 오른쪽 위 닫기 버튼, 창 밖(어두운 막) 클릭.
2. Esc 는 상세 창부터 닫는다(battle-hud.md 6 의 Esc 순서 맨 앞, stage-select.md 의 레이어 뒤로 가기보다 앞).
3. 창이 떠 있는 동안 뒤 화면은 입력을 받지 않는다(보드 클릭·호버, 1~5, Space, F, B, WASD, 화살표의 레이어 이동).
4. 닫으면 열기 전 그대로다(선택한 유닛, 처치·치료 모드, 고른 병종 탭).

### 4. 창

```
┌────────────────────────────────────────────────────────────────────────┐
│ ┌────┐                                                                 │
│ │아이콘│ 기사 2                   방어 전문 · 아군        ⛑ 3    [× Esc] │
│ └────┘                                                                 │
│ ──────────────────────────────────────────────────────────────────────│
│ 능력치                      │ 기술                                    │
│ ♥ 체력        14 / 22        │ [◆] 도발            지속 · 기본          │
│ ⚔ 공격         2             │     이 유닛을 칠 수 있는 적은 다른 아군    │
│ ⛨ 방어         1             │     대신 이 유닛만 공격한다. …            │
│ ➶ 이동         2             │ [◆] 돌격            지속 · 강화          │
│ ◎ 사거리       1             │     이번 턴에 2칸 이상 이동한 뒤 …        │
│                             │                                        │
│ 지금 상태                    │ 강화                                    │
│ [● 보름달 표적] [● 빛 속]     │ ✓ 백전노장    체력 +6          ⚉ 물려받음 │
│   └ 호버: 그 상태의 설명      │ ○ 판금 갑옷   방어 +1, 체력 +2           │
│ 스폰이 되면                  │ ✓ 돌격        기술: 돌격                 │
│ ♥22 ⚔2 ⛨0 ➶2 ◎1            │                                        │
│                             │                                        │
│ ──────────────────────────────────────────────────────────────────────│
│ ← → 다른 병종                                          I · Esc 닫기    │
└────────────────────────────────────────────────────────────────────────┘
```

1. 막: 화면 전체 `bg.night` 60% (upgrade.md 6-1 과 같다). 누르면 닫힌다.
2. 창: 화면 가운데 한 점 앵커, **960×680 고정**. 틀 `frame.popup`, 바탕 `bg.panel` 92% 아래에 `bg.night` 88% 를 한 겹 더 깐다(틀 바탕 그림만으로는 뒤 보드가 비쳐 긴 글이 안 읽혔다). 틀 색은 보는 유닛의 진영: 아군·호위 대상 `ally`, 하수인·뱀파이어 `enemy`(위 두 모서리 `Greeble_Spikes`), 스폰 `spawn`. 나타날 때 0.15초 감속.
3. 크기는 모든 화면에서 같다. 공용 패널(`Panel.asset`, 1920×1080) 기준 px 이고, 전투 HUD 의 0.55배 판(battle-hud.md 1-1)에 그리지 않는다.
4. 배치 (창 왼쪽 위 기준 px, 안쪽 여백 32):

| 구역 | x | y | 크기 | 내용 |
|---|---|---|---|---|
| 머리: 아이콘 | 32 | 32 | 72×72 | 병종 아이콘(ui-style.md 9) 52px, 진영 색. `bg.panelDeep` 칸 + 1px 진영 색 테두리. 하수인은 겉모습 `UnitLook.icon`(#150 보고서 C1 에서 장 정보 "나오는 적" 줄과 함께 넣었다), 그것이 없으면 `Map_Skull_01`, 뱀파이어 `Map_Monster_01`, 스폰은 원래 병종의 아이콘 |
| 머리: 이름 | 120 | 48 | 440×40 | `h.1` `text.1`, 아이콘 높이 가운데. 유닛 판은 유닛 이름("기사 2"), 스폰은 `hud.spawnOf`, 병종 판은 병종 이름. 이름 아래에 소개 글은 두지 않는다 |
| 머리: 역할 | 오른쪽 끝에서 200 | 54 | 자동×28 | 역할 · 진영 `small` 진영 색, 오른쪽 맞춤. 5절 |
| 머리: 비용 | 오른쪽 끝에서 88 | 52 | 자동×32 | 아군 병종만: 인구(은이 들면 은도) 아이콘 수치 `num.2` `light` |
| 닫기 | 888 | 16 | 40×40 | `btn.ghost`, `Menu_Cancel_01` 20px, 안쪽 단축키 "Esc" |
| 구분선 | 32 | 112 | 896×24 | `Section_Break_02`, `line` |
| 왼쪽 열 | 32 | 148 | 336×468 | 능력치(6절) → 지금 상태(7절) → 스폰이 되면(8절). 구역 사이 20 |
| 오른쪽 열 | 396 | 148 | 532×468 | 기술(9절) → 강화(10절, 아군) 또는 특성(11절, 적) |
| 바닥 줄 | 32 | 634 | 896×24 | 왼쪽: 병종 판에서 `detail.browse` "← → 다른 병종". 오른쪽: `detail.closeHint` "I · Esc 닫기". `small` `text.3` |

5. 두 열은 내용이 넘치면 그 열만 세로로 스크롤한다.
6. 구역 제목("능력치", "기술" 등)은 `small` Bold `text.3`, 아래 1px `line`, 아래 여백 8.

### 5. 머리의 글

병종 소개 글(`unit.<key>.desc`, 적 노트)은 창에 쓰지 않는다. 이름 옆 역할 낱말이 그 자리를 맡는다.

| 무엇 | 아군 병종 | 하수인·정예·중간보스 | 뱀파이어 | 스폰 |
|---|---|---|---|---|
| 역할 | `role.<key>` (아래 표) · `unit.card.ally` | `unit.card.minion` (정예·중간보스도 같다. 데이터에 등급 칸이 없다) | `unit.card.boss` | `unit.card.spawn` |

역할 낱말 (새 키 `role.<key>`, units.md "역할 구분" 표를 따른다):

| key | 한국어 | English |
|---|---|---|
| pspear | 소모품 | Fodder |
| parcher | 싼 원거리 | Cheap Ranged |
| sword | 공격 전문 | Striker |
| archer | 원거리 | Ranged |
| knight | 방어 전문 | Tank |
| hunter | 뱀파이어 사냥 | Vampire Slayer |
| gravedigger | 시신 차단 | Corpse Warden |
| doctor | 치유 | Healer |
| scout | 정찰·함정 | Scout |

### 6. 능력치

1. 줄(높이 34): 아이콘 24 진영 색 + 이름 `body` `text.2` + 숫자 `num.2` `text.1`(x 150 에서 시작).
2. 줄 순서: 체력, 공격, 은탄(0 이면 줄 없음), 방어, 이동, 사거리, 치료량(0 이면 줄 없음). 이름은 지금 툴팁 키(`unit.card.hp` 등)를 쓴다. 은탄 줄 이름은 `detail.bane` "뱀파이어·스폰에게", 숫자는 "+{n}", 아이콘 `Status_Vampiric_01`.
3. 체력: 유닛 판은 "{지금} / {최대}", 최대의 1/3 이하면 지금 값을 `enemy`. 병종 판은 최대만.
4. 강화로 바뀐 능력치는 숫자를 `light` 로 쓴다. 기본값은 줄에 쓰지 않고, 그 줄을 호버하면 툴팁에 `detail.base` "기본 {n}" 과 출처를 한 줄씩("백전노장 +6") 보인다.
5. 조건이 맞을 때만 바뀌는 값(밀집 대형의 방어 +1, 매복·돌격의 피해)은 능력치에 넣지 않는다. 기술 설명이 말한다.
6. 스폰·하수인·뱀파이어도 같은 줄이다(체력·공격·방어·이동·사거리). ui.md "적 유닛의 스펙을 제공한다. 이동력도 들어간다".

### 7. 지금 상태 (유닛 판만)

1. 상태는 **용어**다. 상태마다 이름(낱말 하나)·색·설명을 가진 항목 하나로 두고(아래 표), 창에는 이름만 칩으로 보인다. 설명 문장은 창에 쓰지 않는다.
2. 칩: 높이 28, 지름 8 점 + 이름 `small` Bold, 점·글자·1px 테두리가 그 상태의 색, 바탕 `bg.panelDeep`, 안쪽 여백 가로 10. 한 줄에 나란히 두고 넘치면 다음 줄로(간격 8).
3. 칩을 호버하면 0.5초 뒤 툴팁(`frame.tip`, 폭 320): 첫 줄 이름 `body` Bold(그 상태의 색), 아래 설명 `small` `text.2`. 지금 쓰는 HoverTip 을 그대로 쓴다.
4. 유닛 패널의 경고 줄(battle-hud.md 3-2)은 하나만 보이지만 여기서는 해당하는 상태를 모두 보인다. 순서는 표의 순서. 해당하는 상태가 없으면 구역을 숨긴다.
5. 상태 표 (새 키 `state.<id>` 이름, `state.<id>.desc` 설명. 설명의 숫자는 규칙 값에서 채운다):

| id | 이름 | English | 색 | 누구에게 | 설명 (호버) |
|---|---|---|---|---|---|
| target | 보름달 표적 | Moon's Prey | `enemy` | 흡혈 표적으로 정해진 아군 | 다음 적 페이즈에 뱀파이어가 흡혈해 쓰러뜨린다. 그때 빛 칸에 있거나 뱀파이어에게서 {EatRange}칸 넘게 떨어져 있으면 피한다. |
| safe | 지금은 안전 | Safe for Now | `text.2` | 표적인데 지금 자리에서는 흡혈당하지 않는 아군 (표적 칩 바로 뒤) | 지금 자리에 그대로 있으면 이번 흡혈을 피한다. |
| lit | 빛 속 | In the Light | `light` | 빛 칸에 선 아군 | 흡혈 표적으로 정해지지 않는다. 여기서 쓰러지면 시신이 남지 않아 스폰이 되지 않는다. |
| risen | 방금 일어남 | Just Risen | `spawn` | 이번 적 페이즈에 일어난 스폰 | 이번 적 페이즈에 시신에서 일어난 스폰이다. |
| coffin | 관 속 | In the Coffin | `light` | 잠든 뱀파이어 | 잠들어 있어 공격받지 않는다. 달이 {MoonWake} 인 채로 시작하는 적 페이즈에 깨어난다. |
| guard | 경계 {n} | On Guard {n} | `light` | 경계 태세의 적 | `unit.card.stance.guard` 의 지금 문장 |
| hold | 고정 | Holding | `text.2` | 고정 태세의 적 | `unit.card.stance.hold` 의 지금 문장 |
| traps | 함정 {남은 수}/{최대} | Traps {n}/{max} | `ally` | 함정 기술이 있는 아군 | 이번 전투에 더 깔 수 있는 함정 수. 다시 채워지지 않는다. |

6. 새 상태가 생기면 이 표에 한 줄을 더한다(코드에서도 상태 목록 한 곳). "행동을 마침" 은 상태로 두지 않는다(보드에서 흐린 모습으로 이미 보인다).

### 8. 스폰이 되면 (아군 병종만, 호위 대상 뺌)

1. `bg.panelDeep` 칸, 왼쪽 3px `spawn` 선. 제목 줄 앞에 `Status_Vampiric_01` 18px `spawn`.
2. 아이콘 수치 한 줄(`num.2`, 아이콘·숫자 `spawn`): 체력 {최대 체력} · 공격 {공격} · 방어 0 · 이동 {이동} · 사거리 1. 값은 spawn.md 9 로 계산한다(산 강화 반영).
3. 설명 문장은 칸에 쓰지 않는다. 칸을 호버하면 툴팁으로 `detail.ifSpawn`: "뱀파이어·스폰에게 쓰러지면 이 값의 적으로 일어난다. 기술은 잃는다."
4. 근거: vampire.md "주력 유닛일수록 더 강한 적이 된다", ui.md "플레이어가 위험한 타일을 스스로 계산할 수 있어야 한다". 지금은 죽어서 시신이 된 뒤에야 이 값이 보인다.

### 9. 기술

1. 그 유닛(병종)이 지금 가진 기술을 모두 한 줄씩: 기본 기술 먼저, 그다음 산 강화의 기술(강화 순서).
2. 줄: 아이콘 칸 40×40(`bg.panelDeep`, 아이콘 28: 행동 바에 칸이 있는 기술은 그 칸의 아이콘·색, 나머지는 `Status_Up_01` `text.2`) + 이름 `body` Bold `text.1` + 오른쪽 꼬리표 `small` `text.3` "{종류} · {출처}" + 아래 설명 `body` `text.2` (줄바꿈, 줄 수 제한 없음). 줄 사이 14.
3. 종류: `detail.kind.action` "행동"(눌러서 쓰고 그 턴의 행동을 쓴다. 행동 바 단축키가 있으면 꼬리표 앞에 단축키 칸) / `detail.kind.passive` "지속"(조건이 맞으면 저절로 듣는다). 출처: `detail.src.base` "기본" / `detail.src.upgrade` "강화".
4. 설명 안의 숫자와 조건 낱말은 `text.1` Bold 로 쓴다(리치 텍스트 `<b>`). 숫자는 문장에 박지 않고 데이터·규칙 값에서 채운다(아래 표의 {…}).
5. 기술이 없으면 `detail.noSkill` "기술 없음" `small` `text.3` 한 줄.
6. 유닛 패널의 "기술: 이름들" 줄과 행동 바 칸 툴팁은 그대로 이름만 쓴다. 긴 설명은 이 창에만 있다.

기술 설명 (새 키 `skill.<id>.desc`):

| 기술 | 종류 | 한국어 | English |
|---|---|---|---|
| 도발 taunt | 지속 | 이 유닛을 공격할 수 있는 적은 다른 아군 대신 **이 유닛만** 공격한다. 근접·원거리 모두 해당한다. 보름달 흡혈의 표적은 바꾸지 못한다. | Any enemy able to attack this unit attacks **only this unit**, melee or ranged. Does not change the full moon's prey. |
| 창벽 pike | 지속 | 적이 이동을 마친 칸이 이 유닛의 **바로 옆**이면 그 적에게 **1** 피해를 준다(방어 무시). 창벽을 가진 아군마다 따로 듣는다. | When an enemy ends its move **next to** this unit, it takes **1** damage (ignores DEF). Each unit with Pike Wall triggers separately. |
| 밀집 대형 formation | 지속 | 공격받을 때 **바로 옆에 다른 농민 창병**이 있으면 방어 **+1**. 여럿이어도 +1 이다. 보름달 흡혈은 막지 못한다. | DEF **+1** when attacked while **next to another Peasant Spearman**. Does not stack. No effect against the full moon's bite. |
| 매복 사격 ambush | 지속 | 이번 턴에 **이동하지 않고** 공격하면 피해 **+1**. | **+1** damage when attacking **without moving** this turn. |
| 베어 넘기기 cleave | 지속 | 공격으로 적을 쓰러뜨리면 **한 번 더 행동**한다. 이동은 못 한다. 턴당 1회. | After killing an enemy with an attack, **act once more** (no movement). Once per turn. |
| 돌격 charge | 지속 | 이번 턴에 **2칸 이상 이동**한 뒤 공격하면 피해 **+2**. | **+2** damage when attacking after moving **2 or more hexes** this turn. |
| 성수 세례 ward | 지속 | **보름달 흡혈의 표적이 되지 않는다.** 그 밖의 공격은 평소대로 받는다. | **Never chosen as the full moon's prey.** Other attacks hit as usual. |
| 수습 bury | 행동 | **{BuryRange}칸 안의 시신** 하나를 없앤다. 그 시신은 스폰으로 일어나지 않는다. 행동을 쓴다. | Remove one **body within {BuryRange} hexes**. It will not rise. Uses the action. |
| 성스러운 매장 rite | 지속 | 수습할 때마다 달이 **1 내려간다**. 보름달이 풀리면 정해진 흡혈 표적도 풀린다. | Each burial **lowers the Moon by 1**. If that breaks the full moon, its prey is released. |
| 밤샘 수습 twice | 지속 | 수습한 뒤 **한 번 더 행동**(공격 또는 수습)한다. 이동은 못 한다. 턴당 1회. | After burying, **act once more** (attack or bury, no movement). Once per turn. |
| 치료 mend | 행동 | **{MendRange}칸 안의 다친 다른 아군** 하나의 체력을 **{치료량}** 되돌린다. 자기 자신은 못 고친다. 행동을 쓴다. | Restore **{heal}** HP to one **other wounded ally within {MendRange} hexes**. Cannot target self. Uses the action. |
| 비산 splash | 지속 | 공격이 맞으면 표적 **바로 옆의 다른 적 모두**에게 **1** 피해(방어 무시). 아군과 시신은 맞지 않는다. | On hit, **every other enemy next to the target** takes **1** damage (ignores DEF). Allies and bodies are not hit. |
| 함정 trap | 행동 | **바로 옆 빈칸**에 함정을 깐다. 적이 밟으면 그 칸에 멈춰 **{함정 피해}** 피해를 입고 그 적 페이즈에 더 움직이거나 공격하지 못한다. 한 판에 **{함정 수}개**, 다시 채워지지 않는다. 적은 함정을 보지 못한다. 행동을 쓴다. | Lay a trap on an **adjacent empty hex**. An enemy stepping on it stops, takes **{trap damage}**, and cannot move or attack for that phase. **{trap count}** per battle, never refilled. Enemies cannot see traps. Uses the action. |
| 톱니 덫 barbs | 지속 | 함정 피해 **+{BarbsDamage}**. | Trap damage **+{BarbsDamage}**. |
| 덫 꾸러미 satchel | 지속 | 한 판에 함정을 **{SatchelHeld}개**까지 깐다. | Lay up to **{SatchelHeld}** traps per battle. |

- 값: `BuryRange` 2, `MendRange`(Rules) 3, 치료량 = 병종 값 + 산 강화, 함정 피해 `TrapDamage` 3 (+톱니 덫), 함정 수 `TrapsHeld` 3 (덫 꾸러미 4). 함정 설명의 피해·개수는 그 유닛이 가진 강화가 반영된 값을 쓴다.
- 도발은 이름 키가 아직 없다. `skill.taunt` "도발" / "Taunt" 를 더한다(지금 Text 표에 `skill.taunt` 가 없다).
- 달의 피·포식·초승의 부름(웨어울프)은 병종이 빠졌지만 기술 코드는 남아 있어, 키가 비지 않게 한 줄짜리 설명만 둔다.

### 10. 강화 (아군만)

1. 그 병종의 강화 셋을 데이터 순서대로 한 줄씩(높이 36): 상태 아이콘 20 + 이름 `body` + 효과 `small` `text.2`(`upgrade.<id>.desc`) + 스폰이 물려받는 강화면 오른쪽 끝에 `Status_Vampiric_01` 18px `spawn`(툴팁 `camp.inherit`).
2. 상태: 산 강화는 ✓ `light` + 이름 `text.1`. 사지 않은 강화는 빈 원 `line` + 이름·효과 `text.3`.
3. 기술을 주는 강화도 효과 칸은 강화의 한 줄 설명(`upgrade.<id>.desc`) 그대로다. 긴 기술 설명은 사서 기술 구역에 올라왔을 때만 보인다.
4. 가격·구매 버튼은 없다. 안내 문구도 두지 않는다.

### 11. 특성 (적만)

기술 줄과 같은 모양(아이콘 칸 + 이름 + 설명), 꼬리표 없음. 해당하는 것만:

| 누구 | 이름 | 한국어 설명 | English |
|---|---|---|---|
| 하수인(정예·중간보스 포함) | 하수인 `detail.trait.minion` | 이 적에게 쓰러진 아군은 **스폰이 되지 않는다.** 은탄의 추가 피해를 받지 않는다. | Allies it kills **do not rise.** Takes no extra damage from silver. |
| 뱀파이어·스폰 | 변이 `detail.trait.turn` | 이 적에게 쓰러진 아군은 시신을 남기고 **다음 적 페이즈에 스폰으로 일어난다.** 빛 칸에서 쓰러지면 일어나지 않는다. 은탄의 추가 피해를 받는다. | Allies it kills leave a body that **rises as spawn next enemy phase** — unless they fall on a lit hex. Takes extra damage from silver. |
| 사거리 2 이상인 적·아군 | 원거리 `detail.trait.ranged` | **{사거리}칸** 안을 쏜다. 바로 옆 칸은 피해가 절반이다. 바위에 가린 대상은 쏘지 못한다. | Shoots within **{rng} hexes**. Half damage at point-blank. Cannot shoot past rocks. |
| 뱀파이어 | 관 `detail.trait.coffin` | 달이 **{MoonWake}** 인 채로 시작하는 적 페이즈에 깨어난다. 다른 적이 모두 쓰러져도 깨어난다. 잠든 동안은 공격받지 않고, 깬 페이즈에는 행동하지 않는다. | Wakes on an enemy phase that begins with the Moon at **{MoonWake}**, or once no other enemy remains. Cannot be attacked while asleep; does not act on the phase it wakes. |
| 뱀파이어 | 보름달 흡혈 `detail.trait.feed` | 달이 **{MoonMax}** 이면 **{EatRange}칸** 안의 빛 밖 아군 하나를 표적으로 정하고, 다음 적 페이즈에 흡혈해 쓰러뜨린다. 체력을 **{흡혈 회복}** 되찾는다. | At Moon **{MoonMax}** it marks one unlit ally within **{EatRange} hexes** and drains it next enemy phase, restoring **{heal}** HP. |
| 스폰 | 물려받은 몸 `detail.trait.origin` | **{원본 이름}** 의 체력·공격·이동을 물려받았다. 방어는 0 이고 근접 공격만 한다. | Inherited HP, ATK and MOV from **{origin}**. DEF 0, melee only. |

- 아군의 "원거리" 는 9절 기술 구역 맨 끝에 꼬리표 없이 둔다.
- 위협 범위·행동 예고는 넣지 않는다(ui.md, combat.md). 숫자와 규칙만 말한다.

### 12. 병종 넘기기 (병종 판만)

1. 편성·장 정보·병영에서 열었으면 ←/→ 로 그 목록의 앞·뒤 병종으로 바꾼다(편성 줄 순서, 출전 병종 칸 순서, 병영의 열린 탭 순서). 끝에서 멈춘다.
2. 창은 그대로 있고 내용만 바뀐다. 병영에서는 닫을 때 마지막에 본 병종의 탭이 골라져 있다.
3. 유닛 판에는 넘기기가 없다(바닥 줄 왼쪽을 비운다).

### 13. 문장 (Text 표 새 키)

| 키 | 한국어 | English |
|---|---|---|
| `detail.open` | 자세히 (I) | Details (I) |
| `detail.closeHint` | I · Esc 닫기 | I · Esc to close |
| `detail.browse` | ← → 다른 병종 | ← → Other units |
| `detail.sec.stats` / `.state` / `.spawn` / `.skills` / `.upgrades` / `.traits` | 능력치 / 지금 상태 / 스폰이 되면 / 기술 / 강화 / 특성 | Stats / Right Now / If It Rises / Skills / Upgrades / Traits |
| `detail.base` | 기본 {0} | Base {0} |
| `detail.bane` | 뱀파이어·스폰에게 | vs Vampires & Spawn |
| `detail.ifSpawn` | 뱀파이어·스폰에게 쓰러지면 이 값의 적으로 일어난다. 기술은 잃는다. | Killed by a vampire or spawn, it rises with these stats. Skills are lost. |
| `state.<id>` / `state.<id>.desc` | 7절 표 | 이름은 7절 표, 설명은 구현 때 옮긴다 |
| `detail.noSkill` | 기술 없음 | No skills |
| `detail.kind.action` / `.passive` | 행동 / 지속 | Action / Passive |
| `detail.src.base` / `.upgrade` | 기본 / 강화 | Innate / Upgrade |
| `role.<key>` | 5절 표 | |
| `skill.<id>.desc` | 9절 표 | |
| `detail.trait.*` (이름·`.desc`) | 11절 표 | |

## 레퍼런스

그림은 저장소에 넣지 않는다(ui-references.md). 링크는 열리는 것만 적었다.

| 작품 | 본 곳 | 가져오는 것 | 가져오지 않는 것 |
|---|---|---|---|
| Wargroove | 유닛 정보 https://interfaceingame.com/screenshots/wargroove-unit-information/ , 도감 https://interfaceingame.com/screenshots/wargroove-codex/ | 병종 하나를 한 창에: 왼쪽 그림·수치, 오른쪽 특수 조건(치명타 조건)을 문장으로. 전투 중에도 같은 창 | 상성표(이 게임에는 병종 상성이 없다) |
| XCOM 2 | 능력 https://interfaceingame.com/screenshots/xcom-2-abilities/ , 병사 수치 https://interfaceingame.com/screenshots/xcom-2-soldier-stats/ | 기술 = 아이콘 + 이름 + 설명 문단의 목록. 가진 것과 아직 없는 것을 한 목록에서 밝기로 가른다 | 승급 나무(강화는 셋뿐이라 줄 셋이면 된다) |
| Slay the Spire | 카드 보기 https://interfaceingame.com/screenshots/slay-the-spire-view-card/ , 적 정보 https://interfaceingame.com/screenshots/slay-the-spire-enemy-stats/ | 뒤를 어둡게 하고 가운데에 크게, 아무 데나 누르면 닫힘. 설명 속 핵심 낱말을 색·굵기로 띄움 | 낱말마다 뜨는 겹 툴팁(1차에는 넣지 않는다, "미룬 것") |
| Into the Breach | 유닛 정보 칸 https://interfaceingame.com/screenshots/into-the-breach-hornet-leader/ , https://interfaceingame.com/screenshots/into-the-breach-artillery-mech/ | 적도 아군과 같은 틀로 수치·능력을 다 보여 준다(숨기는 정보 없음). 무기 설명은 한두 문장 | 공격 예고 표시(이 게임은 행동 예고가 없다) |
| Gears Tactics | 캐릭터 수치 https://interfaceingame.com/screenshots/gears-tactics-character-stats/ | 수치 줄에 항목 이름을 글로 쓰고, 바뀐 값은 색으로 | 장비 칸 |
| Baldur's Gate 3 | 용어 안내의 Examine 항목 https://game8.co/games/BG3/archives/418670 | 적을 "살펴보기" 로 여는 따로 창이 있고, 호버 패널은 짧게 둔다(짧은 패널 + 긴 창의 두 단계) | 오른쪽 클릭 메뉴(오른쪽 클릭은 회수에 쓴다) |

## 정한 세부 (docs/design 에 없는 것)

- **가운데 창 하나, 모든 화면에서 같다.** 근거: 오른쪽 칸(ui-style.md 16-3)은 전투에서는 전투 기록·적 패널, 캠페인에서는 장 정보·병영이 이미 쓴다. 병영에서 열면 병영 칸을 덮게 된다. 가운데 창은 어디서나 같은 자리·같은 크기라 코드도 한 벌이다. 읽는 동안은 게임이 멈춰 있어도 되는 순간(아군 턴·배치·캠페인)에만 열린다.
- **여는 길은 정보 버튼과 `I` 하나.** 근거: 오른쪽 클릭은 회수(#130), 보드 왼쪽 클릭은 선택·명령이다. 호버 패널에 버튼이 있으면 "더 볼 수 있다" 가 보이고, `I` 는 지금 쓰는 키(1~5, Space, Esc, Alt, F, R, B, WASD, Q·E, 화살표)와 겹치지 않는다.
- **적 턴 재생 중에는 열지 않는다.** 근거: 재생을 멈추고 잇는 처리가 필요해진다. 호버 패널은 적 턴에도 보인다.
- **"스폰이 되면" 구역.** 근거: 8절 4번. 이 게임에서 병종을 고르는 고민의 절반은 "죽으면 무엇이 되나" 다.
- **지금 상태는 전부, 용어로 보인다 (사용자 결정 2026-11-20).** 상태마다 이름·색·설명을 가진 용어로 두고 창에는 이름 칩만, 설명은 호버 툴팁. 유닛 패널은 높이 때문에 경고 하나만 보이지만(battle-hud.md "경고 줄은 하나만") 상세 창은 자리가 있다.
- **풀어 쓴 설명 줄은 창에 두지 않는다 (사용자 결정 2026-11-20).** 이름 아래 병종 소개 글, 능력치의 "기본 n", "스폰은 물려받지 않는다", "구매는 병영에서", "스폰이 되면" 의 설명 문장, "행동을 마쳤습니다" 를 뺐다. 남는 문장은 기술·특성 설명뿐이고, 나머지 설명은 호버 툴팁으로 간다.
- **기술 설명의 숫자는 데이터에서.** 근거: 밸런스 수치가 자주 바뀐다(#132·#136·#148). 문구에 숫자를 박으면 값이 바뀔 때마다 한·영 문구를 고쳐야 한다.
- **강화는 읽기만.** 근거: 구매 확인·금화 표시는 병영 레이어의 일이다(ui-style.md 16-5).
- **초상은 아이콘.** 근거: 지금 있는 병종 아이콘으로 바로 만든다. 3D 모델 그림은 "미룬 것".

## 미룬 것 (필요해지면 더한다)

- 왼쪽 위 아이콘 자리에 3D 모델 그림(렌더 텍스처). 모양은 그대로 두고 그 칸만 바꾸면 된다.
- 설명 속 낱말(스폰, 시신, 보름달, 은탄, 빛)에 호버하면 뜨는 낱말 툴팁.
- 장 정보의 적 구성에서 적 종류를 여는 길, 처음 만난 적이 쌓이는 도감.
- 적 턴 재생 중 열기(재생 멈춤).

## 열린 질문 (사용자가 정할 것)

없음. 창 모양(가운데 창), 여는 법(정보 버튼 + `I`), "스폰이 되면" 구역은 추천안을 낸 뒤 사용자가 그대로 구현을 맡겨(2026-11-20 "구현 시작") 이 문서대로 만들었다. 바꾸려면 말해 주면 된다.

## 구현

- 구현됨: 이슈 #127. 위 요구사항과 구현이 다르면 구현을 고친다.
- 코드: `Game/Screens/UnitDetailModel.cs`(무엇을 보일지: 능력치·상태 용어·기술·특성·강화, 시험 `UnitDetailTests`), `Game/Screens/UnitDetail.cs`(창 그리기), 배치 `UI/Detail.uxml`·`Detail.uss`, 정보 버튼 `ToolkitKit.InfoBtn`(`Theme.uss` `.info-btn`).
- 창은 공용 `Panel.asset` 의 사본(그리기 순서 +10)을 쓰는 따로 UIDocument 다. 전투(`BattleFlow`)와 캠페인(`CampFlow`)이 하나씩 만들고, 키(I·Esc·←/→)는 연 화면이 받는다. 창이 떠 있는지는 `UnitDetail.IsOpen` 하나로 본다(보드 클릭·전투 카메라 키·단축키가 이것을 보고 멈춘다).
- 유닛 패널은 호버한 동안만 있어서 호버한 적의 정보 버튼은 마우스로 누를 수 없다(선택한 아군·배치 중 살펴본 유닛만 누를 수 있다). 호버한 유닛은 `I` 로 연다. 버튼은 `I` 키 안내를 겸한다.
- 화면 검사: `tools/ui-audit/sweep.sh` 의 `2e2-detail-kind`(병영 병종 판), `4b-detail-ally`(아군 유닛 판), `8d-detail-enemy`(적 유닛 판). 창이 안 뜨면 `no-card` 로 실패한다.
