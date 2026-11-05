# 다중 언어

## 경로
- 기획 방향: `docs/design/` (아직 언어 관련 방향 없음)
- 기획서: `docs/ui/localization.md` (계획 L1 에서 쓴다)
- 계획: `docs/dev/localization-plan.md`
- 코드: 아직 없음. Core 는 메시지(id + 값)만 내고, 문장은 Game 쪽 `Loc` 이 Unity Localization 표로 만든다(계획 3절)
- 패키지: `com.unity.localization` 1.5.14 (아직 설치 전)

## 결정
| 결정 | 상태 | 날짜 | 출처 |
|---|---|---|---|
| 다중 언어는 Unity Localization 패키지로 한다 | 시험 중 | 2026-11-06 | 사용자 원문 "Unity Localization으로 작업하자", 이슈 #23 |

## 열린 작업
- 계획 5절 정할 것: 지원 언어, docs/design 방향, 에디터 도구 포함 여부, 번역 방식
- 계획 L0~L6

## 맡길 때
- 모델: 구조 변경(L2 Core 메시지)은 opus, 표 뽑기 스크립트·화면 문자열 바꾸기는 sonnet
- 함정: Core 는 `noEngineReferences` 라 Unity Localization 을 직접 쓸 수 없다. Core 에 한글 문장을 다시 넣지 않는다
- 함정: HTML 비교 시험은 한국어 표로 고정해 돈다. 기준값(`tools/golden/out`)은 한국어다
