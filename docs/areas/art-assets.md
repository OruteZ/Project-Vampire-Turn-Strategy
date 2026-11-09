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
| 보드 둘레를 장별 Synty 소품으로 꾸미고, 카메라가 화면 전체를 그린다 | 시험 중 | 2026-11-07 | #34 "synty 에셋을 사용해서 게임 스테이지처럼 보이도록 꾸며줘" |
| 맵(칸 배치)은 테마에 맞춰 다시 짜지 않고 그대로 둔다 | 시험 중 | 2026-11-07 | #34 사용자 답 "Keep maps as is" |
| 전투 보드 꾸미기는 장별 프리팹(`Assets/_Project/Art/Stages/<장 id>.prefab`)으로 굽고, 구운 뒤엔 프리팹이 기준(사람·에이전트가 직접 고침). 장별 조명도 프리팹 안에 두고, 베이크 조명이 필요한 장만 나중에 씬으로 | 시험 중 | 2026-11-09 | #101 "그래 뭐 그럼 그렇게 하자" |
| 무기는 쥐는 법(`Hold`: 칼날·창·자루·그대로)으로 붙이고, 루트 모션은 모델만 움직인다(칸 자리·걷기는 코드) | 시험 중 | 2026-11-07 | #42 요청 2 "모든 유닛이 도구를 정상적으로", "Loot Motion을 키는쪽으로" |
| 배치 때 아군은 북쪽, 적은 남쪽을 본다. 잠든 뱀파이어는 관만 보인다 | 시험 중 | 2026-11-07 | #42 요청 2 "적방향 (북쪽)", "관만 보여줄 것" |
| 관은 석관 `SM_Prop_Tomb_01`, 색칠 없이 | 시험 중 | 2026-11-08 | #82 "A, 색칠없이" |

## 맡길 때
- 유료 에셋(`Assets/Synty/`, `UPackage/`)은 git 에 올리지 않는다(공개 저장소, 라이선스, .gitignore). 새 클론은 docs/guides/asset-setup.md 대로 `bash tools/assets/setup.sh`. Synty 참조를 더하거나 빼면 `node tools/assets/scan.mjs` 로 목록을 다시 뽑아 같이 커밋한다
- 새 3D 에셋은 애니메이션에 거슬릴 부분(관절, 턱, 눈, 회전부, 튀어나오는 부분 등)을 무조건 별도 파츠로 나누고, 파츠 분해표에 분리 이유(어떤 애니메이션 때문인지)·피벗·미러·재사용 여부를 적는다
- 생성 API 키는 환경변수(OPENAI_API_KEY, MESHY_API_KEY)에서 읽는다. 프록시가 키를 넣는 환경(클라우드)에선 헤더 없이 보낸다. 프로세스에 키가 없으면(세션 재시작 전) 실행 직전 사용자 범위 환경변수에서 읽어 넣는다(값은 출력하지 않는다)
- 생성 API 가 모델 이름을 거부하면(예: "does not exist") 쓸 수 있는 모델을 확인해 보고한 뒤 바꾼다
- 함정: 유닛·칸 표시·마우스 고르기 높이는 모두 `BoardView.OnGround`/`Stand` 를 거친다. 새로 칸 위에 무언가 세울 때 `TileTop` 을 더하지 말고 이것을 쓴다 (battle-terrain.md 4)
- 함정: `TerrainData` 는 `heightmapResolution` 을 먼저 정하고 `size` 를 정한다. 거꾸로 하면 크기가 되돌아간다
- 함정: 꾸미기 프리팹 굽기는 프리팹이 없는 장만 짓는다(손맞춤 보존). 한 장을 규칙대로 다시 보려면 그 프리팹을 지우고 누른다. 맵 칸을 바꾸면 `StageDressingTests` 가 그 장을 잡는다 (docs/art/stage-dressing.md 4)
- 함정: 애니메이션 팩마다 Sidekick 전용판 클립(`A_MOD_*`)이 따로 있다. Sidekick 캐릭터에는 그쪽을 쓴다. 무기 자리 뼈는 Sidekick `prop_r`/`prop_l`, Polygon 캐릭터 `Hand_R`/`Hand_L`
- 함정: Sidekick 프리셋은 메시 하나라 모자 같은 부품만 끌 수 없다. 부품을 고르려면 Sidekick 도구로 새로 만든다 (위 "모델 바꾸기")
- 함정: 구운 캐릭터는 굽기마다 GUID·fileID 가 새로 정해진다. 에셋 참조로 가리키지 말고 `UnitLook.recipe` 이름으로 찾는다
- 함정: 애니메이션 패키지 임포트 때 `Rig Error: Copied Avatar Rig Configuration mis-match` 경고가 수백 개 나온다. Synty 쪽 설정이라 오류가 아니다. 이 클립을 PolygonDarkFantasy 캐릭터에 쓸 때 자세가 맞는지는 P7 에서 본다.
- 함정: SIDEKICK 패키지의 에디터 스크립트가 Synty 서버에서 Sidekick 도구를 스스로 내려받아 `Assets/DownloadCache/` 에 둔다 (gitignore 됨). 도구 데이터는 `Failed to load tool data` 로 불러오지 못했다. 완성 프리팹만 쓰고 도구는 들이지 않았다.
- 함정: Dark Fantasy 무기를 `prop_r` 에 회전 0 으로 붙이면 날이 몸 뒤로 간다(역수). 쥐는 법이 `AsIs` 인 칼·창이 그렇다. 새 무기는 쥐는 법을 꼭 고른다. `UnitLookTests` 가 몸 뒤로 뻗은 무기를 잡는다
- 함정: `Animator.Update(0)` 로 에디터에서 자세를 잡을 때 화면 밖이면 컬링돼 T 자세로 남는다. `cullingMode = AlwaysAnimate` 를 먼저 준다
- 함정: 애니메이션 패키지끼리 같은 파일이 있어 `Overriding existing asset with new GUID from package` 경고가 3건 나왔다. 그 파일을 참조할 때 GUID 가 나중 패키지 것이다.

