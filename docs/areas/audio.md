# 사운드

## 경로
- 기획: docs/design 에 아직 없음 (생기면 1순위)
- 기획서: docs/audio/sounds.md
- 코드: Assets/_Project/Scripts/Game/Audio/ (재생), Core/BattleEvent.cs (`BannerKind`·`PopupKind`)
- 구현 기록: docs/dev/sound.md
- 데이터: Assets/_Project/Audio/SoundCatalog.asset (ArtCatalog.sound 에 걸려 있음), UnitLook.sounds. 사운드 파일도 Assets/_Project/Audio/ 아래에 둔다 (git 에 올리는 팩은 Packs/<팩>, Asset Store 팩은 패키지가 정한 폴더·gitignore)

## 결정
| 결정 | 상태(시험 중/확정 후보) | 날짜 | 출처(이슈·사용자 원문) |
|---|---|---|---|
| 사운드는 카탈로그의 타입 있는 필드로 고르고, 이름(문자열)으로 찾지 않는다 | 시험 중 | 2026-11-09 | #105 "확장 가능하게 이름으로 부르는 짓거리같은거 하지말고" |
| 원본 재배포만 막는 무료 팩(400 Sounds Pack)은 git 에 올린다 (Assets/_Project/Audio/Packs/). Asset Store 팩은 UPackage 로 git 밖 | 시험 중 | 2026-11-10 | #105 "깃에 넣은건 정확히는 배포가 아니지" |

## 맡길 때
- 모델: 칸 더하기·연결은 sonnet, 사운드 방향·목록은 opus
- 함정: 무작위 없음 원칙 — 변형은 차례로 돌리고 피치를 흔들지 않는다
- 함정: 같은 순간에 두 소리가 겹치지 않게. 피해 팝업·뱀파이어 회복 팝업은 소리가 없다 (맞음·흡혈 소리가 이미 난다)
