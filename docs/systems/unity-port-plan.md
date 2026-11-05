# Unity 포팅 계획

`docs/Prototype.html` 의 육각 턴제 전술 게임을 Unity 6 (6000.3.27f2, URP) 로 옮긴다. 플레이어가 보는 규칙·수치·스테이지·전투 기록 문장은 HTML 과 한 글자도 다르지 않아야 한다.
지키는 규칙은 하나다. 같은 편성과 같은 명령을 넣으면 C# 전투 상태와 기록이 HTML 과 똑같다. 이것을 node 로 HTML 을 그대로 돌린 골든 기록과 비교해서 증명한다.
아트는 `Assets/Synty` 와 `UPackage/` 의 Synty 에셋으로 채우고, 에디터 작업은 전부 Unity CLI (`unity`) 로 한다.
PR 은 순서대로 P1 골격, P2 데이터, P3 육각 이동, P4 전투 규칙과 적 AI, P5 캠페인, P6 보드 화면, P7 유닛 화면, P8 입력과 배치, P9 메뉴·캠페인·전투 UI 다.
작업 이슈는 https://github.com/OruteZ/Project-Vampire-Turn-Strategy/issues/1 이다.

## How to read this

One box is one unit of work. Every box names the evidence that checks it. A nested box is a sub-step of the box above it. Check a box only when its evidence exists, a file, a log line, a screenshot, a test run, or a SHA. The body is a how-to. The appendices explain and record.

상자 하나가 일 하나다. 상자마다 그것을 확인하는 증거(파일, 로그 줄, 스크린샷, 테스트 결과, SHA)를 적는다. 증거가 있을 때만 상자를 체크한다.

The program runs `pstack/skills/poteto-mode/playbooks/autopilot-stack.md`. 작업이 앞 PR 위에 차례로 쌓이고 사용자가 리뷰한 뒤 직접 머지하므로 Autopilot-stack 을 쓴다. 모든 PR 은 `dev` 를 바닥으로 하는 한 줄 스택이고, 머지는 사용자가 아래에서부터 한다. 에이전트는 어떤 PR 도 머지하지 않는다.

Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked.

## Program checklist

### Arm the program

- [ ] 이 계획과 진행 방식을 사용자에게 알리고 멈춘다. 사용자가 분명히 시작하라고 할 때만 실행한다.
- [ ] 시작할 때와 매 점검 때 아래를 다시 읽는다. pstack 파일은 설치된 플러그인 폴더(`<pstack>` = `C:/Users/User/.claude/plugins/cache/cursor-plugins/pstack/<버전>`)에서 읽는다.
  - [ ] `<pstack>/skills/poteto-mode/playbooks/autopilot-stack.md` 와 `<pstack>/skills/poteto-mode/playbooks/autopilot-full.md`
  - [ ] `<pstack>/skills/swarm/SKILL.md`
  - [ ] 이 저장소의 Unity CLI 검증 스킬 `.claude/skills/verify-unity/SKILL.md` 와 `features/README.md`. 모든 live lane 은 이 스킬의 `vu.sh` 로 돈다.
  - [ ] `<pstack>/skills/poteto-mode/playbooks/opening-a-pr.md`
  - [ ] `<pstack>/skills/how/SKILL.md`, `<pstack>/skills/interrogate/SKILL.md`, `<pstack>/skills/show-me-your-work/SKILL.md`
  - [ ] `git show origin/main:CLAUDE.md` 와 `git show origin/dev:CLAUDE.md`. 둘이 다르면 dev 를 따른다.
- [ ] 사용자가 시작하라고 하면 아래 점검 문구로 `/loop 1h` 를 건다. 주기를 기억에 맡기지 않는다.
- [ ] 점검 문구는 그대로 쓴다. "Re-read the execution playbook from trunk. Audit the operation against it and fix drift in this tick. Probe every active lane and judge progress by side effects only. Stand down a stuck lane and dispatch its replacement now. Then post a short status message to the operator in chat only when the audit found a tracked change that no earlier status message reported, such as a PR opened, a code-ready head, a round launched or closed, a verdict, a merge, a stuck agent and the action taken, a blocker added or cleared, or a decision only the operator can make. Name every such change and nothing else. Do not repeat a table, the merged list, or an unchanged blocker. If the audit found none, end the turn with no reply text. Either way, log this tick's row in your decision trail. The row names the items reported, or none."
- [ ] 사용자가 멈추라고 하면 모든 담당 에이전트에게 바로 쓰기 금지를 보낸다.

### Spawn owners

- [ ] PR 하나에 담당 에이전트 하나를 붙인다. `subagent_type` 은 `pstack:poteto-agent` 다. 모델은 `feature, refactoring` 줄(`opus low`)을 쓰고, P4 는 `hardest tasks` 줄(`opus xhigh`)을 쓴다.
- [ ] 담당은 각자 worktree `C:/Unity Projects/Project Vampire Spawn-<pr-id>` 에서 일한다 (CLAUDE.md 작업 트리 규칙). worktree 를 만든 뒤 `Assets/Synty` 를 메인 트리 폴더로 잇는 junction 을 건다 (`cmd /c mklink /J`). Synty 는 git 에 없기 때문이다.
- [ ] 아래 순서를 지킨다. 각 PR 은 앞 PR 의 브랜치에서 갈라진다.
  - [ ] P1 이 처음이고 `dev` 에서 갈라진다. 같은 단계에서 동시에 시작할 수 있는 PR 은 없다.
  - [ ] P2 는 P1 다음, P3 는 P2 다음, P4 는 P3 다음, P5 는 P4 다음이다.
  - [ ] P6 은 P5 다음, P7 은 P6 다음, P8 은 P7 다음, P9 는 P8 다음이다.
- [ ] 파일 경계를 지킨다. P1~P5 는 `Assets/_Project/Scripts/Core/**`, `Assets/_Project/Tests/**`, `tools/golden/**`, `docs/**` 만 고친다. P6~P9 는 `Assets/_Project/Scripts/Game/**`, `Assets/_Project/Art/**`, `Assets/_Project/UI/**`, `Assets/_Project/Scenes/**`, `Assets/_Project/Tests/**`, `docs/**`, `ProjectSettings/**` 만 고친다. `docs/design/**` 은 아무도 쓰지 않는다.
- [ ] 리뷰 관문을 지킨다. P6, P7, P8, P9 는 화면과 조작을 바꾸므로 사용자가 채팅에서 스크린샷과 영상을 본 뒤에 머지한다.

### PR mechanics, for every PR

- [ ] PR 도구는 `gh` 로 정한다. `command -v origin` 이 없으므로 `gh` 를 쓴다고 기록한다. `gt` 는 쓰지 않는다.
- [ ] PR 은 draft 가 아닌 ready 로 연다 (**Opening a PR**). `gh pr create --base <부모 브랜치>` 를 쓰고, P1 의 부모는 `dev` 다. 본문 첫 줄에 `#1` 을 적는다 (CLAUDE.md 이슈 규칙).
- [ ] PR 쪽으로 push 하기 전에 한 번 컴파일과 테스트를 돌린다. `unity command --project-path "<worktree>" script recompile` 를 하고 `console status` 의 compile-failure 가 false 인지 본다. 그다음 `unity command --project-path "<worktree>" test run --mode editor` 를 돌린다. hook 을 켠 채로 push 한다.
- [ ] 커밋 전에 `/deslop` 을, 리뷰 전에 `/no-comments` 를 돌린다. Claude Code 에 `/deslop` 이 없으면 CLAUDE.md 주석 규칙으로 직접 점검하고 그렇게 적는다.
- [ ] Bugbot 과 보안 리뷰 댓글은 `<pstack>/skills/poteto-mode/references/bugbot-triage.md` 로 분류한다. 이 저장소에 Bugbot 이 없으면 건너뛰고 그렇게 적는다.
- [ ] code-ready 보고와 babysit 전에 부모 브랜치 최신으로 rebase 한다. 수정 라운드에서는 그 기준을 유지한다. 머지 준비, `git merge-tree` 충돌, 부모 쪽 변경 때문에 생긴 실패 때만 다시 rebase 한다.

### Verdict and merge, for every PR

