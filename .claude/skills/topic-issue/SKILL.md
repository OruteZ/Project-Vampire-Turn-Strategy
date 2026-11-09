---
name: topic-issue
description: How this project tracks work in GitHub topic issues (label 주제) and the board, and how checks are split between agent and user. Use when starting, continuing, committing, verifying, reporting or closing any task in this repo, when a user request or question arrives mid-task, or when something needs the user's check, decision or permission.
---

# 주제 이슈와 검증 위임

형식·명령 전체는 `docs/guides/work-status.md`. OZ = 사용자, GitHub 계정 OruteZ. 세션 시작 보드 목록은 SessionStart 훅이 넣어 준다 (사용자 대기 주제는 세션 첫 보고에 알린다).

## 주제 이슈 (사용자 결정 2026-11-08)
- 이슈 하나 = 큰 **주제** 하나(라벨 `주제`) = 세션 하나가 닫을 때까지 맡는 분량(사용자 대기 뒤에도 같은 세션). 하위 이슈·담당·종류 칸은 없다. 일은 본문 `## 흐름` 에 `기획:`/`구현:`/`사용자:` 단계로 일렬로 쌓는다
- 미작업 주제는 사용자가 맡길 때만 시작한다
- 새 일은 맞는 주제의 흐름에 더한다. 없을 때만 `node tools/board/board.mjs open "<제목>"` (OZ 는 `.github/ISSUE_TEMPLATE/topic.yml`)
- 다른 주제의 결과가 먼저 필요하면 `board.mjs after <#> <선행#>`. 선행이 열린 주제를 맡으면 먼저 알린다
- 요청: 사용자 원문을 `## 요청` 에 인용하고 원하는 것 한 줄마다 단계 하나 이상. 작업 중에 온 요청·질문도 받는 즉시 인용·단계를 더한다. OZ 가 쓴 `목표`·`요청` 문장은 고치지 않는다(해석·진행은 흐름과 댓글에)
- 상태: 시작하면 `board.mjs status <#> "작업 중"`. 다음 단계가 사용자 몫(확인, 결정, docs/design 쓰기, 내려받기·로그인 같은 준비, 허락)이면 `사용자:` 단계로 두고 `사용자 대기` (채팅에만 쓰지 않는다)
- 브랜치: 주제 일을 시작하면 바로 `git push -u origin HEAD` 하고 `## 관련` 에 `- 브랜치: <이름> (<날짜>)` 한 줄(세션마다, 지우지 않음)
- 단계를 끝내면 체크하고 그 줄에 커밋 해시, 댓글에 `확인: 어떻게 확인했는지`. 코드 커밋 메시지엔 주제 `#번호`. 할 일·상태만 바꾸는 커밋은 만들지 않는다. 못 하거나 미루는 단계는 체크하지 않고 이유를 댓글로
- 라벨: Unity 에디터(CLI 연결)가 필요한 주제는 `에디터 필요`, 사용자 몫이 아닌 막힘은 `막힘` + 이유 댓글
- 닫기·merge·보고 전에 흐름을 사용자 원문과 한 줄씩 대조한다. 보고엔 체크 안 된 단계를 주제별로 빠짐없이 적는다. 흐름이 다 끝나고 사용자 확인까지 받으면 닫는다(남은 단계는 다른 주제로 옮겨 링크한 뒤에만)
- 이슈 도구가 안 되면(gh 로그인 없음, 클라우드면 GitHub MCP) 사용자에게 알리고 채팅으로 보고한다

## 검증 위임 (사용자 결정 2026-11-08, 2026-11-09 고침)
- "문제없이 돌아가는가"(뜨는가·눌리는가·규칙대로 숫자가 바뀌는가·에러 없는가)는 에이전트가 에디터를 켜고 verify-unity 로 직접 확인한다. 통과한 항목은 `## 확인` 에서 체크하고 `(에이전트)` 를 붙인다
- "퀄리티 판단"(보기 좋은가·연출 느낌·어색함·번역 자연스러움)은 사용자 몫. 직접 플레이하는 퀄리티 확인은 에이전트가 하지 않는다. `## 확인` 에 체크리스트(항목마다 할 일·기대 결과 한 줄·`- [ ]`)를 쓰고 `사용자: 확인` 단계를 두고 `사용자 대기`
- 결과가 오면(이슈 체크·댓글·채팅) 단계를 체크하고, 실패 항목은 다음 `구현:` 단계로 고친 뒤 그 항목만 담은 `사용자: 다시 확인` 을 둔다. 결과 전에는 "확인됨" 으로 보고하지 않는다
- 에이전트가 돌리는 것은 클릭 없는 자동 검사뿐: 컴파일, EditMode·PlayMode 시험, `tools/ui-audit/sweep.sh`, 스크린샷. 스크린샷은 `verify-artifacts/` 에 두고(저장소 X) 경로를 주제 이슈 댓글에 적는다
