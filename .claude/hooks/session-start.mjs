// SessionStart: stdout lands in the session's context. Prints the board and only the setup checks that fail.
import { execSync } from 'node:child_process';
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';

const root = process.env.CLAUDE_PROJECT_DIR ?? process.cwd();
const run = cmd => { try { return execSync(cmd, { cwd: root, encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'], timeout: 60000 }).trim(); } catch { return null; } };
const read = f => { try { return readFileSync(join(root, f), 'utf8'); } catch { return ''; } };
const out = [];

if (run('gh auth status') === null) out.push('이슈 도구: gh 로그인이 안 돼 있다. 사용자에게 `gh auth login` 을 권하고, 안 되면 채팅으로 보고한다 (클라우드는 GitHub MCP).');
else {
  const board = run('node tools/board/board.mjs sync');
  out.push(board === null ? '작업 보드: board.mjs sync 실패. 직접 돌려 보고 사용자 대기 주제를 알린다.'
    : `작업 보드 (사용자에게 사용자 대기 주제를 알린다. 미작업 주제는 사용자가 맡길 때만 시작):\n${board}`);
}
if (!/m_SerializationMode: 2/.test(read('ProjectSettings/EditorSettings.asset')) || !/m_Mode: Visible Meta Files/.test(read('ProjectSettings/VersionControlSettings.asset')))
  out.push('Unity 설정: Asset Serialization 이 Force Text 가 아니거나 Version Control 이 Visible Meta Files 가 아니다. 바꾸기 전에 사용자에게 알린다.');
if (!run('git config merge.unityyamlmerge.driver'))
  out.push(`git: UnityYAMLMerge 가 없다. 한 번 한다: git config merge.unityyamlmerge.driver '"<Unity>/Editor/Data/Tools/UnityYAMLMerge.exe" merge -p %O %B %A %A'`);
if (!existsSync(join(root, 'Assets/Synty')))
  out.push('유료 에셋: Assets/Synty 가 없다. 메인 트리면 docs/guides/asset-setup.md 대로 bash tools/assets/setup.sh, 작업 트리면 메인 트리로 junction 을 건다 (worktree 스킬).');

console.log(out.join('\n\n'));