- [ ] code-ready head 에서 전체 swarm 을 한 번 돌린다. 새 동작, 재설계, 큰 diff 가 생길 때만 다시 전체 swarm 을 돌린다. rebase, 충돌 해결, 작은 수정은 새 head 에서 검증자 하나만 돌린다. swarm 은 `<pstack>/skills/swarm/SKILL.md` 대로 한다. gates lane 하나, PR 의 **Verify, live** 블록에 있는 live lane 열 개, **Verify, perf** 블록의 perf lane 하나, diff 와 증거를 읽고 PR 본문을 믿지 않는 audit lane 둘 이상이다. 판정 전에 루트가 merge-ready 보고의 증거를 직접 확인한다.
- [ ] 모든 lane 이 `PASS` 일 때만 깨끗하다. lane 이 메모로 남긴 결함도 담당에게 돌려보낸다. 새 head 는 새로 판정한다. `playbooks/shipping.md` 의 patch-id 규칙으로 유효한 결과만 예외다.
- [ ] 깨끗한 PR 은 루트가 스택 순서대로 잇는다 (`gh pr edit <pr> --base <부모 브랜치>`). 판정 뒤 rebase 해도 patch-id 가 같으면 판정이 유지된다 (`playbooks/shipping.md`). 머지는 사용자가 아래에서부터 한다.

### Boot recipe, for every live lane

이 저장소에는 클라우드 VM 이 없다. Unity 에디터 하나를 Unity CLI 로 조종하므로, 각 lane 은 검증 전용 worktree 의 에디터에서 차례로 돈다. 입력은 Unity CLI 명령으로만 넣는다.

- [ ] 검증 트리는 한 번만 만든다. `git worktree add "C:/Unity Projects/Project Vampire Spawn-verify"` 를 하고 `Assets/Synty` junction 을 걸고 `unity open "C:/Unity Projects/Project Vampire Spawn-verify"` 를 한다. 첫 임포트는 오래 걸린다.
- [ ] `git -C "C:/Unity Projects/Project Vampire Spawn-verify" fetch origin <head-branch> && git -C "C:/Unity Projects/Project Vampire Spawn-verify" checkout <head SHA>`.
- [ ] `unity status --json` 에서 그 경로의 state 가 `ready` 가 될 때까지 기다린다. `unity command --project-path "<verify>" script recompile` 를 하고 `console status` 의 compile-failure 가 false 인지 본다.
- [ ] 입력은 `editor.playmode start/stop`, `runtime simulate_pointer`, `runtime simulate_key` 로만 넣는다. 읽기 전용 진단은 `console get`, `scene get_hierarchy`, `diagnostics get_performance`, 그리고 상태를 바꾸지 않는 `script eval` 이다.
- [ ] 스크린샷은 `capture game_view --output-path` 또는 `capture editor_window --output-path` 로 `%TEMP%/swarm-<pr-id>/worker-<n>/<slug>.png` 에 저장하고, 보고에 그 경로를 붙인다.

## 프로젝트 골격과 에셋 임포트를 만든다 (P1)

**Depends on.** None.

**Files.**

- [ ] Create `Assets/_Project/Scripts/Core/VampireSpawn.Core.asmdef`. `noEngineReferences` 를 true 로 해서 규칙 코드가 Unity 없이 돈다.
- [ ] Create `Assets/_Project/Scripts/Game/VampireSpawn.Game.asmdef`. Core 를 참조한다.
- [ ] Create `Assets/_Project/Tests/EditMode/VampireSpawn.Tests.EditMode.asmdef` 와 `SmokeTests.cs`.
- [ ] Create `Assets/_Project/Scenes/Main.unity`. 카메라와 Directional Light 만 둔다.
- [ ] Edit `ProjectSettings/EditorBuildSettings.asset`. `Main.unity` 를 0번으로 둔다 (CLAUDE.md 시작 씬 규칙).
- [ ] Edit `.gitignore`. 임포트한 Synty 패키지가 `Assets/Synty` 밖에 생기면 그 폴더를 더한다.
- [ ] Edit `docs/areas/art-assets.md`. 임포트한 패키지의 실제 경로로 표를 고친다.
- [ ] Create `docs/areas/battle.md`. `docs/areas/README.md` 형식으로 만든다.

**Build.**

- [ ] `UPackage/` 의 아직 안 들인 패키지 일곱 개(ANIMATION 셋, INTERFACE 둘, SIDEKICK 둘)를 `unity command script eval --code "UnityEditor.AssetDatabase.ImportPackage(@\"<경로>\", false)"` 로 하나씩 들인다. 패키지마다 `console get --level error` 가 비어 있는지 본다.
- [ ] `scene create`, `scene add_to_build` 로 `Main.unity` 를 만들고 0번에 등록한다.

**You see.**

- [ ] `unity command scene list_open` 에 `Assets/_Project/Scenes/Main.unity` 가 나오고 `test run --mode editor` 결과에 `SmokeTests` 1개 통과가 나온다.

**Verify, unit.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked.

- [ ] `SmokeTests.CoreAssemblyHasNoUnityReference` 가 Core 어셈블리의 참조 목록에 `UnityEngine` 이 없음을 확인한다. Run `unity command test run --mode editor --filter SmokeTests`.

**Verify, live.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked. Ten lanes on `opus low` at the PR head, per the boot recipe.

- [ ] Lane 1. Regression lane against trunk. trunk(`dev`)와 head 에서 각각 열고 컴파일한다. trunk 에는 `_Project` 가 없다는 것을 기록하고, head 의 컴파일 성공과 0번 씬을 확인한다. Save `p1-compile.png`. Pass when 두 트리 모두 `console status` compile-failure 가 false 이고 head 의 Build Settings 0번이 `Main.unity` 일 때.
- [ ] Lane 2. 재생 모드에 들어갔다 나온다. Save `p1-play.png`. Pass when `console get --level error` 가 비어 있을 때.
- [ ] Lane 3. `asset find --name SM_Chr_DarkLord_Male_01` 로 기존 Synty 가 junction 으로 보이는지 본다. Save `p1-darklord.png`. Pass when 결과가 1개 이상일 때.
- [ ] Lane 4. SIDEKICK Knights 프리팹 하나를 `Main` 에 임시로 놓고 game view 를 찍은 뒤 씬을 저장하지 않고 닫는다. Save `p1-sidekick-knight.png`. Pass when 모델이 분홍색(셰이더 깨짐)이 아닐 때.
- [ ] Lane 5. SIDEKICK Villagers 프리팹으로 Lane 4 와 같이 한다. Save `p1-sidekick-villager.png`. Pass when 분홍색이 아닐 때.
- [ ] Lane 6. ANIMATION Sword Combat 클립 하나를 `asset find --type AnimationClip` 으로 찾는다. Save `p1-anim.png`. Pass when 검·활·이동 패키지마다 클립이 1개 이상일 때.
- [ ] Lane 7. INTERFACE HUD 스프라이트를 `asset find --type Sprite` 로 찾는다. Save `p1-ui.png`. Pass when HUD 와 Menus 패키지마다 스프라이트가 1개 이상일 때.
- [ ] Lane 8. `git status --short` 를 본다. Save `p1-git.png`. Pass when Synty 와 UPackage 파일이 하나도 추적되지 않을 때.
- [ ] Lane 9. 검증 트리를 지우고 다시 만든 뒤 Lane 1 을 반복한다. Save `p1-fresh.png`. Pass when 새 클론에서 junction 만으로 컴파일이 성공할 때.
- [ ] Lane 10. `script eval` 로 `QualitySettings.renderPipeline` 이 URP 에셋인지 본다. Save `p1-urp.png`. Pass when 값이 `PC_RPAsset` 일 때.

**Verify, perf.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked.

- [ ] Metric. `unity recompile` 이 끝날 때까지 걸린 초.
- [ ] Probe. trunk 와 head 에서 번갈아 세 번씩 `unity recompile --project-path "<verify>"` 시간을 잰다.
- [ ] Baseline. trunk 값을 먼저 적는다.
- [ ] Rule. head 중앙값이 trunk 중앙값보다 10초 넘게 길면 실패다.

**Review gate.** None. P1 is not review-gated.

**Merge.**

- [ ] 정확한 head SHA 에서 루트의 깨끗한 판정.
- [ ] Bugbot 분류 끝.
- [ ] 판정 뒤 부모 최신으로 rebase, patch-id 그대로.
- [ ] 루트가 스택에 잇고, 사용자가 아래에서부터 머지한다.

## 골든 기록 도구와 게임 데이터를 옮긴다 (P2)

**Depends on.** P1.

**Files.**

