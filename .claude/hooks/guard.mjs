// PreToolUse: blocks what CLAUDE.md forbids. Exit 2 sends the message back to the session.
import { lstatSync } from 'node:fs';
import { resolve } from 'node:path';

const VU = 'bash .claude/skills/verify-unity/scripts/vu.sh';
const UNITY_YAML = /\.(unity|prefab|asset|mat|anim|controller|overrideController)$/i;

const EDITOR = `Unity 에디터·batchmode 를 직접 띄우지 않는다. 이 컴퓨터는 동시에 최대 2개이고, 세션끼리 vu.sh 의 슬롯 잠금으로 순서를 정한다.
- 에디터: VU_PROJECT=<작업 트리> ${VU} launch
- batchmode(시험·빌드): VU_PROJECT=<작업 트리> ${VU} batch <unity 인자...>
- 열린 에디터에 보내는 시험은 vu.sh test 를 쓴다.
- vu.sh 가 exit 3 "WAIT" 를 내면 자리가 빌 때까지 기다린다: 같은 명령을 VU_WAIT=1800 을 붙여 run_in_background 로 돌리고, 그동안 에디터 없이 되는 일을 한다. 남의 에디터는 끄지 않는다. 끝까지 자리가 없으면 그 단계는 체크하지 않고 댓글 "에디터 대기(2개 한도)" + 라벨 에디터 필요, 보고엔 "빌드 미검증(에디터 한도)".
- 다 쓰면 vu.sh cleanup (에디터는 켜 둔 채 "놀고 있음" 으로 두고, 자리가 필요한 세션이 끈다). 작업 트리를 지우기 전엔 vu.sh close.`;

function checkCommand(cmd) {
  if (/^\s*git\s+commit\b/.test(cmd)) return;
  if (!/vu\.sh/.test(cmd)) {
    const cliLaunch = /(^|[\s;&|(`])unity(\.exe)?\s+(--?[\w-]+\s+)*(open|test|build)\b/i.test(cmd);
    const rawLaunch = /-batchmode\b/i.test(cmd) ||
      (/Unity\.exe/i.test(cmd) && /-projectpath/i.test(cmd) && !/Win32_Process|Get-CimInstance|Get-Process|tasklist/i.test(cmd));
    if (cliLaunch || rawLaunch) return EDITOR;
  }
  const rm = cmd.match(/git\s+(?:-C\s+("[^"]+"|\S+)\s+)?worktree\s+remove\s+(?:--?\w+\s+)*("[^"]+"|\S+)/);
  if (rm) {
    const tree = resolve(rm[2].replace(/"/g, ''));
    const linked = ['Assets/Synty', 'UPackage'].filter(j => { try { return lstatSync(resolve(tree, j)).isSymbolicLink(); } catch { return false; } });
    if (linked.length) return `worktree 를 지우기 전에 junction 부터 끊는다. Windows 의 git worktree remove 는 junction 을 따라가 메인 트리의 유료 에셋을 지운다.
${linked.map(j => `cmd //c rmdir "${resolve(tree, j)}"`).join('\n')}`;
  }
  const other = cmd.match(/git\s+-C\s+("[^"]+"|\S+)\s+(checkout|switch)\b/);
  if (other && resolve(other[1].replace(/"/g, '')).toLowerCase() !== resolve(process.env.CLAUDE_PROJECT_DIR ?? '.').toLowerCase())
    return '다른 작업 트리의 브랜치를 바꾸지 않는다. 그 트리를 쓰는 에이전트가 있다. merge 는 git -C <트리> merge --no-ff 로 한다.';
}

function checkFile(path, text) {
  const p = path.replace(/\\/g, '/');
  if (/(^|\/)docs\/design\//.test(p))
    return 'docs/design/ 은 사람이 쓰는 기획 방향이라 에이전트는 읽기만 한다(새 파일·수정·이동·삭제 모두 안 함). 바꿀 방향이면 기획서(docs/ 의 다른 폴더)에 쓰거나 사용자에게 docs/design 수정을 부탁한다.';
  if (UNITY_YAML.test(p))
    return '씬·프리팹·에셋 YAML 은 직접 고치지 않는다. Unity CLI 로 고친다 (unity command ... 또는 vu.sh eval, verify-unity 스킬 참고).';
  if (/\.cs$/i.test(p) && /\[MenuItem\s*\(/.test(text) && !/\/ToolsWindow\.cs$/.test(p))
    return '새 [MenuItem] 을 만들지 않는다. 프로젝트 메뉴엔 도구 모음 창(Assets/_Project/Scripts/Editor/ToolsWindow.cs) 하나만 두고, 그 창에 분야별 버튼 한 줄로 더한다.';
}

let input = '';
process.stdin.on('data', d => (input += d)).on('end', () => {
  const { tool_name: tool = '', tool_input: ti = {} } = JSON.parse(input);
  let why;
  if (tool.startsWith('mcp__unityMCP__')) why = 'Unity MCP 는 쓰지 않는다. 에디터 작업은 Unity CLI(unity) 와 verify-unity 스킬의 vu.sh 로 한다.';
  else if (tool === 'Agent' || tool === 'Task') {
    if (/^pstack:/.test(ti.subagent_type ?? '') && !/pstack-unity/.test(ti.prompt ?? ''))
      why = 'pstack 에이전트 지시에 .claude/skills/pstack-unity/SKILL.md 의 절대경로를 넣고 "pstack 보다 이 파일 먼저" 라고 쓴다 (검증·PR·커밋 방식이 이 저장소와 다르다).';
  }
  else if (tool === 'Bash' || tool === 'PowerShell') why = checkCommand(ti.command ?? '');
  else if (ti.file_path || ti.notebook_path) {
    const text = [ti.content, ti.new_string, ...(ti.edits ?? []).map(e => e.new_string)].filter(Boolean).join('\n');
    why = checkFile(ti.file_path ?? ti.notebook_path, text);
  }
  if (!why) return;
  process.stderr.write(`차단 (CLAUDE.md 규칙, .claude/hooks/guard.mjs): ${why}\n`);
  process.exit(2);
});
