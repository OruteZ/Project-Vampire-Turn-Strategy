---
name: pstack-unity
description: What to replace when following pstack (/pstack:poteto-mode, its playbooks, poteto-agent delegates) in this repo, verification and PR/commit/branch steps. Read right after pstack's own Claude Code adapter, before any playbook step.
---

# pstack 을 이 저장소에서 쓸 때

pstack 은 그대로 따르되, 아래 두 가지는 이 표대로 바꿔 읽는다. 이 파일이 pstack 보다 앞선다. 나머지(원칙, how·why·architect·arena 등)는 그대로다.

- `poteto-agent` 등 pstack 에이전트에게 주는 지시엔 이 파일의 절대경로를 넣고 "pstack 보다 이 파일 먼저" 라고 쓴다 (훅이 경로가 없는 지시를 막는다)
- 작업 추적은 topic-issue 스킬을 따른다. pstack 의 todo 목록·show-me-your-work 기록은 주제 이슈의 흐름·댓글을 대신하지 않는다

## 검증 (Prove It Works, Bug fix 1·4, Feature 5, Prototype 5, Visual parity 4, Shipping 1)

| pstack | 이 저장소 |
|---|---|
| matching surface, `control-ui`·`control-cli` (cursor-team-kit, 없음) | verify-unity 스킬의 `vu.sh`. 에디터를 직접 띄우지 않는다(훅) |
| 직접 재현·확인, 사용자에게 넘기지 않기 (Bug fix 1, Never Block on the Human) | 에이전트는 클릭 없는 자동 검사만: 컴파일(`vu.sh doctor`), `vu.sh test editor\|playmode`, `tools/ui-audit/sweep.sh`, `vu.sh key`·`click` 으로 재현, 스크린샷, 읽기 전용 `eval`. "돌아가는가" 는 여기서 끝낸다 |
| 보기·느낌·어색함·번역 판단도 에이전트가 결정 | 사용자 몫. 주제 이슈 `## 확인` 에 체크리스트, `사용자: 확인` 단계, 상태 `사용자 대기` 로 두고 결과를 기다린다. 결과 전엔 "확인됨" 으로 보고하지 않는다. 이것은 사람을 막는 질문이 아니라 정해진 단계다 |
| "Inconclusive is not a pass" | 에디터 슬롯이 없거나 에디터가 같은 프로젝트를 열고 있으면 그 단계는 체크하지 않고 "빌드 미검증(에디터 한도/에디터 실행 중)" 으로 보고 |
| 독립 검증자 여럿을 동시에 (Shipping 1, swarm 검증) | 에디터는 프로젝트 폴더 하나에 하나, 컴퓨터 전체 2개. 검증은 에디터가 열린 트리에서 한 번에 하나씩 |
| `tdd` 의 실패하는 시험 먼저 | EditMode 시험(`Assets/_Project/Tests/EditMode`)을 먼저 커밋하고 `vu.sh test editor <filter>` 로 실패 → 통과를 보인다 |
| Bugbot, 클라우드 검증 에이전트 | 없음. 건너뛴다 |

## PR·커밋·브랜치 (Opening a PR, Shipping, Babysit, Multi-phase, Autopilot)

| pstack | 이 저장소 |
|---|---|
| main 에서 worktree, `isolation: "worktree"` | worktree 스킬: `.claude/worktrees/<이름>` 에 dev 에서 `claude/<이름>` 브랜치. 에디터를 쓸 트리는 junction(`Assets/Synty`, `UPackage`)을 걸고, 지우기 전에 끊는다 |
| Commit liberally 뒤 rebase 로 정리, amend | 작게 커밋하고 커밋마다 바로 push(topic-issue 3절). push 한 커밋은 rebase·amend·force-push 하지 않는다 |
| `git fetch && git reset --hard origin/<branch>` | 쓰지 않는다. 다른 에이전트 트리의 브랜치도 바꾸지 않는다(훅) |
| Conventional Commits `type(scope): subject`, 영어, technical-writing | 한국어 한 줄 요약 + 주제 `#번호` (`git log` 의 모양). 할 일·상태만 바꾸는 커밋은 만들지 않는다 |
| PR 열기, 스택 PR, Babysit, Shipping, auto-merge | 작업 PR 은 열지 않는다. 끝나면 브랜치에 dev 를 merge, 시험 통과 뒤 `git push origin HEAD:dev`. dev → main 은 사용자가 정할 때만 |
| 답변의 PR 링크, Verification 절 | 주제 이슈 링크, 단계 체크·커밋 해시, `확인:` 댓글. 체크 안 된 단계를 빠짐없이 적는다 |