- [ ] Create `tools/golden/prototype-golden.mjs`. `docs/Prototype.html` 의 `<script>` 를 node `vm` 에서 DOM 대역과 함께 돌린다 (Appendix A 에서 동작 확인함).
- [ ] Create `tools/golden/out/data.json`. HTML 의 `TYPES`, `UNLOCK_AT`, `UPGRADES`, `SKEL`, `STAGES`, 상수(`DRAIN_HEAL`, `MOON_MAX`, `EAT_RANGE`, `MOON_WAKE`)를 그대로 뽑는다.
- [ ] Create `Assets/_Project/Scripts/Core/GameData.cs`.
- [ ] Create `Assets/_Project/Tests/EditMode/GameDataParityTests.cs`.
- [ ] Create `docs/systems/unity-port.md`. 맨 위에 `출처 docs/Prototype.html (커밋 <해시>)` 를 적는다.

**Build.**

- [ ] `GameData` 에 병종 표(`UnitKind` enum 을 키로 하는 읽기 전용 표), 강화 표, 스테이지 표, 상수를 둔다. 스테이지 지도는 문자열 줄 그대로 두고 `Terrain` enum(`Plain`, `Rock`, `Torch`, `Water`, `Sun`, `Deploy`)으로 읽는다.
- [ ] `prototype-golden.mjs data` 가 `data.json` 을 쓴다. C# 표는 손으로 옮기되 테스트가 JSON 과 한 칸씩 비교한다.

**You see.**

- [ ] `node tools/golden/prototype-golden.mjs data` 가 `tools/golden/out/data.json` 을 쓰고, `GameDataParityTests` 가 모두 통과한다.

**Verify, unit.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked.

- [ ] `GameDataParityTests` 가 병종 6개, 강화 12개, 스테이지 11개의 모든 필드와 지도 줄, 적 배치, 예시 편성을 `data.json` 과 비교한다. Run `unity command test run --mode editor --filter GameDataParity`.

**Verify, live.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked. Ten lanes on `opus low` at the PR head, per the boot recipe.

- [ ] Lane 1. Regression lane against trunk. trunk 에는 `GameData` 가 없다는 것을 기록한다. head 에서 `script eval` 로 `GameData.Stages.Count` 를 읽는다. Save `p2-count.png`. Pass when 11 일 때.
- [ ] Lane 2. `data.json` 의 `TYPES.knight` 한 줄을 일부러 바꾸고 테스트를 돌린 뒤 되돌린다. Save `p2-red.png`. Pass when 바꿨을 때 실패하고 되돌린 뒤 통과할 때.
- [ ] Lane 3. `script eval` 로 데모 스테이지의 지도 8줄을 출력한다. Save `p2-demo-map.png`. Pass when HTML 의 `map` 배열과 글자가 같을 때.
- [ ] Lane 4. 10장 보스 수치를 읽는다. Save `p2-s10-boss.png`. Pass when 체력 42, 공격 7, 방어 1, 이동 4, 회복 6 일 때.
- [ ] Lane 5. `UNLOCK_AT` 를 읽는다. Save `p2-unlock.png`. Pass when 농민 창병 1, 농민 궁수 1, 검사 3, 궁수 5, 기사 7, 사냥꾼 9 일 때.
- [ ] Lane 6. 강화 표에서 스폰이 물려받는 것만 고른다. Save `p2-inherit.png`. Pass when `pspear_hp`, `parcher_mov`, `sword_def`, `archer_atk`, `knight_def`, `hunter_mov` 여섯 개일 때.
- [ ] Lane 7. 각 장의 추가 목표 문장을 출력한다. Save `p2-bonus.png`. Pass when HTML `bonusText` 결과와 같을 때.
- [ ] Lane 8. 2장 지도의 `~` 칸 수를 센다. Save `p2-water.png`. Pass when HTML 에서 센 값과 같을 때.
- [ ] Lane 9. 새 검증 트리에서 `node tools/golden/prototype-golden.mjs data` 를 두 번 돌려 해시를 비교한다. Save `p2-deterministic.png`. Pass when 두 해시가 같을 때.
- [ ] Lane 10. `console get --level warn` 을 본다. Save `p2-console.png`. Pass when 경고가 0개일 때.

**Verify, perf.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked.

- [ ] Metric. EditMode 테스트 전체의 걸린 시간. trunk 에는 이 테스트가 없으므로 P2 가 더한 테스트의 시간에 절대 예산을 둔다.
- [ ] Probe. trunk 와 head 에서 번갈아 세 번씩 `test run --mode editor` 를 돌려 결과 보고의 duration 을 읽는다.
- [ ] Baseline. trunk 값(Smoke 만)을 먼저 적는다.
- [ ] Rule. `GameDataParity` 묶음이 2초를 넘으면 실패다.

**Review gate.** None. P2 is not review-gated.

**Merge.**

- [ ] 정확한 head SHA 에서 루트의 깨끗한 판정.
- [ ] Bugbot 분류 끝.
- [ ] 판정 뒤 부모 최신으로 rebase, patch-id 그대로.
- [ ] 루트가 스택에 잇고, 사용자가 아래에서부터 머지한다.

## 육각 좌표와 이동을 옮긴다 (P3)

**Depends on.** P2.

**Files.**

- [ ] Create `Assets/_Project/Scripts/Core/Hex.cs`.
- [ ] Create `Assets/_Project/Scripts/Core/Board.cs`.
- [ ] Edit `tools/golden/prototype-golden.mjs`. `hex` 명령을 더한다.
- [ ] Create `tools/golden/out/hex.json`.
- [ ] Create `Assets/_Project/Tests/EditMode/HexParityTests.cs`.

**Build.**

- [ ] `Hex` 는 (열, 행) 값 타입이다. flat-top, odd-q 이웃 표(`NB`), 축 좌표 변환, 거리(`hd`)를 HTML 과 같은 순서로 둔다. 이웃 순서가 BFS 결과를 바꾸므로 순서를 그대로 지킨다.
- [ ] `Board` 는 스테이지 지도에서 만들고 `Walkable`, `Lit`, `Reachable`, `PathTo`, `DistField` 를 가진다. 막힘 판정에는 유닛과 시신 위치를 인자로 받는다.
- [ ] `hex` 명령은 11개 스테이지의 모든 칸에 대해 이웃, `lit`, 그리고 예시 편성 유닛마다 `reachable` 과 `pathTo` 를 기록한다.

**You see.**

- [ ] `HexParityTests` 가 11개 스테이지에서 `hex.json` 과 모두 같다.

**Verify, unit.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked.

- [ ] `HexParityTests` 가 칸마다 이웃 목록(순서 포함), `Lit`, 유닛마다 도달 칸과 거리, 경로를 비교한다. Run `unity command test run --mode editor --filter HexParity`.

**Verify, live.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked. Ten lanes on `opus low` at the PR head, per the boot recipe.

- [ ] Lane 1. Regression lane against trunk. trunk 에는 `Board` 가 없다는 것을 기록한다. head 에서 `script eval` 로 데모의 기사(5,7) 도달 칸 수를 읽는다. Save `p3-knight-reach.png`. Pass when `hex.json` 의 값과 같을 때.
- [ ] Lane 2. `Hex` 이웃 순서를 하나 바꾸고 테스트를 돌린 뒤 되돌린다. Save `p3-red.png`. Pass when 바꿨을 때 실패할 때.
- [ ] Lane 3. 1장 화톳불(4,3) 주변의 `Lit` 칸을 출력한다. Save `p3-lit.png`. Pass when 화톳불과 이웃 여섯 칸 중 지도 안의 칸만 나올 때.
- [ ] Lane 4. 3장 `S` 칸의 `Lit` 를 읽는다. Save `p3-sun.png`. Pass when true 일 때.
- [ ] Lane 5. 2장에서 물 칸 건너 경로를 묻는다. Save `p3-water.png`. Pass when 경로가 `d` 다리 칸으로만 지날 때.
- [ ] Lane 6. 바위 칸으로 가는 `PathTo` 를 묻는다. Save `p3-rock.png`. Pass when HTML 처럼 목표 한 칸만 돌려줄 때.
- [ ] Lane 7. 적 유닛이 길을 막을 때 도달 칸을 비교한다. Save `p3-blocked.png`. Pass when `hex.json` 의 막힘 사례와 같을 때.
- [ ] Lane 8. 시신이 길을 막을 때 도달 칸을 비교한다. Save `p3-corpse.png`. Pass when `hex.json` 의 시신 사례와 같을 때.
- [ ] Lane 9. 10장의 `DistField` 를 아군 주변 목표로 만든다. Save `p3-dist.png`. Pass when 모든 칸 거리가 `hex.json` 과 같을 때.
- [ ] Lane 10. `console get --level warn` 을 본다. Save `p3-console.png`. Pass when 경고가 0개일 때.

