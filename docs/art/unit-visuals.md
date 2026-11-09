# 유닛 모습 (모델·무기·애니메이션)

> 출처: docs/design/art.md, docs/design/units.md, docs/design/campaign.md (커밋 f7fc7f8)
> 근거 자료: docs/dev/unit-models-plan.md (맞춰 보기)
> 목적: 병종마다 어떤 모델이 어떤 무기를 어느 손에 들고 어떤 동작을 하는지 정하고, 모델을 나중에 바꾸기 쉬운 구조를 정한다.

## 1. 병종별 모습

| 병종 | 모델 | 뼈대 | 오른손 | 왼손 | 동작 묶음 |
|---|---|---|---|---|---|
| 농민 창병 | Sidekick `FantasyVillagers_05` | Sidekick | 쇠스랑 `SM_Wep_Pitchfork_01` | | 창 |
| 농민 궁수 | Sidekick `FantasyVillagers_04` | Sidekick | | 활 `SM_Wep_Rigged_Bow_01` | 활 |
| 검사 | Sidekick `FantasyKnights_05` | Sidekick | 검 `SM_Wep_Sword_01` | 방패 `SM_Wep_Shield_01` | 검·방패 |
| 궁수 | Sidekick `FantasyKnights_02` | Sidekick | | 활 `SM_Wep_Rigged_Bow_01` | 활 |
| 기사 | Sidekick `FantasyKnights_06` | Sidekick | 검 `SM_Wep_Sword_02` | 방패 `SM_Wep_Shield_01` | 검·방패 |
| 사냥꾼 | PolygonDarkFantasy `SM_Chr_Hunter_Male_01` | Polygon | | 석궁 `SM_Wep_Rigged_CrossBow_01` (한 손 석궁, #99) | Polygon 활 (#99) |
| 장의사 (#99) | PolygonDarkFantasy `SM_Chr_Gravedigger_Male_01` | Polygon | 삽 `SM_Wep_Shovel_01` | | Polygon 기본 |
| 역병 의사 (#99) | PolygonDarkFantasy `SM_Chr_PlagueDoctor_01` | Polygon | 지팡이 `SM_Wep_Staff_01` | | Polygon 기본 |
| 스켈레톤 | `SM_Chr_Skeleton_01` | Polygon | 검 `SM_Wep_Sword_03` | | Polygon 기본 |
| 스켈레톤 궁수 (#99) | `SM_Chr_Skeleton_Ranger_01` | Polygon | | 활 `SM_Wep_Rigged_Bow_01` | Polygon 활 |
| 스켈레톤 중갑병 (#99) | `SM_Chr_Skeleton_HeavyArmor_01` | Polygon | 철퇴 `SM_Wep_Mace_01` | 방패 `SM_Wep_Shield_01` | Polygon 기본 |
| 뱀파이어 | `SM_Chr_DarkLord_Male_01` | Polygon | 낫 `SM_Wep_Scythe_01` | | Polygon 기본 |
| 스폰 | 원래 병종 모델 | 원래 것 | 없음 | 없음 | 맨손 근접 (units.md "원거리 공격을 할 수 없다") |

## 2. 동작 묶음

상태 이름은 모두 같다: 대기 `Idle`, 이동 `Walk`, 공격 `Attack`, 맞음 `Hit`, 죽음 `Death` (`Art/Unit.controller`). 묶음은 클립만 바꾼다.

| 묶음 | 대기 | 이동 | 공격 | 맞음 | 죽음 |
|---|---|---|---|---|---|
| 창 | `A_MOD_BL_Idle_Standing_Masc` | `A_MOD_BL_Walk_F_Masc` | `A_MOD_SWD_Attack_HeavyStab01_ReturnToIdle_Neut` | `A_MOD_SWD_Hit_F_React_Neut` | `A_MOD_SWD_Death_B_Neut` |
| 검·방패 | 같음 | 같음 | `A_MOD_SWD_Attack_LightCombo01A_ReturnToIdle_Neut` | 같음 | 같음 |
| 활 | `A_MOD_BOW_Stand_Idle_BowDown_Neut` | `A_MOD_BL_Walk_F_Masc` | `A_MOD_BOW_Stand_Shoot_ToBowDown_Neut` | `A_MOD_BOW_Hit_Head_F_React_Masc` | `A_MOD_BOW_Death_Stomach_F_Masc` |
| Polygon 활 (#99, 원거리 Polygon 유닛) | `A_POLY_BOW_Stand_Idle_BowDown_Neut` | Polygon 기본 | `A_POLY_BOW_Stand_Shoot_ToBowDown_Neut` | `A_POLY_BOW_Hit_Head_F_React_Masc` | `A_POLY_BOW_Death_Stomach_F_Masc` (파일 이름) |
| Sidekick 맨손 (스폰) | 창과 같음 | 같음 | `A_MOD_SWD_Attack_LightCombo01A_ReturnToIdle_Neut` | 같음 | 같음 |
| Polygon 기본 | 지금 `Unit.controller` 그대로 (Polygon 판 클립) | | | | |

- 화살은 날리지 않는다. 공격 동작과 맞는 쪽 반응으로 충분하다. 날리게 되면 이 문서에 더한다.
- 공격 클립은 루트 모션판을 쓴다: Sidekick 은 `_RM_` 이 붙은 같은 클립(`A_MOD_SWD_Attack_HeavyStab01_ReturnToIdle_RM_Neut`, `A_MOD_SWD_Attack_LightCombo01A_ReturnToIdle_RM_Neut`), Polygon 은 `A_Attack_LightCombo01A_ReturnToIdle_RootMotion_Sword`. 활 쏘기·맞음·죽음은 루트 모션판이 없어 지금 클립 그대로다.

### 2-1. 루트 모션

1. Animator 는 루트 모션을 켠다. 클립이 몸을 앞으로 내딛거나 뒤로 밀리면 모델이 그만큼 실제로 움직인다(제자리에서 미끄러지지 않는다).
2. 움직이는 것은 **모델**이고 유닛 자리(칸)는 그대로다. 규칙상 유닛은 늘 칸 가운데에 있다.
3. 걷기는 예외다. 칸 사이 이동은 프로토타입 속도(칸마다 0.12초)로 코드가 옮긴다. 걷는 동안 클립의 루트 이동은 버린다(걷기 클립 보폭으로는 이 속도를 낼 수 없다).
4. 동작이 끝나고 대기로 돌아오면 모델은 0.25초 동안 칸 가운데로 돌아온다. 그래서 공격을 여러 번 해도 모델이 칸에서 벗어나 쌓이지 않는다.
5. 루트 회전은 버린다. 바라보는 방향은 게임이 정한다(4절).

## 3. 모델을 바꾸기 쉬운 구조

1. 병종 하나의 모습은 에셋 하나(`UnitLook`, `Assets/_Project/Art/Units/<병종>.asset`)다. 담는 것: 모델 프리팹, 뼈대 종류(Sidekick / Polygon), 무기 목록(프리팹, 손, 위치, 회전), 동작 묶음.
2. 무기를 붙이는 뼈는 뼈대 종류가 정한다: Sidekick 은 `prop_r`/`prop_l`, Polygon 은 `Hand_R`/`Hand_L`. 그래서 같은 뼈대의 다른 모델로 바꾸면 무기 위치·회전을 그대로 쓸 수 있다.
3. Sidekick 도구로 만든 캐릭터는 유료 에셋을 가공한 것이라 git 에 올리지 않는다. 대신 **레시피**(`.sk`: 부품 이름, 색, 체형 값)를 `Assets/_Project/Art/Characters/<이름>.sk` 에 커밋하고, 클론마다 다시 굽는다 (도구 모음 "Synty 다시 불러오기", `/synty-reload`, `tools/assets/sidekick-bake.sh`. 셋 다 같은 굽기 코드 `tools/assets/SidekickBaker.cs`). 구운 결과는 `Assets/_Generated/Resources/Characters/<이름>/` (gitignore).
4. `UnitLook` 의 `recipe` 에 레시피 이름을 쓰면 구운 캐릭터를 이름으로 불러온다 (`Resources`). 굽기마다 GUID·fileID 가 새로 정해지므로 에셋 참조로 가리키지 않는다. `model` 칸은 레시피가 없거나 아직 굽지 않은 클론에서 쓰는 Synty 완성 프리팹이다. 그래서 굽지 않은 클론도 게임과 시험이 돈다.
5. 무기는 3-1 의 쥐는 법으로 붙인다. 그래도 어긋나면 그 무기 줄의 위치·회전(쥐는 법 위에 더하는 미세 조정)만 고친다. 키가 달라도 유닛 키는 `ArtCatalog.unitHeight` 로 맞춰진다.
6. `SidekickRecipeTests` 가 레시피마다 구운 것이 있고 레시피가 바뀌지 않았는지(`recipe.sha1`), 모든 `UnitLook` 에 대체 모델이 있는지 본다.

### 3-1. 무기 쥐는 법 (새 유닛·새 무기에도 그대로)

무기 줄마다 **쥐는 법**(`hold`)을 하나 고른다. 손 뼈마다 쥐는 법별 "소켓" 방향이 코드에 정해져 있어, 무기는 소켓에 맞춰 놓이고 무기 줄의 위치·회전은 기본 0 이다.

| 쥐는 법 | 무엇 | 소켓 | 쥐는 지점 |
|---|---|---|---|
| 칼날 `Blade` | 검, 도끼, 단검 | 날(무기 +Y)이 주먹 아래로 나와 앞으로 55도 기운다. 대기에서 칼끝이 앞 아래(바닥 위), 찌르기에서 앞 | 무기 기준점 (Synty 무기는 기준점이 손잡이) |
| 창 `Spear` | 창, 쇠스랑 | 칼날과 같은 방향. 대기에서 창끝을 앞으로 낮게 겨누고 찌르기에서 앞을 찌른다 | 자루 길이의 아래에서 35% |
| 자루 `Pole` | 낫, 지팡이 | 날이 주먹 위로 나온다. 대기에서 세워 든다 | 자루 길이의 아래에서 35% |
| 그대로 `AsIs` | 활, 석궁, 방패 (그 뼈대용으로 만든 Synty 무기) | 뼈 축 그대로. 활 동작 묶음의 손은 뼈대·모델마다 축이 달라, 쏘는 순간 활이 서고(시위가 몸 쪽) 석궁 날개가 수평이 되게 `UnitLook` 의 미세 회전으로 맞춘다(#99, 시험 `ShootersHoldTheirWeaponUprightOnTheBowArm`) | 무기 기준점 |

- Synty 무기 규칙(기준점 = 손잡이, 날 = +Y, 날 폭 = X)을 따르는 무기는 쥐는 법만 고르면 맞는다.
- 뼈대가 달라도 같은 쥐는 법은 같은 모습이다. Polygon `Hand_R`/`Hand_L` 은 Sidekick `prop_r`/`prop_l` 과 축 순서만 다르고, 코드가 그 차이를 소켓에서 바꾼다.
- 대기 자세에서 칼날·자루 무기가 몸 뒤를 가리키면(역수) 시험(`UnitLookTests`)이 실패한다.
- 새 무기·유닛을 들이면 6절 사진으로 본다.

## 4. 바라보는 방향

- 처음 놓일 때(배치·전투 시작·스폰이 일어날 때) 아군은 북쪽(적 쪽, 보드 위쪽), 적(해골·뱀파이어)은 남쪽(아군 쪽)을 본다.
- 걷거나 공격한 뒤에는 마지막으로 향한 쪽을 그대로 본다.
- 시체는 지금처럼 눕힌 모습이다.

## 5. 잠든 뱀파이어

- 관이 열리기 전에는 뱀파이어 모델을 만들지 않고 관만 보인다. 관은 PolygonDarkFantasy 석관 `SM_Prop_Tomb_01` 을 색칠 없이(돌 그대로) 쓴다. 긴 쪽을 남북으로 두고, 긴 쪽 길이 = 유닛 키가 되게 크기를 맞춘다.
- 관이 열리면(`Woke`) 관을 치우고 그 자리에 모델을 만들어 4절 방향(남쪽)으로 세운다.

## 6. 한눈에 보기 (UnitLineup)

`VampireSpawn.Editor.UnitLineup.Capture(<png 경로>)` 가 모든 `UnitLook`(아군 6, 해골, 뱀파이어)을 게임과 같은 코드(`UnitView.Create`)로 만들어 세 줄로 찍는다: 앞에서 본 대기, 옆에서 본 대기, 옆에서 본 공격. 도구 모음 (Tools) 의 "유닛 모습 한눈에" 버튼도 같은 것을 `verify-artifacts/unit-lineup.png` 에 찍는다.

## 정한 세부 (docs/design 에 없는 것)

- **복장은 사진첩 추천대로.** 사용자 "일단 그걸로 작업하고" (#29). 나중에 사용자가 Sidekick 으로 만든 모델로 바꾼다.
- **사냥꾼은 석궁.** 이미 UI 아이콘이 석궁이고, 활 쓰는 궁수 계열과 판에서 갈린다.
- **뱀파이어는 낫, 스켈레톤은 검.** UI 아이콘(낫, 뼈)과 맞추되 스켈레톤은 키트에 뼈 무기 모델이 없어 검.
- **화살 없음.** 연출 비용 대비 정보가 늘지 않는다(누가 누구를 쳤는지는 맞음 동작과 피해 숫자가 보인다).
- **루트 모션은 모델만, 걷기는 코드.** 사용자 요청 "Loot Motion을 키는쪽으로" (#42). 칸 규칙과 프로토타입 이동 속도를 지키려고 칸 자리는 코드가, 몸짓은 클립이 맡는다.
- **쥐는 법 네 가지.** 사용자 요청 "앞으로 추가할 유닛과 도구 에셋도 적용될 수 있도록" (#42). 무기마다 숫자를 맞추는 대신 쥐는 법을 고른다. 자루 35%, 칼날 55도는 6절 사진으로 맞춘 값이다(55도보다 곧으면 Dark Fantasy 검 끝이 바닥을 뚫는다). 창을 세워 들면 찌르기 동작에서 자루 끝으로 뒤를 찔러 창과 자루를 나눴다.
- **아군 북쪽, 적 남쪽.** 사용자 요청 "기본배치에서 유닛이 적방향 (북쪽)을" (#42). 적은 같은 이유로 아군 쪽을 본다.
- **잠든 뱀파이어는 관만.** 사용자 요청 "일어나기 전까지는 모델링 생성자체를 안하고 관만" (#42).

## 열린 질문 (사용자가 정할 것)

없음.
