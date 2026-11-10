---
name: worktree
description: Rules for git worktrees and branches in this repo, including the paid-asset junctions and how finished work reaches dev. Use when creating or removing a worktree, switching or merging branches, finishing a task and pushing to dev, or when another agent may be using the main tree.
---

# 작업 트리·브랜치

- 메인 작업 트리(저장소 폴더)는 한 번에 한 에이전트만 쓴다. 다른 에이전트는 `git worktree add` 로 작업 트리를 따로 만든다
- 새 작업 트리는 유료 에셋이 git 에 없으니 메인 트리로 junction 을 건다: `cmd //c mklink //J "<트리>\Assets\Synty" "<메인 트리>\Assets\Synty"` (`UPackage` 도 같게)
- 다른 작업 트리의 브랜치를 바꾸지 않는다(훅이 `git -C <남의 트리> checkout|switch` 를 막는다). dev 가 메인 트리에 체크아웃돼 있으면, merge 는 메인 트리가 깨끗한지 확인한 뒤 `git -C <메인 트리> merge --no-ff` 로 한다
- 파일 쓰기는 항상 절대경로로 한다 (상대경로는 다른 트리에 잘못 쓰일 수 있다)
- 끝난 worktree 는 제거한다. 그 전에 그 트리의 에디터를 `VU_PROJECT=<트리> bash .claude/skills/verify-unity/scripts/vu.sh close` 로 닫고, junction(`Assets/Synty`, `UPackage`)을 `cmd //c rmdir` 로 먼저 끊는다. Windows 의 `git worktree remove` 는 junction 을 따라가 메인 트리의 유료 에셋을 지운다 (훅이 junction 이 남은 remove 를 막는다)
- 작업이 끝나면 작업 브랜치에 dev 를 먼저 merge 하고 테스트가 통과한 뒤 dev 에도 바로 push 한다 (`git push origin HEAD:dev`, fast-forward)
