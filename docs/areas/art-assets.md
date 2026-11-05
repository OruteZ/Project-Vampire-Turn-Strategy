# 아트 에셋

유료 에셋이라 git 에 없다. 새 클론은 `UPackage/` 의 .unitypackage 를 `tools/import-unitypackage.sh UPackage/<파일>` 로 하나씩 임포트한다. worktree 는 메인 트리의 `Assets/Synty` 를 junction 으로 잇는다 (`.claude/skills/verify-unity/SKILL.md` Launch).

## 분류·경로

| 분류 | 경로 | 원본 패키지 | 내용 |
|---|---|---|---|
| 다크 판타지 환경·소품·캐릭터 | Assets/Synty/PolygonDarkFantasy | POLYGON_Dark_Fantasy | 프리팹 647. 스켈레톤 `SM_Chr_Skeleton_01`, 뱀파이어 후보 `SM_Chr_DarkLord_Male_01`, 사냥꾼 `SM_Chr_Hunter_Male_01`, 바위 `SM_Env_Rock_0*`, 화로 `SM_Prop_Brazier_01`, 불 `FX_Fire_01` |
| 공용 | Assets/Synty/PolygonGeneric | POLYGON_Dark_Fantasy | 프리팹 495 |
| 기사 캐릭터 | Assets/Synty/SidekickCharacters/Characters/FantasyKnights | SIDEKICK_Fantasy_Knights | `FantasyKnights_01`~`06` 완성 프리팹 |
| 마을 사람 캐릭터 | Assets/Synty/SidekickCharacters/Characters/FantasyVillagers | SIDEKICK_Fantasy_Villagers | `FantasyVillagers_01`~`05` 완성 프리팹 |
| 사람 몸 바탕 | Assets/Synty/SidekickCharacters/Characters/HumanSpecies | SIDEKICK 둘 다 | `HumanSpecies_01`~`04` |
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
- 관(coffin) 프리팹을 아직 못 찾았다 (계획 위험 4, P7)

## 맡길 때
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
