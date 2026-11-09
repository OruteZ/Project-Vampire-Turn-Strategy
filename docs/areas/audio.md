# 사운드

## 경로
- 기획: docs/design 에 아직 없음 (생기면 1순위)
- 기획서: docs/audio/sounds.md
- 코드: Assets/_Project/Scripts/Game/Audio/ (재생), Core/BattleEvent.cs (`BannerKind`·`PopupKind`)
- 구현 기록: docs/dev/sound.md
- 데이터: SoundCatalog 에셋 (ArtCatalog.sound 에 건다), UnitLook.sounds

## 결정
| 결정 | 상태(시험 중/확정 후보) | 날짜 | 출처(이슈·사용자 원문) |
|---|---|---|---|
| 사운드는 카탈로그의 타입 있는 필드로 고르고, 이름(문자열)으로 찾지 않는다 | 시험 중 | 2026-11-09 | #105 "확장 가능하게 이름으로 부르는 짓거리같은거 하지말고" |

## 맡길 때
- 모델: 칸 더하기·연결은 sonnet, 사운드 방향·목록은 opus
- 함정: 무작위 없음 원칙 — 변형은 차례로 돌리고 피치를 흔들지 않는다
- 함정: 같은 순간에 두 소리가 겹치지 않게. 피해 팝업·뱀파이어 회복 팝업은 소리가 없다 (맞음·흡혈 소리가 이미 난다)