**Verify, perf.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked.

- [ ] Metric. 10장(11x8)에서 이동 4 유닛의 `Reachable` 1만 번 호출 시간. trunk 에는 없으므로 절대 예산을 둔다.
- [ ] Probe. `script eval` 로 `Stopwatch` 를 두르고 세 번 잰다.
- [ ] Baseline. trunk 는 기능 없음을 기록한다.
- [ ] Rule. 중앙값이 200ms 를 넘으면 실패다. 적 페이즈 하나가 이 호출을 수십 번 하므로 그 예산이 넉넉하다.

**Review gate.** None. P3 is not review-gated.

**Merge.**

- [ ] 정확한 head SHA 에서 루트의 깨끗한 판정.
- [ ] Bugbot 분류 끝.
- [ ] 판정 뒤 부모 최신으로 rebase, patch-id 그대로.
- [ ] 루트가 스택에 잇고, 사용자가 아래에서부터 머지한다.

## 전투 규칙과 적 AI 를 옮긴다 (P4)

**Depends on.** P3.

**Files.**

- [ ] Create `Assets/_Project/Scripts/Core/Battle.cs`.
- [ ] Create `Assets/_Project/Scripts/Core/BattleEvent.cs`.
- [ ] Create `Assets/_Project/Scripts/Core/EnemyAI.cs`.
- [ ] Create `Assets/_Project/Scripts/Core/Korean.cs`. `josa` 를 옮긴다.
- [ ] Edit `tools/golden/prototype-golden.mjs`. `battle` 명령과 정책 플레이어를 더한다.
- [ ] Create `tools/golden/out/battles/*.json`.
- [ ] Create `Assets/_Project/Tests/EditMode/BattleParityTests.cs`.

**Build.**

- [ ] `Battle` 은 `Phase` enum(`Deploy`, `Player`, `Enemy`, `Over`)을 가진 상태 기계다. 명령은 `Deploy`, `Recall`, `Swap`, `Start`, `Move`, `Attack`, `Execute`, `Wait`, `EndTurn` 이다. 단계에 맞지 않는 명령은 거절한다.
- [ ] HTML 은 규칙 중간에 화면을 그리고 기다린다. C# 에서는 규칙이 화면을 모른다. 명령마다 `BattleEvent` 목록(이동 경로, 피해, 사망, 시신, 기상, 관 열림, 보름달, 흡혈, 처치, 기록 문장)을 돌려주고, 화면은 그 목록을 차례로 재생한다 (P7). `EndTurn` 은 적 페이즈 전체를 한 번에 계산한다.
- [ ] 아이디 발급(`uid`), 적 행동 순서(스켈레톤, 스폰, 뱀파이어), 창벽, 베어 넘기기, 돌격, 매복, 대뱀파이어, 보름달 표적 고정, 관 열림을 HTML 과 같은 순서로 옮긴다.
- [ ] `battle` 명령은 11개 스테이지마다 정해진 정책 셋(예시 편성 그대로 턴 종료만, 가장 가까운 적 공격, 보름달 표적을 빛으로 빼기와 아군 처치 섞기)으로 HTML 을 끝까지 돌린다. 명령 하나마다 전체 상태(유닛, 시신, 달, 표적, 턴, 단계, 통계, 기록 문장)를 적는다. 모두 33개 기록이다.

**You see.**

- [ ] `BattleParityTests` 가 33개 기록을 같은 명령으로 재생하고, 명령마다 상태와 기록 문장이 모두 같다.

**Verify, unit.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked.

- [ ] `BattleParityTests` 가 기록마다 명령을 하나씩 넣고 상태 전체를 비교한다. 처음 다른 명령의 번호와 필드를 실패 메시지에 낸다. Run `unity command test run --mode editor --filter BattleParity`.
- [ ] `BattleRulesTests` 가 피해 최소 1, 빛 속 사망은 시신 없음, 스켈레톤에게 죽으면 시신 없음, 단계 밖 명령 거절을 확인한다. 기대값은 HTML 규칙 요약 문장에서 가져온다. Run `unity command test run --mode editor --filter BattleRules`.

**Verify, live.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked. Ten lanes on `opus low` at the PR head, per the boot recipe.

- [ ] Lane 1. Regression lane against trunk. trunk 에는 `Battle` 이 없다는 것을 기록한다. head 에서 `script eval` 로 데모를 예시 편성, 턴 종료만으로 끝까지 돌린다. Save `p4-demo-end.png`. Pass when 결과가 Appendix A 의 node 결과(7턴 패배, 전사 5, 스폰 2, 흡혈 1)와 같을 때.
- [ ] Lane 2. 적 행동 순서를 바꾸고 테스트를 돌린 뒤 되돌린다. Save `p4-red.png`. Pass when 바꿨을 때 실패할 때.
- [ ] Lane 3. 뱀파이어에게 죽은 아군이 다음 적 페이즈 시작에 스폰으로 일어나는 기록을 찾는다. Save `p4-rise.png`. Pass when 기록 문장이 `뱀파이어 스폰으로 일어났다` 로 끝나고 그 턴에 행동하지 않을 때.
- [ ] Lane 4. 달이 3이 되는 적 페이즈를 본다. Save `p4-wake.png`. Pass when `관이 열렸다` 기록이 나오고 그 턴에 뱀파이어가 움직이지 않을 때.
- [ ] Lane 5. 보름달 표적을 빛 속으로 뺀 기록을 본다. Save `p4-eat-fail.png`. Pass when `흡혈 실패` 기록과 달 0 이 나올 때.
- [ ] Lane 6. 보름달 표적을 아군 처치로 보낸 기록을 본다. Save `p4-execute.png`. Pass when 처치 통계가 1 늘고 시신이 생기지 않을 때.
- [ ] Lane 7. 캠페인 강화를 모두 산 상태로 9장 기록을 재생한다. Save `p4-abilities.png`. Pass when 창벽, 매복, 돌격, 베어 넘기기, 대뱀파이어 태그가 모두 기록에 한 번 이상 나올 때.
- [ ] Lane 8. 받침 없는 조사 처리를 본다. `script eval` 로 `Korean.Josa` 에 `기사`, `검사 2`, `스폰(궁수)` 를 넣는다. Save `p4-josa.png`. Pass when HTML `josa` 결과와 같을 때.
- [ ] Lane 9. 단계 밖 명령(배치 단계의 `Attack`)을 넣는다. Save `p4-reject.png`. Pass when 상태가 바뀌지 않고 거절이 돌아올 때.
- [ ] Lane 10. 33개 기록 전부를 두 번 재생해 상태 해시를 비교한다. Save `p4-deterministic.png`. Pass when 두 번의 해시가 같고 node 기록 해시와도 같을 때.

**Verify, perf.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked.

- [ ] Metric. 가장 무거운 적 페이즈(10장, 적 10기 이상)의 `EndTurn` 계산 시간. trunk 에는 없으므로 절대 예산을 둔다.
- [ ] Probe. `script eval` 로 기록을 그 턴 직전까지 재생하고 `EndTurn` 을 100번 복제 상태에서 잰다.
- [ ] Baseline. trunk 는 기능 없음을 기록한다.
- [ ] Rule. 중앙값이 5ms 를 넘으면 실패다. 화면 재생과 별개로 한 프레임 안에 끝나야 하기 때문이다.

**Review gate.** None. P4 is not review-gated.

**Merge.**

- [ ] 정확한 head SHA 에서 루트의 깨끗한 판정.
- [ ] Bugbot 분류 끝.
- [ ] 판정 뒤 부모 최신으로 rebase, patch-id 그대로.
- [ ] 루트가 스택에 잇고, 사용자가 아래에서부터 머지한다.

## 캠페인 진행과 저장을 옮긴다 (P5)

**Depends on.** P4.

**Files.**

- [ ] Create `Assets/_Project/Scripts/Core/Campaign.cs`.
- [ ] Create `Assets/_Project/Scripts/Game/CampaignStore.cs`. `PlayerPrefs` 키 `pvs-campaign-v2` 에 JSON 으로 쓴다.
- [ ] Edit `tools/golden/prototype-golden.mjs`. `campaign` 명령을 더한다.
- [ ] Create `tools/golden/out/campaign.json`.
- [ ] Create `Assets/_Project/Tests/EditMode/CampaignParityTests.cs`.

