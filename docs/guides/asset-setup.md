# 유료 에셋 받아서 돌리기

이 저장소는 공개 저장소라 Synty 유료 에셋(`Assets/Synty/`, 원본 `UPackage/*.unitypackage`)을 올리지 않는다. 새로 클론한 사람은 에셋을 따로 받아 넣어야 게임과 테스트가 돈다. 이 문서는 그 순서와, 에셋 참조를 바꾸는 사람이 지킬 규칙이다.

## 무엇이 필요한가

지금 게임이 쓰는 유료 에셋 목록과 패키지는 아래 두 파일이 정본이다. 목록은 `tools/assets/required.tsv`(GUID, 경로, 패키지, 쓰는 파일), 패키지 표는 `tools/assets/packages.tsv`(파일 이름, 바이트, 필요한 에셋 수)다.

| 패키지 파일 | 필요 | 쓰는 곳 |
|---|---|---|
| `POLYGON_Dark_Fantasy_Unity_2022_3_v1_4_1.unitypackage` | 필수 | 스켈레톤·뱀파이어·사냥꾼 모델, 바위, 화로, 불 |
| `SIDEKICK_Fantasy_Knights_Unity_2021_3_v1_0_7.unitypackage` | 필수 | 검사·궁수·기사 모델 |
| `SIDEKICK_Fantasy_Villagers_Unity_2021_3_v1_0_1.unitypackage` | 필수 | 농민 창병·농민 궁수 모델 |
| `ANIMATION_Base_Locomotion_Unity_2021_1_v1_1_3.unitypackage` | 필수 | 서기·걷기 애니메이션 |
| `ANIMATION_Sword_Combat_Unity_2021_1_v1_2_0.unitypackage` | 필수 | 공격·맞기·죽기 애니메이션 |
| `POLYGON_Bow_Crossbow_Unity_2022_3_v1.2.1.unitypackage` | 필수 | 활·석궁 무기, 화살, 공용(PolygonGeneric) 소품 일부 |
| `ANIMATION_Bow_Combat_Unity_2022_3_v1_0_0.unitypackage` | 필수 | 궁수 서기·쏘기·맞기·죽기 애니메이션 |
| `INTERFACE_Dark_Fantasy_HUD_Unity_2022_3_v1_1_1.unitypackage` | 필수 | 전투 HUD 아이콘·프레임 |
| `INTERFACE_Dark_Fantasy_Menus_Unity_2022_3_v1_0_1.unitypackage` | 필수 | 메뉴 UI |
| `POLYGON_Werewolf_2022_3_v1_3_1.unitypackage` | 선택 | 웨어울프 모델 (#111, 아직 게임이 쓰지 않음. 다른 팩과 겹치는 공용 에셋만 목록에 있다) |
| `Free_UI_Click_Sound_Pack.unitypackage` | 필수 | UI 사운드 (Asset Store SwishSwoosh "Free UI Click Sound Pack" 을 받아 이 이름으로 바꿔 둔다. 공백 없는 이름이어야 setup.sh 가 읽는다) |

팀원은 회사 드라이브 받기 도구 NestResources 로 받는다(팀원에게만 배포, `NestResources.exe install-skill` 을 한 번 돌려 두면 `setup.sh` 가 빠진 패키지를 저절로 받는다). 그 밖의 사람은 Synty 를 산 계정(Synty Store 또는 Unity Asset Store)에서 받는다. 드라이브의 9개는 2026-11-09 에 파일 이름·바이트 크기가 위 표와 같음을 확인했다. 파일 이름의 판(`v1_4_1` 등)이 같아야 한다. 다른 판은 GUID 가 다를 수 있고, 그러면 아래 확인이 어느 에셋이 어긋났는지 알려 준다.

## 새 클론에서 하는 순서

1. Unity 6000.3.27f2 와 Unity CLI(`unity`)를 깐다. Git Bash 와 node 가 있어야 한다.
2. 위 패키지 아홉 개를 `UPackage/` 에 둔다. NestResources 스킬을 깔아 둔 팀원은 건너뛴다(4 의 `setup.sh` 가 받는다). 다른 곳에 두었으면 `SYNTY_PACKAGES=<폴더>` 를 주거나 스크립트에 폴더를 넘긴다.
3. 무엇이 빠졌는지 에디터 없이 본다:

   ```bash
   node tools/assets/check.mjs
   ```

   패키지마다 `준비됨`, `필요`(빠진 에셋 목록과 패키지 파일이 있는지), `선택` 중 하나가 나온다. 모두 준비됐으면 0 으로 끝난다.
4. 설치한다:

   ```bash
   bash tools/assets/setup.sh
   ```

   스크립트는 다음을 차례로 한다.
   1. 빠진 에셋이 든 패키지만 고른다.
   2. 에디터를 연다. 이미 열려 있으면 그것을 쓴다.
   3. 패키지를 하나씩 `AssetDatabase.ImportPackage` 로 임포트하고, 패키지마다 콘솔 오류를 보여 준다.
   4. `check.mjs` 를 다시 돌린다.
   5. Sidekick 레시피(`Assets/_Project/Art/Characters/*.sk`)를 굽는다 (`tools/assets/sidekick-bake.sh`, 결과는 gitignore 된 `Assets/_Generated/Resources/Characters/`).
   6. 에디터 안에서 `RequiredAssetsTests` 를 돌린다.

   Unity CLI 가 없으면 에디터에서 VampireSpawn > 도구 모음 (Tools) > 유료 에셋 > **Synty 다시 불러오기** 를 누른다. 빠진 패키지를 `UPackage/` 에서 임포트하고 레시피를 구운 뒤 결과를 그 창에 보인다 (처음 한 번은 굽기 도구 설치 → 컴파일 → 굽기 순서로 저절로 이어진다). 확인은 Window > General > Test Runner 에서 `SidekickRecipeTests`, `RequiredAssetsTests`. Claude Code 에서는 `/synty-reload` 스킬이 이 전체(임포트, 굽기, 확인)를 한다 (`.claude/skills/synty-reload/SKILL.md`). 굽지 않은 클론은 레시피를 쓰는 유닛을 `UnitLook.model` 의 대체 프리셋으로 그린다.

   빠진 패키지가 `UPackage/` 에 없고 NestResources 가 있으면(`NESTRESOURCES=<exe 경로>`, 없으면 `~/.claude/skills/nestresources/SKILL.md` 에 적힌 경로) 받을 팩과 용량을 보여 주고 물은 뒤 `UPackage/` 에 받는다. 묻지 않고 받으려면 `NESTRESOURCES_YES=1`. 그래도 패키지 파일이 없으면 무엇을 어디에 둘지 알려 주고 멈춘다. 에디터의 "Synty 다시 불러오기" 버튼은 받지 않으니 먼저 `setup.sh` 나 NestResources 로 받아 둔다. 처음 클론은 패키지 아홉 개 임포트에 수 분 걸린다.
5. 전체를 확인한다:

   ```bash
   bash .claude/skills/verify-unity/scripts/vu.sh test editor
   ```

   ```bash
   bash .claude/skills/verify-unity/scripts/vu.sh test playmode
   ```

## 에셋 없이 되는 것

- 규칙·데이터·UI 문장 테스트는 에셋을 쓰지 않게 짰다. `GameData`, `Hex`, `Battle`, `Campaign`, `Overlay`, `UiTextParity` 가 그렇다. 규칙은 `VampireSpawn.Core` 에 있고 Unity 그림에 기대지 않는다. 2026-11-05 GitHub 에서 새로 클론해(에셋 없음) 확인했다. setup.sh 가 패키지 다섯 개만 임포트했고(각 49~124초, 오류 0), EditMode 154/154, PlayMode 4/4 가 통과했다.
- 보드·유닛을 그리는 테스트(`BoardViewTests`, PlayMode `BattlePlaybackTests`)와 `RequiredAssetsTests.EveryRequiredAssetResolves` 는 에셋이 있어야 통과한다. 에셋 없이 실패하면 메시지가 이 문서를 가리킨다.

## 임포트 뒤에 바뀌는 파일

- SIDEKICK 에디터 스크립트는 `Packages/manifest.json` 에 `com.unity.formats.fbx` 를 넣는다. 이미 커밋돼 있어 보통 diff 가 없다.
- SIDEKICK 은 `Assets/DownloadCache/` 에 도구를 스스로 내려받는다. gitignore 돼 있다.
- 에디터를 열 때 `Sidekick Tool Downloader` 창 오류가 콘솔에 남는다. 게임과 테스트에는 영향이 없다.
- `setup.sh` 가 `Packages/` 나 `ProjectSettings/` 에 diff 가 생기면 알려 준다. 그대로 커밋하지 말고 무엇이 바뀌었는지 먼저 본다.

## 에셋 참조를 바꾸는 사람이 할 일

- `ArtCatalog`, 재질, 애니메이터, 씬에서 `Assets/Synty/` 의 새 에셋을 쓰거나 빼면 목록을 다시 뽑아 같이 커밋한다. 에셋과 `UPackage/` 가 다 있어야 한다.

  ```bash
  node tools/assets/scan.mjs
  ```

- 다시 뽑지 않으면 `RequiredAssetsTests.ListCoversEverySyntyReference` 가 실패하며 빠진 경로를 알려 준다.
- 새 패키지를 들이면 `UPackage/` 에 두고 `scan.mjs` 를 돌린다. `packages.tsv` 에 줄이 생긴다. 이 문서의 표와 `docs/areas/art-assets.md` 에도 한 줄 더한다.
- 패키지끼리 같은 경로의 파일이 있으면 나중에 임포트한 쪽 GUID 가 이긴다(애니메이션 패키지에 3건). `required.tsv` 의 패키지 칸에 `+` 로 둘 이상이 적힌 에셋은 여러 패키지에 같은 파일이 든 것이다(`PolygonGeneric` 은 POLYGON_Bow_Crossbow·POLYGON_Dark_Fantasy 둘 다에 있다). 어느 쪽을 임포트해도 된다.
