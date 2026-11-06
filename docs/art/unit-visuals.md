# 유닛 모습 (모델·무기·애니메이션)

> 출처: docs/design/art.md, docs/design/units.md, docs/design/campaign.md (커밋 f7fc7f8)
> 근거 자료: docs/dev/unit-models-plan.md (맞춰 보기, 후보 사진첩 docs/review/unit-candidates-0~2.png)
> 목적: 병종마다 어떤 모델이 어떤 무기를 어느 손에 들고 어떤 동작을 하는지 정하고, 모델을 나중에 바꾸기 쉬운 구조를 정한다.

## 1. 병종별 모습

| 병종 | 모델 | 뼈대 | 오른손 | 왼손 | 동작 묶음 |
|---|---|---|---|---|---|
| 농민 창병 | Sidekick `FantasyVillagers_05` | Sidekick | 쇠스랑 `SM_Wep_Pitchfork_01` | | 창 |
| 농민 궁수 | Sidekick `FantasyVillagers_04` | Sidekick | | 활 `SM_Wep_Rigged_Bow_01` | 활 |
| 검사 | Sidekick `FantasyKnights_05` | Sidekick | 검 `SM_Wep_Sword_01` | 방패 `SM_Wep_Shield_01` | 검·방패 |
| 궁수 | Sidekick `FantasyKnights_02` | Sidekick | | 활 `SM_Wep_Rigged_Bow_01` | 활 |
| 기사 | Sidekick `FantasyKnights_06` | Sidekick | 검 `SM_Wep_Sword_02` | 방패 `SM_Wep_Shield_01` | 검·방패 |
| 사냥꾼 | PolygonDarkFantasy `SM_Chr_Hunter_Male_01` | Polygon | 석궁 `SM_Wep_Rigged_CrossBow_01` | | Polygon 기본 |
| 스켈레톤 | `SM_Chr_Skeleton_01` | Polygon | 검 `SM_Wep_Sword_03` | | Polygon 기본 |
| 뱀파이어 | `SM_Chr_DarkLord_Male_01` | Polygon | 낫 `SM_Wep_Scythe_01` | | Polygon 기본 |
| 스폰 | 원래 병종 모델 | 원래 것 | 없음 | 없음 | 맨손 근접 (units.md "원거리 공격을 할 수 없다") |

## 2. 동작 묶음

상태 이름은 모두 같다: 대기 `Idle`, 이동 `Walk`, 공격 `Attack`, 맞음 `Hit`, 죽음 `Death` (`Art/Unit.controller`). 묶음은 클립만 바꾼다.

| 묶음 | 대기 | 이동 | 공격 | 맞음 | 죽음 |
|---|---|---|---|---|---|
| 창 | `A_MOD_BL_Idle_Standing_Masc` | `A_MOD_BL_Walk_F_Masc` | `A_MOD_SWD_Attack_HeavyStab01_ReturnToIdle_Neut` | `A_MOD_SWD_Hit_F_React_Neut` | `A_MOD_SWD_Death_B_Neut` |
| 검·방패 | 같음 | 같음 | `A_MOD_SWD_Attack_LightCombo01A_ReturnToIdle_Neut` | 같음 | 같음 |
| 활 | `A_MOD_BOW_Stand_Idle_BowDown_Neut` | `A_MOD_BL_Walk_F_Masc` | `A_MOD_BOW_Stand_Shoot_ToBowDown_Neut` | `A_MOD_BOW_Hit_Head_F_React_Masc` | `A_MOD_BOW_Death_Stomach_F_Masc` |
| Sidekick 맨손 (스폰) | 창과 같음 | 같음 | `A_MOD_SWD_Attack_LightCombo01A_ReturnToIdle_Neut` | 같음 | 같음 |
| Polygon 기본 | 지금 `Unit.controller` 그대로 (Polygon 판 클립) | | | | |

- 화살은 날리지 않는다. 공격 동작과 맞는 쪽 반응으로 충분하다. 날리게 되면 이 문서에 더한다.

## 3. 모델을 바꾸기 쉬운 구조

1. 병종 하나의 모습은 에셋 하나(`UnitLook`, `Assets/_Project/Art/Units/<병종>.asset`)다. 담는 것: 모델 프리팹, 뼈대 종류(Sidekick / Polygon), 무기 목록(프리팹, 손, 위치, 회전), 동작 묶음.
2. 무기를 붙이는 뼈는 뼈대 종류가 정한다: Sidekick 은 `prop_r`/`prop_l`, Polygon 은 `Hand_R`/`Hand_L`. 그래서 같은 뼈대의 다른 모델로 바꾸면 무기 위치·회전을 그대로 쓸 수 있다.
3. Sidekick 도구로 만든 새 캐릭터로 바꾸는 법: 새 프리팹을 그 병종 `UnitLook` 의 모델 칸에 넣는다. 뼈대가 Sidekick 이면 나머지 칸은 그대로 둔다. 키가 달라도 유닛 키는 `ArtCatalog.unitHeight` 로 맞춰진다.
4. 무기 위치가 어긋나면 그 `UnitLook` 의 위치·회전만 고친다. 코드는 바꾸지 않는다.
5. 바꾼 뒤 `node tools/assets/scan.mjs` 로 에셋 목록을 다시 뽑는다 (Synty 에셋 참조가 바뀌므로).

## 정한 세부 (docs/design 에 없는 것)

- **복장은 사진첩 추천대로.** 사용자 "일단 그걸로 작업하고" (#29). 나중에 사용자가 Sidekick 으로 만든 모델로 바꾼다.
- **사냥꾼은 석궁.** 이미 UI 아이콘이 석궁이고, 활 쓰는 궁수 계열과 판에서 갈린다.
- **뱀파이어는 낫, 스켈레톤은 검.** UI 아이콘(낫, 뼈)과 맞추되 스켈레톤은 키트에 뼈 무기 모델이 없어 검.
- **화살 없음.** 연출 비용 대비 정보가 늘지 않는다(누가 누구를 쳤는지는 맞음 동작과 피해 숫자가 보인다).

## 열린 질문 (사용자가 정할 것)

없음.
