// Lists docs/request files and flags any whose status disagrees with its GitHub issue (docs/guides/request.md).
// node tools/requests/check.mjs [--offline]   exit 1 when a file is malformed or out of step with its issue.
import fs from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { sync } from '../board/board.mjs';

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const DIR = path.join(ROOT, 'docs', 'request');
const offline = process.argv.includes('--offline');
const STATUSES = ['request', 'working', 'blocked', 'done', 'dropped'];

function parse(file) {
  const text = fs.readFileSync(path.join(DIR, file), 'utf8').replace(/\r/g, '');
  const head = text.match(/^---\n([\s\S]*?)\n---\n/);
  const meta = {};
  if (head) for (const line of head[1].split('\n')) { const m = line.match(/^(\w+):\s*(.*)$/); if (m) meta[m[1]] = m[2].trim(); }
  const title = (text.match(/^# (.+)$/m) || [])[1] || '';
  return { file, meta, title, hasHead: !!head };
}

function issue(n) {
  if (offline) return null;
  try { return JSON.parse(execFileSync('gh', ['issue', 'view', n, '--json', 'state,labels'], { cwd: ROOT, encoding: 'utf8' })); } catch { return { state: 'MISSING', labels: [] }; }
}

const problems = [];
const files = fs.readdirSync(DIR).filter((f) => f.endsWith('.md') && !f.startsWith('_')).sort();
for (const f of files) {
  const r = parse(f);
  const s = r.meta.status || '';
  const n = (r.meta.issue || '').replace('#', '');
  const bad = (why) => problems.push(`${f}: ${why}`);
  if (!r.hasHead) bad('머리말(--- 블록)이 맨 위에 없다. _TEMPLATE.md 형식으로 맞춘다');
  if (!STATUSES.includes(s)) bad(`status "${s}" 는 ${STATUSES.join('/')} 중 하나여야 한다`);
  if (['working', 'blocked', 'done'].includes(s) && !n) bad(`status ${s} 인데 issue 가 비었다`);
  if (s === 'done' && !r.meta.done) bad('status done 인데 done(커밋) 이 비었다');
  const gh = n ? issue(n) : null;
  if (gh) {
    const blocked = gh.labels.some((l) => l.name === '막힘');
    if (gh.state === 'MISSING') bad(`이슈 #${n} 을 찾을 수 없다`);
    else if (gh.state === 'CLOSED' && !['done', 'dropped'].includes(s)) bad(`이슈 #${n} 은 닫혔는데 status 가 ${s}`);
    else if (gh.state === 'OPEN' && ['done', 'dropped', 'request'].includes(s)) bad(`이슈 #${n} 은 열렸는데 status 가 ${s}`);
    else if (gh.state === 'OPEN' && blocked !== (s === 'blocked')) bad(`이슈 #${n} 막힘 라벨(${blocked})과 status ${s} 가 다르다`);
  }
  console.log(`${(r.meta.id || '???').padEnd(4)} ${s.padEnd(8)} ${(n ? '#' + n : '-').padEnd(5)} ${r.title}  (${f})`);
}
const fresh = files.map(parse).filter((r) => r.meta.status === 'request');
if (fresh.length) console.log(`새 요청 ${fresh.length}개: ${fresh.map((r) => r.meta.id).join(', ')}. 사용자가 맡기면 시작한다.`);
for (const p of problems) console.log('확인 ' + p);
if (!offline) {
  try { sync(); } catch (e) { console.log('보드를 읽지 못했다: ' + e.message.split('\n')[0]); }
}
process.exit(problems.length ? 1 : 0);