**Build.**

- [ ] `Campaign` 은 금화, 클리어, 추가 목표, 강화를 가진다. 장 열림, 강화 열림, 강화 구매, 강화 초기화 환불, 진행 초기화, 전투 결과 보상(첫 클리어, 재도전, 추가 목표 한 번만)을 둔다.
- [ ] 강화는 `Battle` 이 유닛을 만들 때 능력치와 기술로 들어간다. 데모 모드에서는 강화가 없다.
- [ ] `campaign` 명령은 1장부터 10장까지 이기고, 강화를 사고, 초기화하는 순서를 HTML 로 돌려 단계마다 저장 JSON 을 적는다.

**You see.**

- [ ] `CampaignParityTests` 가 단계마다 저장 JSON 이 HTML 과 같다. 에디터를 껐다 켜도 캠페인 금화가 남아 있다.

**Verify, unit.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked.

- [ ] `CampaignParityTests` 가 `campaign.json` 의 단계마다 상태를 비교한다. Run `unity command test run --mode editor --filter CampaignParity`.

**Verify, live.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked. Ten lanes on `opus low` at the PR head, per the boot recipe.

- [ ] Lane 1. Regression lane against trunk. trunk 에는 캠페인이 없다는 것을 기록한다. head 에서 1장 승리를 넣고 금화를 읽는다. Save `p5-first-clear.png`. Pass when 첫 클리어 20, 추가 목표 달성 시 30 일 때.
- [ ] Lane 2. 1장을 다시 이긴다. Save `p5-replay.png`. Pass when 금화가 늘지 않을 때.
- [ ] Lane 3. 금화가 모자랄 때 강화를 산다. Save `p5-poor.png`. Pass when 거절되고 금화가 그대로일 때.
- [ ] Lane 4. 해금된 장을 깨기 전 검사 강화를 산다. Save `p5-locked-up.png`. Pass when 거절될 때.
- [ ] Lane 5. 강화 셋을 사고 초기화한다. Save `p5-respec.png`. Pass when 쓴 금화가 전부 돌아올 때.
- [ ] Lane 6. 저장 뒤 `editor.playmode stop` 과 에디터 재시작을 하고 읽는다. Save `p5-persist.png`. Pass when 금화와 클리어가 같을 때.
- [ ] Lane 7. `PlayerPrefs` 의 값을 깨진 JSON 으로 바꾸고 읽는다. Save `p5-corrupt.png`. Pass when HTML 처럼 빈 캠페인(금화 0)으로 시작하고 오류가 없을 때.
- [ ] Lane 8. 강화 뒤 3장 전투를 만들어 검사 방어를 읽는다. Save `p5-upgrade-applied.png`. Pass when 사슬 갑옷을 산 경우 방어가 2 일 때.
- [ ] Lane 9. 데모 전투에서 같은 검사 방어를 읽는다. Save `p5-demo-no-upgrade.png`. Pass when 방어가 1 일 때.
- [ ] Lane 10. 10장까지 모두 이긴 뒤 `Campaign` 의 끝 상태를 읽는다. Save `p5-finished.png`. Pass when 모든 장이 클리어이고 HTML 의 끝 상태와 같을 때.

**Verify, perf.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked.

- [ ] Metric. 저장과 불러오기 한 번씩 걸린 시간. trunk 에는 없으므로 절대 예산을 둔다.
- [ ] Probe. `script eval` 로 저장·불러오기를 100번 돌려 중앙값을 잰다.
- [ ] Baseline. trunk 는 기능 없음을 기록한다.
- [ ] Rule. 한 번에 10ms 를 넘으면 실패다.

**Review gate.** None. P5 is not review-gated.

**Merge.**

- [ ] 정확한 head SHA 에서 루트의 깨끗한 판정.
- [ ] Bugbot 분류 끝.
- [ ] 판정 뒤 부모 최신으로 rebase, patch-id 그대로.
- [ ] 루트가 스택에 잇고, 사용자가 아래에서부터 머지한다.

## 육각 보드와 지형을 그린다 (P6)

**Depends on.** P5.

**Files.**

- [ ] Create `Assets/_Project/Scripts/Game/BoardView.cs`.
- [ ] Create `Assets/_Project/Scripts/Game/ArtCatalog.cs` 와 `Assets/_Project/Art/ArtCatalog.asset`.
- [ ] Create `Assets/_Project/Art/Materials/Hex_*.mat`.
- [ ] Edit `Assets/_Project/Scenes/Main.unity`. 보드 루트와 비스듬한 위 카메라를 둔다.
- [ ] Edit `docs/areas/art-assets.md`. 지형별 프리팹 표를 더한다.

**Build.**

- [ ] `BoardView` 는 `Board` 에서 육각 타일을 만든다. 육각 타일 에셋이 Synty 에 없으므로 육각 메시를 코드로 만들고 Synty 땅 재질을 입힌다.
- [ ] `ArtCatalog` 는 지형과 유닛 종류를 프리팹으로 잇는 표 하나다. 바위 `#` 은 `SM_Env_Rock_0*`, 화톳불 `T` 는 `SM_Prop_Brazier_01` 과 `FX_Fire_01` 과 점광원, 햇빛 `S` 는 스폿라이트, 물 `~` 은 물 재질, 빛 받는 칸은 밝은 재질이다.
- [ ] 개발용으로 `GameBootstrap` 이 시작 스테이지를 인자로 받아 보드만 띄운다. 이 시험용 진입점은 P9 에서 메뉴로 바꾸고 지운다 (CLAUDE.md 시험용 기능 규칙).

**You see.**

- [ ] 재생하면 데모 지도의 바위 셋, 화톳불 하나, 햇빛 하나, 배치 구역이 HTML 그림과 같은 칸에 보인다.

**Verify, unit.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked.

- [ ] `BoardViewTests` 가 스테이지마다 만든 타일 수가 지도 칸 수와 같고, 칸마다 지형 종류와 `ArtCatalog` 프리팹이 맞는지 확인한다. Run `unity command test run --mode editor --filter BoardView`.

**Verify, live.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked. Ten lanes on `opus low` at the PR head, per the boot recipe.

- [ ] Lane 1. Regression lane against trunk. trunk 는 빈 씬이라는 것을 기록한다. head 에서 데모로 재생하고 찍는다. Save `p6-demo.png`. Pass when 11x8 보드와 HTML 과 같은 자리의 바위·화톳불·햇빛이 보일 때.
- [ ] Lane 2. 1장을 찍는다. Save `p6-s1.png`. Pass when 9x7 보드와 화톳불 한 칸이 맞을 때.
- [ ] Lane 3. 2장을 찍는다. Save `p6-s2.png`. Pass when 물 줄과 다리 두 칸이 맞을 때.
- [ ] Lane 4. 3장을 찍는다. Save `p6-s3.png`. Pass when 햇빛 두 칸이 맞을 때.
- [ ] Lane 5. 5장을 찍는다. Save `p6-s5.png`. Pass when 화톳불 셋과 주변 빛 칸이 맞을 때.
- [ ] Lane 6. 9장을 찍는다. Save `p6-s9.png`. Pass when 해자와 여울 칸이 맞을 때.
- [ ] Lane 7. 10장을 찍는다. Save `p6-s10.png`. Pass when 두 겹 성벽과 화톳불이 맞을 때.
- [ ] Lane 8. 스테이지를 1장에서 2장으로 바꿔 다시 띄운다. Save `p6-rebuild.png`. Pass when 이전 타일이 남지 않을 때.
- [ ] Lane 9. `scene get_hierarchy` 로 보드 루트 아래 개수를 센다. Save `p6-hierarchy.png`. Pass when 타일 수가 칸 수와 같을 때.
- [ ] Lane 10. `console get --level warn` 을 본다. Save `p6-console.png`. Pass when 경고가 0개일 때.

**Verify, perf.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked.

- [ ] Metric. 보드를 만드는 시간과 재생 중 프레임 시간. trunk 에는 보드가 없으므로 빈 씬 프레임 시간을 기준으로 두고 보드에 절대 예산을 둔다.
- [ ] Probe. trunk 와 head 에서 번갈아 재생하고 `diagnostics get_performance` 를 세 번 읽는다. 보드 생성 시간은 `script eval` 의 `Stopwatch` 로 잰다.
- [ ] Baseline. trunk 빈 씬 프레임 시간을 먼저 적는다.
- [ ] Rule. 보드 생성이 200ms 를 넘거나, 1280x720 재생 중 프레임 시간이 16.7ms 를 넘으면 실패다.

