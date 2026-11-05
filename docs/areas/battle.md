# 전투

## 경로
- 기획: `docs/Prototype.html` (규칙·수치·스테이지의 원본. `docs/design/` 에는 아직 전투 기획 파일이 없다)
- 계획: `docs/systems/unity-port-plan.md`
- 코드: `Assets/_Project/Scripts/Core/` (규칙, Unity 참조 없음), `Assets/_Project/Scripts/Game/` (화면·입력)
- 테스트: `Assets/_Project/Tests/EditMode/`
- HTML 정답 도구: `tools/golden/prototype-golden.mjs` (설명은 `docs/systems/unity-port.md`)
- 씬: `Assets/_Project/Scenes/Main.unity` (Build Settings 0번)
- 검증: `.claude/skills/verify-unity/`

## 결정
| 결정 | 상태 | 날짜 | 출처 |
|---|---|---|---|
| 규칙·수치·스테이지·기록 문장을 HTML 과 똑같이 맞춘다 | 시험 중 | 2026-11-05 | 이슈 #1 사용자 원문 |
| 에디터 작업은 전부 Unity CLI 로 한다 | 시험 중 | 2026-11-05 | CLAUDE.md |

## 열린 작업
- #11 P7 유닛 모델과 전투 연출

## 맡길 때
- 모델: 규칙 이식(P4)은 opus xhigh, 나머지 구현은 opus low (`~/.claude/rules/pstack-models.md`)
- 함정: `GameData.cs` 는 생성 파일이다. 값은 HTML 을 고친 뒤 `node tools/golden/prototype-golden.mjs data` 와 `csharp` 로 바꾼다.
- 함정: `VampireSpawn.Core` 는 `noEngineReferences` 다. 규칙 코드에 `UnityEngine` 을 쓰면 컴파일이 깨진다. 화면에 필요한 것은 `Game` 쪽에 둔다.
