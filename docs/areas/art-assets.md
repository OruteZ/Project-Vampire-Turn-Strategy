# 아트 에셋

유료 에셋이라 git 에 없다. 새 클론은 `docs/guides/asset-setup.md` 대로 `bash tools/assets/setup.sh` 를 돌린다(필요한 패키지만 임포트하고 확인). 게임이 참조하는 에셋 목록은 `tools/assets/required.tsv`. worktree 는 메인 트리의 `Assets/Synty` 를 junction 으로 잇는다 (`.claude/skills/verify-unity/SKILL.md` Launch).

## 분류·경로

| 분류 | 경로 | 원본 패키지 | 내용 |
|---|---|---|---|
| 다크 판타지 환경·소품·캐릭터 | Assets/Synty/PolygonDarkFantasy | POLYGON_Dark_Fantasy | 프리팹 647. 스켈레톤 `SM_Chr_Skeleton_01`, 뱀파이어 후보 `SM_Chr_DarkLord_Male_01`, 사냥꾼 `SM_Chr_Hunter_Male_01`, 바위 `SM_Env_Rock_0*`, 화로 `SM_Prop_Brazier_01`, 불 `FX_Fire_01` |
| 공용 | Assets/Synty/PolygonGeneric | POLYGON_Dark_Fantasy | 프리팹 495 |
| 기사 캐릭터 | Assets/Synty/SidekickCharacters/Characters/FantasyKnights | SIDEKICK_Fantasy_Knights | `FantasyKnights_01`~`06` 완성 프리팹 |
| 마을 사람 캐릭터 | Assets/Synty/SidekickCharacters/Characters/FantasyVillagers | SIDEKICK_Fantasy_Villagers | `FantasyVillagers_01`~`05` 완성 프리팹 |
| 사람 몸 바탕 | Assets/Synty/SidekickCharacters/Characters/HumanSpecies | SIDEKICK 둘 다 | `HumanSpecies_01`~`04` |
| 활·석궁 무기 | Assets/Synty/PolygonBowCrossbow | POLYGON_Bow_Crossbow | 리깅된 활 `SM_Wep_Rigged_Bow_01`·`_NativeAmerican_01`(시위 뼈 `bowString`), 화살 `SM_Prop_Arrow_01`, 리깅된 석궁 `SM_Wep_Rigged_CrossBow_01`, 석궁 화살 |
| 기본 이동 애니메이션 | Assets/Synty/AnimationBaseLocomotion | ANIMATION_Base_Locomotion | |
| 활 전투 애니메이션 | Assets/Synty/AnimationBowCombat | ANIMATION_Bow_Combat | |
| 검 전투 애니메이션 | Assets/Synty/AnimationSwordCombat | ANIMATION_Sword_Combat | |
| HUD | Assets/Synty/InterfaceDarkFantasyHUD | INTERFACE_Dark_Fantasy_HUD | uGUI 프리팹 327 |
| 메뉴 UI | Assets/Synty/InterfaceDarkFantasyMenus | INTERFACE_Dark_Fantasy_Menus | uGUI 프리팹 228 |
| UI 공용 스프라이트·셰이더 | Assets/Synty/InterfaceCore | INTERFACE 둘 다 | |

## 결정
| 결정 | 상태 | 날짜 | 출처 |
|---|---|---|---|
| UI 는 uGUI 로 만든다 (INTERFACE 패키지가 RectTransform 프리팹, UXML 없음) | 시험 중 | 2026-11-05 | #2 |

## 열린 작업
- 관(coffin) 프리팹이 없어 P7 은 상자로 대신했다. 맞는 소품을 찾으면 바꾼다

## 맡길 때
- 함정: 애니메이션 팩마다 Sidekick 전용판 클립(`A_MOD_*`)이 따로 있다. Sidekick 캐릭터에는 그쪽을 쓴다. 무기 자리 뼈는 Sidekick `prop_r`/`prop_l`, Polygon 캐릭터 `Hand_R`/`Hand_L`
- 함정: Sidekick 프리셋은 메시 하나라 모자 같은 부품만 끌 수 없다. 부품을 고르려면 Sidekick 도구로 새로 만든다 (위 "모델 바꾸기")
- 함정: 구운 캐릭터는 굽기마다 GUID·fileID 가 새로 정해진다. 에셋 참조로 가리키지 말고 `UnitLook.recipe` 이름으로 찾는다
- 함정: 애니메이션 패키지 임포트 때 `Rig Error: Copied Avatar Rig Configuration mis-match` 경고가 수백 개 나온다. Synty 쪽 설정이라 오류가 아니다. 이 클립을 PolygonDarkFantasy 캐릭터에 쓸 때 자세가 맞는지는 P7 에서 본다.
- 함정: SIDEKICK 패키지의 에디터 스크립트가 Synty 서버에서 Sidekick 도구를 스스로 내려받아 `Assets/DownloadCache/` 에 둔다 (gitignore 됨). 도구 데이터는 `Failed to load tool data` 로 불러오지 못했다. 완성 프리팹만 쓰고 도구는 들이지 않았다.
- 함정: 애니메이션 패키지끼리 같은 파일이 있어 `Overriding existing asset with new GUID from package` 경고가 3건 나왔다. 그 파일을 참조할 때 GUID 가 나중 패키지 것이다.

