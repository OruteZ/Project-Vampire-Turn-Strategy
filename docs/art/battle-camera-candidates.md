# 전투 카메라 연출 후보 (레퍼런스 조사)

> 출처: docs/design/overview.md, combat.md, vampire.md, ui.md, art.md (커밋 f7fc7f8)
> 상태: **후보 보고서**. 사용자가 고른 항목만 기획서(수치·순서·화면)로 옮긴다. 아직 기획서가 아니다.

## 1. 지금 카메라

- `BoardView.FitCamera`: 보드 전체가 보드 영역에 들어오게 고정. 피치 55°, 회전·팬·줌 없음.
- 연출 훅: `BattleView.Play` 가 이벤트(`Walk`, `Hit`, `Died`, `CorpseLeft`, `Rose`, `Woke`, `Banner`, `Popup`)를 하나씩 재생하고 `speed` 로 대기 시간을 나눈다. 카메라 연출은 이 이벤트에 붙이면 된다.
- 패키지: URP 17.3 있음(Volume 후처리 가능), Cinemachine 없음.

## 2. 레퍼런스에서 배운 것

| 게임 | 카메라 연출 | 우리에게 주는 교훈 |
|---|---|---|
| XCOM 2 | 어깨 너머 액션캠, 처치 시 다이내믹 앵글, 적 턴 따라가기 | 처음엔 재미있지만 반복되면 "끄고 싶다"는 반응. 처치 뒤 1~2초 더 비추는 것이 템포를 늘어지게 한다 |
| Gears Tactics | 처형(빈사 적) 시 근접 시네마틱, 활성 유닛으로 카메라 포커스 키 | 특별한 행동에만 시네마틱을 주면 무게가 생긴다 |
| Into the Breach | 카메라 고정, 작은 화면 흔들림·번쩍임만 | 모든 정보가 한 화면에 보이는 퍼즐형 전투는 카메라를 움직이지 않고 타격감만 준다 |
| Darkest Dungeon | 타격마다 줌·흔들림·기울임, 치명타 줌 | 일반 타격까지 줌하면 멀미·피로 불만. 모더들도 "치명타만 남기기"를 고름 → 강한 순간에만 |
| Fire Emblem / Advance Wars | 별도 전투 장면으로 전환(끌 수 있음) | 보드 정보가 끊긴다. 끄기 옵션이 사실상 필수 |
| Desperados III (art.md 참고작) | 비스듬한 위 시점, 넓게 보는 기본 시점, 휠 줌·회전 | 기본은 넓게, 플레이어가 원할 때만 가까이 |
| Gloomhaven (art.md 참고작) | 보드게임 같은 위 시점, 연출 최소 | 맵 아트가 주인공이 되도록 카메라를 크게 흔들지 않음 |
| Battle Brothers / Wartales | 적 이동마다 카메라가 따라가며 속도 설정 | 행동 예고가 없을 때 "무슨 일이 있었는지"를 보여 주는 것이 카메라의 일 |

공통 원칙(조사에서 추린 것):
1. **연출은 드물수록 세다.** 매 타격이 아니라 게임의 정체성을 말하는 순간에 쓴다.
2. **짧게, 건너뛸 수 있게.** 한 전투 10~20분, 거의 매 턴 깊은 생각(combat.md). 연출은 0.3~1.5초, 클릭·스페이스로 건너뛰기, 재생 속도(`speed`)를 따라 줄고, 옵션에서 끌 수 있다.
3. **끝나면 항상 보드 전체 시점으로 돌아온다.** 플레이어 턴 입력은 언제나 기본 시점에서 시작한다(ui.md 가시성).
4. **흔들림은 약하게, 끌 수 있게.** 멀미 불만은 흔한 함정. 흔들림 세기 옵션(0 포함).
5. **결정적이다.** 무작위 없음(overview.md). 흔들림도 고정 패턴(사인·고정 시드 노이즈)으로 같은 상황에 같은 모양.

## 3. 후보

등급: **A 추천**(정체성·가독성에 직결), **B 있으면 좋음**, **C 보류**, **X 넣지 않기 권장**.