**Review gate.** The operator reviews before merge.

- [ ] Lane 1~7 스크린샷을 `docs/review/p6-review-<stage>.png` 로 복사한다 (review screenshot).
- [ ] 보드를 천천히 도는 30~60초 영상을 Unity Recorder 로 찍어 `docs/review/p6-review.mp4` 로 둔다. Recorder 패키지가 없으면 이 PR 에서 `package add com.unity.recorder` 를 한다. (review video)
- [ ] 스크린샷과 영상을 채팅에 올린다. merge-ready 에서 멈추고 사용자의 결정을 기다린다 (operator).

**Merge.**

- [ ] 정확한 head SHA 에서 루트의 깨끗한 판정.
- [ ] Bugbot 분류 끝.
- [ ] 판정 뒤 부모 최신으로 rebase, patch-id 그대로.
- [ ] 루트가 스택에 잇고, 사용자가 아래에서부터 머지한다.

## 유닛 모델과 전투 연출을 붙인다 (P7)

**Depends on.** P6.

**Files.**

- [ ] Create `Assets/_Project/Scripts/Game/UnitView.cs`.
- [ ] Create `Assets/_Project/Scripts/Game/EventPlayer.cs`.
- [ ] Edit `Assets/_Project/Art/ArtCatalog.asset`. 유닛 종류별 프리팹과 애니메이터를 더한다.
- [ ] Create `Assets/_Project/Art/Materials/Spawn_Tint.mat`.
- [ ] Edit `docs/areas/art-assets.md`. 유닛별 프리팹 표를 더한다.

**Build.**

- [ ] `ArtCatalog` 유닛 표는 창병과 농민 궁수에 SIDEKICK Villagers, 검사·궁수·기사에 SIDEKICK Knights, 사냥꾼에 `SM_Chr_Hunter_Male_01`, 스켈레톤에 `SM_Chr_Skeleton_01`, 뱀파이어에 `SM_Chr_DarkLord_Male_01` 을 쓴다. 스폰은 원래 병종 모델에 `Spawn_Tint` 를 입힌다. 시신은 같은 모델을 눕힌다. 관은 Synty 에서 찾지 못했다 (Appendix C 의 위험 4).
- [ ] 이동, 공격, 피격, 사망 애니메이션은 ANIMATION 패키지의 클립을 쓴다. 체력 막대와 피해 숫자는 월드 공간 UI 다.
- [ ] `EventPlayer` 는 P4 의 `BattleEvent` 목록을 차례로 재생한다. 기다리는 시간은 HTML 의 `wait` 값(한 칸 120ms, 피격 380ms 등)을 그대로 쓴다.

**You see.**

- [ ] 데모 예시 편성으로 턴 종료를 누르면 스켈레톤이 칸을 따라 걷고 공격하며, 피해 숫자가 뜨고, 쓰러진 아군이 시신으로 남았다가 스폰으로 일어난다.

**Verify, unit.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked.

- [ ] `EventPlayerTests` (PlayMode) 가 데모 기록을 재생한 뒤 화면의 유닛 수, 위치, 체력 막대가 `Battle` 상태와 같은지 확인한다. Run `unity command test run --mode playmode --filter EventPlayer`.

**Verify, live.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked. Ten lanes on `opus low` at the PR head, per the boot recipe.

- [ ] Lane 1. Regression lane against trunk. trunk 에서 데모 보드를 찍어 유닛이 없음을 기록한다. head 에서 같은 장면을 찍는다. Save `p7-units.png`. Pass when 적 다섯과 예시 편성 다섯이 HTML 과 같은 칸에 서 있을 때.
- [ ] Lane 2. 병종 여섯을 한 줄로 세워 찍는다. Save `p7-lineup.png`. Pass when 여섯 모두 서로 구분되고 분홍색이 없을 때.
- [ ] Lane 3. 잠든 뱀파이어를 찍는다. Save `p7-asleep.png`. Pass when 관 상태가 깨어난 상태와 눈으로 구분될 때.
- [ ] Lane 4. 턴 종료 뒤 스켈레톤 이동을 0.1초 간격으로 다섯 장 찍는다. Save `p7-walk.png`. Pass when 칸 사이를 걷는 모습이 이어질 때.
- [ ] Lane 5. 피격 순간을 찍는다. Save `p7-hit.png`. Pass when 피해 숫자가 기록의 피해와 같을 때.
- [ ] Lane 6. 시신을 찍는다. Save `p7-corpse.png`. Pass when 시신이 길을 막는 칸에 있을 때.
- [ ] Lane 7. 스폰이 일어난 뒤를 찍는다. Save `p7-spawn.png`. Pass when 원래 병종 모델에 보라 틴트가 들어갈 때.
- [ ] Lane 8. 보름달 흡혈 장면을 찍는다. Save `p7-drain.png`. Pass when 흡혈 연출 뒤 뱀파이어 체력이 회복값만큼 오를 때.
- [ ] Lane 9. `runtime set_time_scale --scale 4` 로 데모를 끝까지 돌린다. Save `p7-fast.png`. Pass when 끝 상태가 `Battle` 과 같고 연출이 남아 있지 않을 때.
- [ ] Lane 10. `console get --level warn` 을 본다. Save `p7-console.png`. Pass when 경고가 0개일 때.

**Verify, perf.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked.

- [ ] Metric. 유닛이 가장 많은 10장에서 적 페이즈를 재생하는 동안의 프레임 시간.
- [ ] Probe. trunk(P6) 와 head 에서 번갈아 10장을 재생하고 `diagnostics get_performance` 를 세 번 읽는다.
- [ ] Baseline. P6 보드만 있는 프레임 시간을 먼저 적는다.
- [ ] Rule. head 프레임 시간이 16.7ms 를 넘거나 P6 보다 8ms 넘게 늘면 실패다.

**Review gate.** The operator reviews before merge.

- [ ] Lane 2, 4, 7, 8 스크린샷을 `docs/review/p7-review-<slug>.png` 로 복사한다 (review screenshot).
- [ ] 데모 적 페이즈 두 번을 담은 30~60초 영상을 Unity Recorder 로 찍어 `docs/review/p7-review.mp4` 로 둔다 (review video).
- [ ] 스크린샷과 영상을 채팅에 올린다. merge-ready 에서 멈추고 사용자의 결정을 기다린다 (operator).

**Merge.**

- [ ] 정확한 head SHA 에서 루트의 깨끗한 판정.
- [ ] Bugbot 분류 끝.
- [ ] 판정 뒤 부모 최신으로 rebase, patch-id 그대로.
- [ ] 루트가 스택에 잇고, 사용자가 아래에서부터 머지한다.

## 칸 선택, 배치, 범위 표시를 붙인다 (P8)

**Depends on.** P7.

**Files.**

- [ ] Create `Assets/_Project/Scripts/Game/BoardInput.cs`.
- [ ] Create `Assets/_Project/Scripts/Game/OverlayView.cs`.
- [ ] Edit `Assets/_Project/Scripts/Game/BoardView.cs`.

**Build.**

- [ ] `BoardInput` 은 Input System 포인터를 보드 평면에 쏘아 칸을 고른다. HTML `onHex` 의 분기를 `Battle` 명령으로 바꾼다. 배치 단계의 끌어 옮기기, 바꾸기, 구역 밖으로 빼서 회수를 둔다.
- [ ] `OverlayView` 는 배치 구역, 이동 칸, 공격 칸, 처치 고리, 선택, 적 위협 범위, 보름달 표적 선과 범위를 그린다. 그릴 칸 목록은 Core 에서 계산하고 화면은 그리기만 한다.

**You see.**

- [ ] 아군을 누르면 이동 칸이 밝아지고, 붉은 칸의 적을 누르면 공격하고, 배치 단계에서 유닛을 끌어 옮길 수 있다.

**Verify, unit.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked.

- [ ] `OverlayTests` 가 데모에서 기사를 골랐을 때 이동 칸과 공격 칸 목록이 HTML `renderOverlay` 의 칸과 같은지 확인한다. 기대값은 `prototype-golden.mjs overlay` 로 뽑는다. Run `unity command test run --mode editor --filter Overlay`.

