# 유닛 모델·무기·애니메이션 재점검 계획

사용자 요청: "창병·궁병에 어울리지 않는 복장의 모델을 쓰고 있다. 무기 모델과 애니메이션도 적용하고 싶다."
이 계획은 하는 순서와 각 단계의 확인 방법만 정한다. 실행은 사용자가 "시작" 하면 한다.

## 지금 상태 (2026-11-06 조사)

| 유닛 | 지금 모델 | 무기 | 애니메이션 |
|---|---|---|---|
| 농민 창병 | `FantasyVillagers_01` (마을 사람 옷) | 없음 | `Unit.controller` 하나 (기본 이동 + 검 전투 클립) |
| 농민 궁수 | `FantasyVillagers_03` | 없음 | 같음 |
| 검사 | `FantasyKnights_02` | 없음 | 같음 |
| 궁수 | `FantasyKnights_05` (판금 기사) | 없음 | 같음 |
| 기사 | `FantasyKnights_01` | 없음 | 같음 |
| 사냥꾼 | `SM_Chr_Hunter_Male_01` | 없음 | 같음 |
| 스켈레톤·뱀파이어 | `SM_Chr_Skeleton_01`, `SM_Chr_DarkLord_Male_01` | 없음 | 같음 |

가진 재료 (`Assets/Synty`, docs/areas/art-assets.md):

- 무기 (`PolygonDarkFantasy/Prefabs/Weapons`): 쇠스랑 `Pitchfork_01`, 장창 `Polearm_01`, 미늘창 `Halberd_01·02`, 검 `Sword_01~03`, 방패 `Shield_01`, 석궁 `Crossbow_01`(+화살, 리깅판), 낫 `Scythe_01`, 도끼·메이스·망치 등.
- 활: 사용자가 `POLYGON_Bow_Crossbow` 팩을 더해 임포트했다 (`Assets/Synty/PolygonBowCrossbow`). 시위까지 리깅된 활 `SM_Wep_Rigged_Bow_01`·`_NativeAmerican_01`, 화살 `SM_Prop_Arrow_01`, 리깅된 석궁 `SM_Wep_Rigged_CrossBow_01`, 석궁 화살.
- **창 전용 애니메이션은 없다.** 검 전투 팩(공격 84개)과 기본 이동 팩만 있다.
- 캐릭터: Sidekick 완성 프리팹(마을 사람 5, 기사 6), PolygonDarkFantasy 캐릭터 15(사냥꾼 남·여, 무덤지기, 사제, 역병 의사 등). Sidekick 조립 도구는 불러오지 못해(art-assets.md 함정) 부품 조합은 지금은 안 된다.

## 정할 것 (사용자)

