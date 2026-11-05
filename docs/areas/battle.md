# 전투

## 경로
- 기획: `docs/Prototype.html` (규칙·수치·스테이지의 원본. `docs/design/` 에는 아직 전투 기획 파일이 없다)
- 계획: `docs/systems/unity-port-plan.md`
- 코드: `Assets/_Project/Scripts/Core/` (규칙, Unity 참조 없음), `Assets/_Project/Scripts/Game/` (화면·입력)
- 테스트: `Assets/_Project/Tests/EditMode/`
- 씬: `Assets/_Project/Scenes/Main.unity` (Build Settings 0번)
- 검증: `.claude/skills/verify-unity/`

## 결정
| 결정 | 상태 | 날짜 | 출처 |
|---|---|---|---|
| 규칙·수치·스테이지·기록 문장을 HTML 과 똑같이 맞춘다 | 시험 중 | 2026-11-05 | 이슈 #1 사용자 원문 |
| 에디터 작업은 전부 Unity CLI 로 한다 | 시험 중 | 2026-11-05 | CLAUDE.md |

## 열린 작업
- #2 P1 프로젝트 골격과 에셋 임포트

## 맡길 때
- 모델: 규칙 이식(P4)은 opus xhigh, 나머지 구현은 opus low (`~/.claude/rules/pstack-models.md`)
- 함정: `VampireSpawn.Core` 는 `noEngineReferences` 다. 규칙 코드에 `UnityEngine` 을 쓰면 컴파일이 깨진다. 화면에 필요한 것은 `Game` 쪽에 둔다.