**Verify, live.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked. Ten lanes on `opus low` at the PR head, per the boot recipe.

- [ ] Lane 1. Regression lane against trunk. trunk 에서 기사 칸을 눌러도 아무 일이 없음을 기록한다. head 에서 `runtime simulate_pointer` 로 기사 칸을 누른다. Save `p8-select.png`. Pass when 이동 칸이 `OverlayTests` 기대값과 같게 밝아질 때.
- [ ] Lane 2. 밝은 칸을 눌러 이동한다. Save `p8-move.png`. Pass when 기사가 그 칸에 서고 이동 칸이 사라질 때.
- [ ] Lane 3. 붉은 칸의 적을 누른다. Save `p8-attack.png`. Pass when 피해 숫자가 뜨고 기사가 행동 끝 표시로 바뀔 때.
- [ ] Lane 4. 배치 단계에서 빈 배치 칸을 누른다. Save `p8-deploy.png`. Pass when 고른 병종이 생기고 인구가 늘 때.
- [ ] Lane 5. 인구가 꽉 찬 상태에서 배치 칸을 누른다. Save `p8-pop-full.png`. Pass when 유닛이 생기지 않고 `인구가 부족합니다` 안내가 뜰 때.
- [ ] Lane 6. 배치한 유닛을 다른 유닛 위로 끌어 놓는다. Save `p8-swap.png`. Pass when 두 유닛 자리가 바뀔 때.
- [ ] Lane 7. 배치한 유닛을 구역 밖으로 끈다. Save `p8-recall.png`. Pass when 유닛이 사라지고 `회수했습니다` 안내가 뜰 때.
- [ ] Lane 8. 적 위협 범위를 켠다. Save `p8-threat.png`. Pass when 칸 목록이 HTML `threatHexes` 와 같을 때.
- [ ] Lane 9. 보름달 전 턴을 만든다. Save `p8-moon-mark.png`. Pass when 표적 선과 2칸 범위가 그려질 때.
- [ ] Lane 10. `Escape` 를 `runtime simulate_key` 로 누른다. Save `p8-escape.png`. Pass when 선택이 풀릴 때.

**Verify, perf.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked.

- [ ] Metric. 칸을 누른 뒤 표시를 다시 그릴 때까지 걸린 시간과 재생 중 프레임 시간.
- [ ] Probe. trunk(P7) 와 head 에서 번갈아 같은 장면을 띄우고, head 에서는 `script eval` 의 `Stopwatch` 로 표시 다시 그리기를 100번 잰다.
- [ ] Baseline. P7 프레임 시간을 먼저 적는다.
- [ ] Rule. 다시 그리기 중앙값이 5ms 를 넘거나 프레임 시간이 16.7ms 를 넘으면 실패다.

**Review gate.** The operator reviews before merge.

- [ ] Lane 1, 4, 6, 8, 9 스크린샷을 `docs/review/p8-review-<slug>.png` 로 복사한다 (review screenshot).
- [ ] 배치부터 첫 공격까지 30~60초 영상을 Unity Recorder 로 찍어 `docs/review/p8-review.mp4` 로 둔다 (review video).
- [ ] 스크린샷과 영상을 채팅에 올린다. merge-ready 에서 멈추고 사용자의 결정을 기다린다 (operator).

**Merge.**

- [ ] 정확한 head SHA 에서 루트의 깨끗한 판정.
- [ ] Bugbot 분류 끝.
- [ ] 판정 뒤 부모 최신으로 rebase, patch-id 그대로.
- [ ] 루트가 스택에 잇고, 사용자가 아래에서부터 머지한다.

## 메뉴, 캠페인, 전투 UI 를 붙인다 (P9)

**Depends on.** P8.

**Files.**

- [ ] Create `Assets/_Project/UI/` 아래 메뉴, 캠페인, 전투 상단 막대, 옆 패널, 결과 창 프리팹.
- [ ] Create `Assets/_Project/Scripts/Game/Screens/*.cs`.
- [ ] Create `Assets/_Project/UI/Fonts/` 의 한글 TMP 폰트 에셋.
- [ ] Delete P6 의 `GameBootstrap` 시험용 진입점.
- [ ] Edit `docs/areas/battle.md`, `docs/systems/unity-port.md`.

**Build.**

- [ ] uGUI 와 INTERFACE Dark Fantasy HUD·Menus 스프라이트로 화면 셋(메뉴, 캠페인, 전투)을 만든다. HTML 의 모든 문장(편성, 작전 개요, 유닛 카드 경고, 전투 기록, 규칙 요약, 결과 보상)을 같은 글자로 쓴다.
- [ ] 한글 폰트는 HTML 이 쓰는 Nanum Myeongjo 와 IBM Plex Sans KR (둘 다 OFL) 이다. 내려받기 전에 사용자에게 파일 이름, 출처, 크기를 알리고 허락을 받는다.
- [ ] 화면 전환, 같은 편성으로 다시, 편성부터 다시, 나가기 두 번 누르기, 진행 초기화 두 번 누르기를 HTML 과 같게 둔다.

**You see.**

- [ ] 메뉴에서 캠페인으로 들어가 1장을 고르고, 배치하고, 이기면 결과 창에 보상이 뜨고, 캠페인 화면 금화가 오른다.

**Verify, unit.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked.

- [ ] `UiTextParityTests` 가 HTML 에서 뽑은 고정 문장 목록(`prototype-golden.mjs strings`)이 UI 프리팹 문자열에 모두 있는지 확인한다. Run `unity command test run --mode editor --filter UiTextParity`.

**Verify, live.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked. Ten lanes on `opus low` at the PR head, per the boot recipe.

- [ ] Lane 1. Regression lane against trunk. trunk(P8) 는 시험용 진입점으로 바로 보드가 뜬다는 것을 기록한다. head 에서 재생하고 메뉴를 찍는다. Save `p9-menu.png`. Pass when 제목과 두 선택지가 HTML 과 같은 글로 보일 때.
- [ ] Lane 2. 데모를 누르고 배치 화면을 찍는다. Save `p9-deploy.png`. Pass when 작전 개요, 편성 목록, 인구 0/10 이 보일 때.
- [ ] Lane 3. 예시 편성과 전투 개시를 누른다. Save `p9-battle.png`. Pass when 상단에 달, 턴, 아군, 적 수가 보이고 버튼이 `턴 종료` 일 때.
- [ ] Lane 4. 유닛 카드를 연다. Save `p9-card.png`. Pass when 체력, 공격, 방어, 이동, 사거리가 `Battle` 값과 같을 때.
- [ ] Lane 5. 전투 기록을 여러 턴 쌓는다. Save `p9-log.png`. Pass when 맨 위 기록이 `Battle` 의 마지막 기록과 같을 때.
- [ ] Lane 6. 데모를 끝까지 지고 결과 창을 찍는다. Save `p9-defeat.png`. Pass when 전사, 처치, 스폰, 흡혈 숫자가 `Battle` 통계와 같을 때.
- [ ] Lane 7. 캠페인에서 1장을 이긴다. Save `p9-reward.png`. Pass when 결과 창 보상과 캠페인 금화가 P5 값과 같을 때.
- [ ] Lane 8. 병영에서 강화를 사고 초기화한다. Save `p9-armory.png`. Pass when 버튼 두 번 확인 뒤 금화가 돌아올 때.
- [ ] Lane 9. 한글 글자가 빠짐없이 그려지는지 결과 창과 규칙 요약을 찍는다. Save `p9-font.png`. Pass when 네모 칸(빠진 글자)이 하나도 없을 때.
- [ ] Lane 10. 전투 중 나가기를 한 번 누르고 2.5초 기다린다. Save `p9-leave.png`. Pass when `전투를 포기할까요?` 가 떴다가 `나가기` 로 돌아올 때.

**Verify, perf.** Tests alone are not sufficient verification. A PR is verified only when its unit, live, and perf boxes are all checked.

- [ ] Metric. 메뉴에서 전투 배치 화면까지 걸린 시간과 전투 중 프레임 시간.
- [ ] Probe. trunk(P8) 와 head 에서 번갈아 재생하고 `diagnostics get_performance` 를 세 번 읽는다. 화면 전환 시간은 `console write` 로 남긴 시각 차이로 잰다.
- [ ] Baseline. P8 프레임 시간을 먼저 적는다.
- [ ] Rule. 화면 전환이 1초를 넘거나 프레임 시간이 16.7ms 를 넘으면 실패다.