| # | 후보 | 붙일 이벤트 | 레퍼런스 | 등급 | 이유 |
|---|---|---|---|---|---|
| 1 | **변이 연출**: 시신 쪽으로 천천히 당기고(푸시인), 색을 빼고 짧은 슬로, 스폰이 일어서면 원위치 | `Rose` | XCOM 처치캠, Darkest Dungeon 치명타 | **A** | "한 명이 물리면 전열이 무너진다"(overview.md)의 순간. 가장 먼저 넣을 것 |
| 2 | **연쇄 변이 따라가기**: 한 명령에 변이가 여럿이면 차례로 짧게 훑기 | 연속 `Rose` | XCOM 적 턴 | **A** | 연쇄 붕괴 위협(vampire.md)을 눈으로 세게 보여 줌. 1번의 짧은 판 |
| 3 | **적 턴 따라가기**: 움직이는 적 쪽으로 살짝 팬·줌 후 복귀 (보드 밖으로 안 나감) | 적 진영 `Walk`·`Hit` | XCOM, Battle Brothers | **A** | 적 행동 예고가 없으므로(combat.md) 무슨 일이 일어났는지 따라가게 해 줌 |
| 4 | **보름달 흡혈**: 예고 때 표적에 짧은 포커스+달빛 비네트, 실행 때 뱀파이어→표적 포커스 | 보름달 예고/흡혈 | Darkest Dungeon 보스, XCOM | **A** | 흡혈은 유일한 예고 대상(vampire.md). "미리 예측"과 "압박"을 카메라로 강조 |
| 5 | **타격 흔들림 + 히트 스톱**(0.05~0.1초 정지) | `Hit` | Into the Breach, Darkest Dungeon | **B** | 값싸고 타격감 큼. 일반 타격은 아주 약하게, 큰 피해·보스만 세게 |
| 6 | **처치 짧은 줌**(치명적 처치만: 뱀파이어·스폰·마지막 적) | `Died` | XCOM 킬캠, Darkest Dungeon 치명타만 | **B** | 모든 처치는 피로. 의미 있는 처치만 |
| 7 | **전투 시작 훑기**: 뱀파이어 보스 → 맵 → 기본 시점으로 내려앉기 (1~2초, 건너뛰기) | 전투 시작 | Desperados III, Wartales, 보스전 소개 | **B** | 보스가 "직접 싸우는 강자"(vampire.md)임을 첫 장면에 보여 줌 |
| 8 | **승리·패배 마무리**: 마지막 일격 슬로+줌, 패배는 마지막 아군에 천천히 당기기 | 마지막 `Died` | XCOM, Fire Emblem | **B** | 10~20분 전투의 마침표. 한 전투에 한 번이라 피로 없음 |
| 9 | **아군 직접 처치**: 짧은 정지 + 약한 줌 | 아군 처치 명령 | Gears 처형 | **B** | 희생을 고르는 무게(combat.md "희생할 유닛") |
| 10 | **진영 턴 전환**: 배너와 함께 아주 살짝 줌 아웃·인 | `Banner` | XCOM 턴 전환 | **B** | 진영별 턴(ui.md) 경계를 느끼게. 비용 작음 |
| 11 | **플레이어 카메라 조작**: 휠 줌(2~3단계), WASD·가장자리 팬, 기본 시점 복귀 키 | 입력 | Desperados III, Gears, XCOM | **C** | 연출이 아니라 조작. 보드가 한 화면에 다 보여 지금은 꼭 필요하지 않음. 큰 맵이 생기면 다시 |
| 12 | **카메라 회전**(Q/E) | 입력 | Desperados III, Gears | **C** | 남쪽엔 낮은 소품만 둔 장식 규칙(stage-dressing.md 1-5)과 충돌. 소품이 유닛을 가림 |
| 13 | **어깨 너머 액션캠** | `Hit` | XCOM, Gears | **X** | 반복 피로가 레퍼런스의 공통 불만. 로우폴리 근접샷 품질·보드 정보 단절 |
| 14 | **별도 전투 장면 전환** | `Hit` | Fire Emblem, Advance Wars | **X** | 보드 정보가 끊기고 템포가 늘어짐 |

## 4. 구현 메모 (고른 뒤 기획서·docs/dev 로)

- `FitCamera` 가 `LateUpdate` 에서 화면비가 바뀌면 위치를 다시 쓴다. 연출은 기본 자세(FitCamera 결과) 위에 얹는 오프셋으로 두고, 끝나면 오프셋 0 으로 돌아오게 하면 서로 안 싸운다. Cinemachine 은 들이지 않아도 된다.
- 연출 대기는 `BattleView.Wait` 를 그대로 써서 `speed` 를 따른다. `speed = 0`(즉시)이면 연출도 없다.
- 1·4·8 의 색 빼기·비네트는 URP Volume(Color Adjustments, Vignette)로 한다.
- 옵션 두 개: 카메라 연출 켜기/끄기, 흔들림 세기.

## 5. 사용자에게 물을 것

1. A 등급(1~4)으로 첫 판을 시작해도 되는지, 더하거나 뺄 것.
2. B 등급 중 같이 넣을 것.
3. 11번(줌·팬 조작)을 이번 범위에 넣을지.

## 참고 자료

- [XCOM 2 리뷰 (Gaming Nexus)](https://www.gamingnexus.com/Article/5249/XCOM-2/), [GameSpot: XCOM 2](https://www.gamespot.com/articles/how-xcom-2s-challenge-goes-deeper-than-just-permad/1100-6432989/)
- [Gears Tactics 처형 (gamepressure)](https://www.gamepressure.com/gears-tactics/execution-how-to-perform-benefit/zed3dc), [PCGamesN](https://www.pcgamesn.com/gears-tactics/gameplay), [카메라 조작 (Twinfinite)](https://twinfinite.net/guides/gears-tactics-rotate-camera/)
- [Into the Breach Design Postmortem (GDC Vault)](https://gdcvault.com/play/1025772/-Into-the-Breach-Design), [Sprucing up cameras with math (GDC 2016)](https://gamedeveloper.com/programming/video-sprucing-up-cameras-with-math)
- [Darkest Dungeon 줌 모드 토론 (Steam)](https://steamcommunity.com/app/262060/discussions/4/618453594749180956/), [멀미 불만 (Steam)](https://steamcommunity.com/app/262060/discussions/0/612823460256613837), [Darkest Dungeon II 전투 연출 도구 (GDC Vault)](https://gdcvault.com/play/1034466/Independent-Games-Summit-Critical-Combat)
- Fire Emblem·Advance Wars·Desperados III·Gloomhaven·Battle Brothers·Wartales: 게임 플레이 기억 기준(별도 출처 없음)