1. **각 병종의 복장.** 3단계의 후보 사진을 보고 고른다. 기획 에이전트가 병종마다 추천 하나를 붙인다.
2. ~~활이 없는 문제~~ 해결: (다) 사용자가 `POLYGON_Bow_Crossbow` 를 넣었다 (#29).
3. **창 공격 동작.** 창 전용 클립이 없다. 검 팩의 찌르기 클립을 쓴다(추천), 또는 창 애니메이션이 든 팩을 산다.

## 단계

각 단계는 이슈 하나, 확인이 끝나야 다음으로 간다. 에디터 작업은 모두 Unity CLI (CLAUDE.md).

### 1. 이슈와 기획 방향 읽기
- [ ] `작업: 유닛 모델·무기·애니메이션` 이슈를 열고 사용자 원문을 맨 위에 인용한다.
- [ ] docs/design/art.md, units.md, docs/areas/art-assets.md 를 읽는다. 지금 docs/design 에는 병종별 복장 방향이 없다. 복장 방향은 사용자 결정으로 art-assets.md 결정 표에 남긴다.

### 2. 묶음 시험 (팩끼리 맞는지 먼저 본다)
위험한 가정을 실제로 돌려 본다. 결과에 따라 3~6단계가 바뀐다.
- [ ] Sidekick 캐릭터에 활 팩 클립(남성 `Masc`)과 검 팩 클립을 Humanoid 로 입혀 자세가 깨지지 않는지 본다. 확인: 정지 화면 스크린샷 클립마다 1장.
- [ ] 손 뼈에 무기를 붙일 자리(뼈 이름, 방향)를 Sidekick 과 PolygonDarkFantasy 캐릭터에서 찾는다. 확인: 쇠스랑·검·석궁을 쥔 스크린샷.
- [ ] 검 팩 찌르기 클립이 창 공격으로 읽히는지 본다. 확인: 공격 3프레임 스크린샷.

#### 2단계 결과 (#29, 2026-11-06)

- 클립은 팩마다 Sidekick 전용판이 있다 (`A_MOD_*`: 기본 이동 `A_MOD_BL_*`, 검 `A_MOD_SWD_*`, 활 `A_MOD_BOW_*`). 지금 `Unit.controller` 는 Polygon 판 클립을 Sidekick 캐릭터에도 쓴다. Sidekick 유닛은 `A_MOD_*`, Polygon 캐릭터(사냥꾼·스켈레톤·뱀파이어)는 Polygon 판을 쓴다.
- 무기 자리 뼈: Sidekick 은 `prop_r`, `prop_l` (오른손·왼손), Polygon 캐릭터는 `Hand_R`, `Hand_L`. 붙이면 클립을 따라 움직인다.
- 활 당기기(`Stand_Aiming_Drawn`)와 활 든 대기(`Stand_Idle_BowDown`), 검·방패가 맞는다. 검 찌르기(`HeavyStab01`)는 창 공격으로 읽힌다. 창은 쥐는 위치(자루 가운데)를 맞춰야 한다. 스크린샷 docs/review/unit-fit-weapons.png.
- Sidekick 프리셋은 메시 하나로 합쳐져 있어 부품(모자, 어깨의 양)만 끌 수 없다. 부품을 바꾸려면 Sidekick 조립 도구가 필요하고, 그 도구는 지금 불러오지 못한다. 그래서 복장은 있는 프리셋 20개 중에서 고른다.

### 3. 후보 사진첩 (복장 고르기)

사진첩: docs/review/unit-candidates-0.png (마을 사람 V01~05, 맨몸 H01~03), -1.png (기사 K01~06), -2.png (PolygonDarkFantasy 사냥꾼 남·여, 무덤지기, 사제, 역병 의사, 마녀).

| 병종 | 지금 | 추천 모델 | 무기 | 이유 |
|---|---|---|---|---|
| 농민 창병 | V01 (어깨에 양) | **V05** 대장장이 남자 | 쇠스랑 `Pitchfork_01` | 싼 소모품 근접. campaign.md "마을 사람들이 창을 들고". 쇠스랑이 농민을 바로 보인다 |
| 농민 궁수 | V03 (요리사 모자) | **V04** 앞치마 여자 | 활 `Rigged_Bow_01` | 마을 사람 줄에서 우스운 소품이 없는 것은 V04·V05 뿐이다. 창병과 다른 얼굴이어야 판에서 갈린다 |
| 검사 | K02 | **K05** 두건 쓴 판금 | 검 `Sword_01` + 방패 `Shield_01` | 정규군 근접. 얼굴이 보이는 가벼운 갑옷이 기사와 갈린다 |
| 궁수 | K05 (판금) | **K02** 곰 가죽 두건 | 활 `Rigged_Bow_01` | 정규군 원거리. 기사 줄에서 가장 가볍고 사냥꾼 느낌 |
| 기사 | K01 | **K06** 깃털 투구 | 검 `Sword_02` + 방패 | 비싼 주력. 투구로 얼굴을 가린 판금이 가장 기사답다 |
| 사냥꾼 | Hunter_Male | 그대로 | 석궁 `Rigged_CrossBow_01` | 뱀파이어 전문. 지금 UI 아이콘도 석궁 |
| 스켈레톤 | Skeleton_01 | 그대로 | 검 `Sword_03` | 하수인 |
| 뱀파이어 | DarkLord_Male | 그대로 | 낫 `Scythe_01` | UI 아이콘(낫)과 맞춘다 |
- [ ] 병종마다 후보 3~4개(Sidekick 프리팹, PolygonDarkFantasy 캐릭터)에 그 병종 무기를 들려 같은 각도·조명으로 찍는다. 확인: `docs/review/unit-candidates-<병종>.png`.
- [ ] 기획 에이전트(opus)가 docs/design 과 units.md 축(근접·원거리, 소모품·주력, 뱀파이어 전문)으로 병종마다 추천 하나와 이유를 붙인다.
- [ ] 사용자에게 사진첩과 추천을 보이고 고르게 한다 (정할 것 1~3).

### 4. 기획서
- [ ] 기획 에이전트가 `docs/art/unit-visuals.md` 를 쓴다. 병종(스켈레톤·뱀파이어·스폰 포함)마다 모델, 무기와 쥐는 손, 방패, 애니메이션 묶음(대기·이동·공격·맞음·죽음), 원거리 공격의 화살 여부, 고른 이유. 출처 커밋을 적는다.
- [ ] 활을 만들기로 했으면 같은 문서에 활 파츠 분해표(파츠, 나눈 이유, 피벗, 미러·재사용)를 넣는다 (CLAUDE.md 에셋 파츠 분리).

### 5. 무기 들기 구현
- [ ] 유닛 모습 데이터: `ArtCatalog` 의 병종별 모델 배열을 "모델 + 무기 + 붙일 뼈 + 위치·회전 + 애니메이터" 한 줄짜리 표로 바꾼다. `UnitView.Create` 가 이 표 한 줄로 유닛을 만든다.
- [ ] 위치·회전 맞추기는 도구 모음 창에 "유닛 무기 맞추기" 버튼 하나로 연다 (CLAUDE.md 메뉴 규칙). 유닛을 띄우고 값을 바꿔 바로 보이고 저장한다.
- [ ] (가)를 골랐으면 활을 생성·분리해 `Assets/_Project/Art/Weapons/` 에 둔다. 생성 에셋은 git 에 올린다(유료 에셋이 아님).
- [ ] 확인: 모든 병종 무기를 든 정지 스크린샷, `node tools/assets/scan.mjs` 다시 뽑기.

### 6. 애니메이션
- [ ] `Unit.controller` 를 바탕으로 무기별 `AnimatorOverrideController`(창, 검·방패, 활 또는 석궁, 맨손 스켈레톤, 뱀파이어)를 만든다. 상태와 트리거 이름(Idle, Walk, Attack, Hit, Death)은 그대로 둬서 `BattleView` 를 바꾸지 않는다.
- [ ] 원거리 공격: 쏘는 클립에 맞춰 화살이 날아가게 할지는 4단계 기획서대로. 날린다면 `BattleView` 공격 재생 시간을 클립 길이에 맞춘다.
- [ ] 확인: 병종마다 이동·공격·맞음·죽음을 재생한 스크린샷(verify-unity), `BattlePlaybackTests`(PlayMode)와 EditMode 시험 통과, 데모 전투 한 판을 끝까지 돌려 콘솔 오류 없음.

### 7. 마무리
- [ ] docs/areas/art-assets.md "유닛에 쓰는 것" 표와 결정 표, docs/dev 구현 기록, 스크린샷 `docs/review/unit-*.png`.
- [ ] dev merge, 시험 통과 후 dev push, 이슈 닫기.

## 맡길 때

- 기획(3·4단계 추천, 기획서)은 opus. 정해진 구현(5·6단계)은 sonnet, 활 생성과 파츠 분리 검토는 opus.
- 2단계 결과가 나쁘면(클립이 안 맞음) 3단계 전에 사용자에게 알린다. 그때는 애니메이션 팩 선택부터 다시 정한다.