**Review gate.** The operator reviews before merge.

- [ ] Lane 1, 2, 3, 6, 7 스크린샷을 `docs/review/p9-review-<slug>.png` 로 복사한다 (review screenshot).
- [ ] 메뉴부터 1장 승리까지 30~60초 영상을 Unity Recorder 로 찍어 `docs/review/p9-review.mp4` 로 둔다 (review video).
- [ ] 스크린샷과 영상을 채팅에 올린다. merge-ready 에서 멈추고 사용자의 결정을 기다린다 (operator).

**Merge.**

- [ ] 정확한 head SHA 에서 루트의 깨끗한 판정.
- [ ] Bugbot 분류 끝.
- [ ] 판정 뒤 부모 최신으로 rebase, patch-id 그대로.
- [ ] 루트가 스택에 잇고, 사용자가 아래에서부터 머지한다.

## Close the program

- [ ] 위의 모든 상자가 증거와 함께 체크돼 있다.
- [ ] 이슈 #1 체크리스트를 사용자 원문과 한 줄씩 대조하고, 결과와 커밋을 댓글로 단 뒤 닫는다.
- [ ] 검증 worktree 와 담당 worktree 를 지운다 (CLAUDE.md).
- [ ] Autopilot-stack 이 정한 보고로 사용자에게 답한다.

## Appendix A. Prototype evidence

- HTML 게임을 node 에서 화면 없이 돌릴 수 있다. `<script>` 를 `vm` 에 넣고 `document` 를 빈 Proxy 로, `setTimeout` 을 `setImmediate` 로 바꿨다. 데모를 예시 편성과 턴 종료만으로 돌리면 7턴에 패배하고 전사 5, 스폰 2, 흡혈 1 이 나왔다. 두 번 돌린 출력 해시가 같았다 (`56712eb5…`). 측정한 결과다. 시험 스크립트는 `Temp/probe-golden.mjs` 에 있고, P2 가 `tools/golden/prototype-golden.mjs` 로 키운다. 브랜치는 만들지 않았다.
- Unity CLI 로 실행 중인 에디터의 game view 를 PNG 로 찍을 수 있다. `unity command capture game_view --output-path Temp/probe.png` 이 76KB 파일을 만들었다. 측정한 결과다.
- Unity CLI 에 재생 시작·정지, 포인터·키 입력, C# 실행, 테스트 실행, 콘솔 읽기, 성능 읽기, 씬 Build Settings 등록 명령이 있다. `unity command <그룹>` 도움말에서 읽었다. 포인터 입력이 실제 게임에 닿는지는 아직 확인하지 않았다 (P8 Lane 1 이 증명한다).
- 아직 증명하지 않은 것. SIDEKICK 모델이 URP 에서 깨지지 않는지(P1 Lane 4, 5), ANIMATION 클립이 PolygonDarkFantasy 캐릭터에 맞는지(P7 Lane 4), Unity Recorder 로 영상을 CLI 에서 찍을 수 있는지(P6 리뷰 관문).

## Appendix B. Alternatives rejected

- HTML 규칙을 손으로만 옮기고 눈으로 비교하는 방법. 기록 문장, 아이디 순서, 적 행동 순서까지 같아야 하는데 사람 눈으로는 못 잡는다. node 골든 기록이 기계적으로 비교하므로 버렸다.
- 규칙을 MonoBehaviour 와 코루틴 안에 HTML 처럼 화면과 섞어 옮기는 방법. 테스트하려면 재생 모드가 필요하고 느리다. 규칙은 Unity 를 모르는 Core 에 두고 이벤트 목록으로 화면과 나눴다.
- 적 페이즈를 코루틴으로 한 단계씩 계산하는 방법. HTML 은 `await wait()` 중에 상태를 바꾸지만, 바뀐 상태가 그 사이 입력에 닿지 않는다(`busy`, `enemy` 단계). 한 번에 계산하고 재생만 나눠도 결과가 같다.
- 데이터를 ScriptableObject 로 두는 방법. 지금은 HTML 이 원본이고 값이 바뀌지 않으므로 C# 표와 비교 테스트가 더 짧다. 기획 문서가 `docs/design` 에 생기고 수치 조정이 잦아지면 다시 본다.
- UI Toolkit. INTERFACE Dark Fantasy 패키지가 uGUI 프리팹이라고 추정해서 uGUI 를 골랐다. 추정이다. P1 Lane 7 에서 패키지 구성을 보고 P9 전에 확정한다.
- PR 마다 클라우드 VM 열 대에서 lane 을 동시에 돌리는 방법. 이 저장소는 Claude Code 로컬 환경이고 Synty 가 git 에 없어서 클라우드 클론에는 에셋이 없다. 로컬 검증 트리 하나에서 차례로 돌린다.

## Appendix C. Risks

1. 검증 스킬 `verify-unity` 는 에디터가 이미 열린 상태에서만 끝까지 돌려 봤다. `vu.sh launch` 가 에디터를 새로 여는 경로와 `cleanup` 이 그 에디터를 닫는 경로는 아직 돌리지 않았다. 검증 트리를 처음 만드는 P1 에서 증명한다.
2. Unity 에디터는 한 프로젝트를 한 번만 연다. 담당 worktree 와 검증 트리마다 에디터가 하나씩 뜨고, 첫 임포트에 Synty 391MB 를 다시 읽는다. 동시에 여는 에디터는 둘(담당 하나, 검증 하나)로 제한한다. 모든 PR 에서 담당이 지켜본다.
3. 에디터가 같은 프로젝트를 열고 있으면 batchmode `unity test` 와 `unity build` 가 잠금으로 실패한다 (CLAUDE.md). 테스트는 실행 중인 에디터에 `unity command test run` 으로 보낸다. 모든 PR 에서 지켜본다.
4. Synty 에 관(coffin) 프리팹이 없다. PolygonDarkFantasy 에서 `coffin`, `sarcoph`, `tomb` 으로 찾았지만 나오지 않았다. P7 에서 SIDEKICK 과 다른 패키지를 다시 찾고, 없으면 상자·석관 비슷한 소품으로 대신한 뒤 사용자에게 알린다.
5. 한글 폰트 내려받기는 사용자 허락이 필요하다. P9 담당이 파일 이름, 출처, 크기를 알리고 기다린다.
6. ANIMATION 패키지는 SIDEKICK 용으로 보인다. PolygonDarkFantasy 캐릭터(스켈레톤, 뱀파이어, 사냥꾼)에 Humanoid 리타깃이 되지 않으면 그 유닛은 클립 없이 기울기·이동 연출만 쓴다. P7 담당이 지켜본다.
7. HTML 은 `localStorage` 키 `pvs-campaign-v2` 를 쓴다. Unity 저장은 `PlayerPrefs` 라서 브라우저 저장을 옮기지 않는다. 옮길 필요가 생기면 사용자에게 묻는다. P5 담당이 기록한다.
8. HTML 이 바뀌면 골든 기록이 바뀐다. `tools/golden/out` 은 커밋하고, 다시 만들 때 diff 를 PR 에 보인다. P2~P5 담당이 지켜본다.

## Appendix D. Links and reading list

- 원본 게임 `docs/Prototype.html`, 작업 규칙 `CLAUDE.md`, 기획 목차 `design.md`, 분야 안내서 `docs/areas/README.md`, `docs/areas/art-assets.md`.
- P4 는 시작 전에 `<pstack>/skills/how/SKILL.md` 로 HTML 의 `enemyPhase`, `meleeAct`, `fullMoonAct` 흐름을 정리하고, 리뷰 전에 `<pstack>/skills/interrogate/SKILL.md` 를 돌린다. 전투 규칙이 가장 틀리기 쉬운 곳이기 때문이다.
- P6 은 리뷰 전에 `<pstack>/skills/interrogate/SKILL.md` 로 `ArtCatalog` 구조를 검토받는다. 이후 PR 이 모두 그 표를 쓰기 때문이다.
- 결정 기록은 `<pstack>/skills/show-me-your-work/SKILL.md` 대로 이슈 #1 댓글과 `docs/systems/unity-port.md` 에 남긴다.
- Unity CLI 명령 목록은 `unity command <그룹>` 으로 본다. 이 계획이 쓰는 그룹은 `capture`, `editor.playmode`, `runtime`, `console`, `script`, `test`, `asset`, `scene`, `diagnostics` 다.