## 보드에서 쓰는 것 (P6, `Assets/_Project/Art/ArtCatalog.asset`)

| 지형 | 표시 |
|---|---|
| 바닥 | 장별 Unity Terrain (꾸미기 프리팹 안, `Art/Stages/<장 id> Terrain.asset`). 칠은 `Art/Stages/Layers/` Grass·Dirt·Stone (Synty `Generic_Grass`·`Generic_Dirt`·`PolygonDarkFantasy_Stone_01`), 테마의 `floor`·`patches` (docs/art/battle-terrain.md) |
| 칸 | 지형에 붙은 홀로그램 격자 선 (`Overlay.mat`, `BoardView` 의 `Grid`). 빛 받는 칸·햇빛 칸은 채움 |
| 바위 `#` | 테마 막힌 칸 소품 (없으면 `SM_Env_Rock_01~03`) |
| 화톳불 `T` | `SM_Prop_Brazier_01` + `FX_Fire_01` + 점광원 |
| 햇빛 `S` | 위에서 비추는 스폿라이트 |
| 물 `~` | 지형을 파낸 웅덩이 + `Hex_Water.mat` 수면 |

장 선택 화면의 3D 원정 지도도 같은 두 팩(PolygonDarkFantasy, PolygonGeneric)만 쓴다. 지역별 프리팹·자리는 docs/art/campaign-map.md (#43, 구현 전).

장마다 모습이 따로 있다 (#34, docs/art/stage-dressing.md): `Assets/_Project/Art/Themes/<테마>.asset` (`BoardTheme`: 장 id, 바닥색, 막힌 칸 소품, 둘레 높은/낮은 소품, 안개·주변광). `ArtCatalog.themes` 가 목록. 막힌 칸은 테마 소품이 바위를 대신하고, 평지 칸은 테마 바닥색이다. 둘레는 굽기 때 칸 좌표로 고르고, 구운 뒤엔 장별 꾸미기 프리팹이 기준이다 (stage-dressing.md 4)

## 유닛에 쓰는 것 (#29, docs/art/unit-visuals.md)

병종 하나 = `Assets/_Project/Art/Units/<병종>.asset` (`UnitLook`: 모델, 뼈대, 무기와 손·쥐는 법·미세 조정, 동작 묶음). 쥐는 법과 소켓은 docs/art/unit-visuals.md 3-1, 한눈에 보기는 도구 모음 "유닛 모습 한눈에"(`UnitLineup`). `UnitLook.key`(병종·하수인 key)와 `icon`(무기 아이콘)이 있고, `ArtCatalog.allyLooks`·`minionLooks`(순서 상관없음, key 로 찾음), `vampireLook` 이 가리킨다.

| 유닛 | 모델 | 무기 | 동작 묶음 |
|---|---|---|---|
| 농민 창병 | `FantasyVillagers_05` | 쇠스랑 (창) | `Units/Spear.overrideController` |
| 농민 궁수 | `FantasyVillagers_04` | 활 `SM_Wep_Rigged_Bow_01` 왼손 | `Units/Bow` |
| 검사 | `FantasyKnights_05` | 검 (칼날) + 방패 | `Units/SwordShield` |
| 궁수 | `FantasyKnights_02` | 활 왼손 | `Units/Bow` |
| 기사 | `FantasyKnights_06` | 검 `Sword_02` (칼날) + 방패 | `Units/SwordShield` |
| 사냥꾼 | `SM_Chr_Hunter_Male_01` | 왼손 석궁 `SM_Wep_Rigged_CrossBow_01` (한 손 석궁) | `Units/PolygonBow.overrideController` (Polygon 활 쏘기, #99) |
| 장의사 (#99) | `SM_Chr_Gravedigger_Male_01` | 삽 `SM_Wep_Shovel_01` (자루) | `Units/Polygon` |
| 역병 의사 (#99) | `SM_Chr_PlagueDoctor_01` | 지팡이 `SM_Wep_Staff_01` (자루) | `Units/Polygon` |
| 스켈레톤 | `SM_Chr_Skeleton_01` | 검 `Sword_03` (칼날) | `Units/Polygon` |
| 스켈레톤 궁수 (#99) | `SM_Chr_Skeleton_Ranger_01` | 왼손 활 `SM_Wep_Rigged_Bow_01` (그대로) | `Units/PolygonBow` |
| 스켈레톤 중갑병 (#99) | `SM_Chr_Skeleton_HeavyArmor_01` | 철퇴 `SM_Wep_Mace_01` (칼날), 왼손 방패 `SM_Wep_Shield_01` | `Units/Polygon` |
| 뱀파이어 | `SM_Chr_DarkLord_Male_01` (잠든 동안은 석관 `SM_Prop_Tomb_01` 만, 깨면 모델) | 낫 (자루) | `Units/Polygon` |
| 스폰·시신 | 원래 병종 모델, 무기 없음 + 보라 고리 | | Sidekick 은 `Units/SidekickUnarmed` |

### 모델 바꾸기 (Sidekick 으로 새로 만든 캐릭터, #30)
1. Sidekick 도구에서 Save Character 로 `Assets/_Project/Art/Characters/<이름>.sk` 에 레시피를 저장한다 (영문 이름).
2. 그 병종 `Units/<병종>.asset` 의 `recipe` 에 `<이름>` 을 쓴다. `model` 은 지금 프리셋 그대로 둔다 (굽기 전 클론의 대체 모델).
3. 도구 모음 (Tools) > **Synty 다시 불러오기**, 또는 `/synty-reload` 스킬, 또는 `bash tools/assets/sidekick-bake.sh`. 무기 뼈 `prop_r`/`prop_l` 이 같아 무기 위치는 그대로 쓴다.
4. `.sk` 와 `UnitLook` 을 커밋한다. `Assets/_Generated/` 는 올리지 않는다.
- 파이프라인 견본: `Characters/SampleKnight.sk` (어느 유닛도 쓰지 않음, 시험이 실제로 굽게 하려고 둔다. 진짜 레시피가 생기면 지운다).