## 보드에서 쓰는 것 (P6, `Assets/_Project/Art/ArtCatalog.asset`)

| 지형 | 표시 |
|---|---|
| 평지·배치 칸 | `Hex_Plain.mat` (#2e2a36), 빛 받는 칸은 `Hex_Lit.mat` |
| 바위 `#` | `Hex_Rock.mat` + `SM_Env_Rock_01~03` (칸마다 돌려 씀) |
| 화톳불 `T` | `SM_Prop_Brazier_01` + `FX_Fire_01` + 점광원 |
| 햇빛 `S` | `Hex_Sun.mat` + 위에서 비추는 스폿라이트 |
| 물 `~` | `Hex_Water.mat`, 한 단 낮게 |

## 유닛에 쓰는 것 (#29, docs/art/unit-visuals.md)

병종 하나 = `Assets/_Project/Art/Units/<병종>.asset` (`UnitLook`: 모델, 뼈대, 무기와 손·위치·회전, 동작 묶음). `ArtCatalog.allyLooks`(UnitKind 순서), `skeletonLook`, `vampireLook` 이 가리킨다.

| 유닛 | 모델 | 무기 | 동작 묶음 |
|---|---|---|---|
| 농민 창병 | `FantasyVillagers_05` | 쇠스랑 (자루 가운데, y −0.45) | `Units/Spear.overrideController` |
| 농민 궁수 | `FantasyVillagers_04` | 활 `SM_Wep_Rigged_Bow_01` 왼손 | `Units/Bow` |
| 검사 | `FantasyKnights_05` | 검 + 방패 | `Units/SwordShield` |
| 궁수 | `FantasyKnights_02` | 활 왼손 | `Units/Bow` |
| 기사 | `FantasyKnights_06` | 검 `Sword_02` + 방패 | `Units/SwordShield` |
| 사냥꾼 | `SM_Chr_Hunter_Male_01` | 석궁 `SM_Wep_Rigged_CrossBow_01` | `Art/Unit.controller` (Polygon) |
| 스켈레톤 | `SM_Chr_Skeleton_01` | 검 `Sword_03` | `Art/Unit.controller` |
| 뱀파이어 | `SM_Chr_DarkLord_Male_01` (잠들면 짙은 붉은 상자 위에 눕힘) | 낫 (y −0.25) | `Art/Unit.controller` |
| 스폰·시신 | 원래 병종 모델, 무기 없음 + 보라 고리 | | Sidekick 은 `Units/SidekickUnarmed` |

### 모델 바꾸기 (Sidekick 으로 새로 만든 캐릭터, #30)
1. Sidekick 도구에서 Save Character 로 `Assets/_Project/Art/Characters/<이름>.sk` 에 레시피를 저장한다 (영문 이름).
2. 그 병종 `Units/<병종>.asset` 의 `recipe` 에 `<이름>` 을 쓴다. `model` 은 지금 프리셋 그대로 둔다 (굽기 전 클론의 대체 모델).
3. 도구 모음 (Tools) > **Synty 다시 불러오기**, 또는 `/synty-reload` 스킬, 또는 `bash tools/assets/sidekick-bake.sh`. 무기 뼈 `prop_r`/`prop_l` 이 같아 무기 위치는 그대로 쓴다.
4. `.sk` 와 `UnitLook` 을 커밋한다. `Assets/_Generated/` 는 올리지 않는다.
- 파이프라인 견본: `Characters/SampleKnight.sk` (어느 유닛도 쓰지 않음, 시험이 실제로 굽게 하려고 둔다. 진짜 레시피가 생기면 지운다). 구운 모습 docs/review/sidekick-bake-sample.png
