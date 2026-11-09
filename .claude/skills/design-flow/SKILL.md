---
name: design-flow
description: How game design flows here, docs/design (human direction) → spec docs (기획서) → implementation, plus the per-area guides in docs/areas. Use before any task that changes game rules, numbers, content or screens, when a request is vague (UI art concept, layout, enemy lineup, rewards), when docs/design and a spec or the code disagree, when starting work in an area, and when recording a user decision.
---

# 기획 흐름과 분야별 안내서

## 기획 문서 흐름 (docs/design → 기획서 → 구현)
- `docs/design/` = 사람이 쓰는 기획 방향. **모든 기획 결정에서 1순위** (기획서·docs/areas 결정·이전 대화보다 앞선다). 에이전트는 읽기만 한다(훅이 쓰기를 막는다). 사용자가 파일을 넣고 커밋을 맡기면 내용 그대로 커밋만 한다
- **기획 에이전트**가 `docs/design/` 을 읽고 다른 `docs/` 하위 폴더에 기획서를 쓴다. 폴더는 최상위 `design.md` 표를 따르고, 새 폴더가 생기면 그 표에 한 줄 더한다(`design.md` 엔 경로·설명만)
  - 기능 하나 = md 하나. 구현 에이전트가 기획서만 보고 구현할 수 있게 규칙·수치·목록·배치·화면 구성을 빠짐없이 쓴다
  - 머리에 "출처: docs/design/<파일> (커밋 <해시>)". `docs/design/` 이 바뀌면 따르는 기획서를 다시 맞추고 출처 커밋을 고친다
  - `docs/design/` 에 없는 세부는 기획 에이전트가 정한다. 방향 자체가 갈리면 사용자에게 묻는다
- **구현 에이전트**는 기획서를 보고 구현한다. 구현 요구사항은 기획서에만 있다. 구현 기록(코드 구조, 검증 방법)은 `docs/dev/` 에 둔다
- 작업을 시작할 때 관련 `docs/design/` 파일과 그 분야 기획서를 먼저 읽는다
- 게임 기능(규칙·수치·화면)을 바꾸는 요청은 기획서를 거친다. 도구·작업 방식 요청은 기획서 없이 구현한다
- 충돌:
  - **기획서 ≠ docs/design → 기획서를 고친다.** 사람은 기획서를 직접 고치지 않고 `docs/design/` 을 고쳐 방향을 잡는다
  - **구현 ≠ 기획서 → 구현(코드·데이터)을 고친다**
  - **docs/design 파일끼리 충돌** → 고르지 않고 이슈 "기획 충돌: <파일 A> ↔ <파일 B>" (충돌 문장 인용·영향받는 기획서와 구현·정할 것). 이슈 도구가 안 되면 사용자에게 알린다. 정해질 때까지 그 부분만 멈춘다
- 기획 구체화:
  - 구현 중 기획서에 없는 것이 필요하면 구현 에이전트가 정하지 않고 기획 에이전트에게 보충을 맡긴다
  - 지시가 애매하면(UI 아트 컨셉, 화면·지도·배경 배치, 적 구성, 보상 등) 구현 전에 기획 에이전트(opus)에게 사용자 원문과 관련 `docs/design` 을 넘겨 기획서를 받는다. 아트 톤·화면 전체 개편처럼 방향이 갈리는 안은 다른 서브에이전트에게 방향성 검토를 받는다
  - 받은 방향은 사용자에게 짧게 알리고 구현한다. 새 원칙은 그 분야 안내서 결정 표에 남긴다. 지시가 분명하면 서브에이전트를 부르지 않는다
- docs 에는 게임 뼈대 문서(기획 방향·기획서·안내서·구현 기록·작업 규칙)만 둔다. 요청·체크리스트·검토 스크린샷처럼 작업마다 바뀌는 것은 이슈에 둔다

## 분야별 안내서 (docs/areas)
- 분야마다 하나(`docs/areas/<분야>.md`): 경로, 지금 유효한 사용자 결정, 맡길 때 모델·함정. 남은 일은 안내서가 아니라 주제 이슈 흐름에 둔다. 목록·형식은 docs/areas/README.md. 없으면 그 분야 첫 작업에서 만든다
- 일을 시작하면 관련 안내서(1~2개)부터 읽는다. 서브에이전트 프롬프트 첫 줄에 "먼저 `docs/areas/<분야>.md` 를 읽는다". 모델은 README 표로 메인이 고른다 (사용자가 정하면 그것)
- 아트가 걸린 일은 `docs/areas/art-assets.md` 표로 에셋을 고르고, 새 아트를 들이면 한 줄 더한다
- 일이 끝나면 안내서의 경로·결정·함정을 고친다 (코드 커밋에 같이)
- 사용자 결정은 그 안내서 "결정" 표에 `시험 중` 으로 넣는다(지금 유효한 것만, 이력은 git·이슈). 사용자가 확정하거나 2주 넘게 그대로면 `확정 후보` 로 바꾸고 보고 때 "docs/design 으로 옮길 후보" 로 알린다. 옮기는 건 사용자, 옮겨지면 표에서 지운다
- 작업 방식 결정(이슈·브랜치·주석 규칙 등)은 안내서가 아니라 CLAUDE.md·`.claude/skills`·`.claude/hooks` 에 둔다
