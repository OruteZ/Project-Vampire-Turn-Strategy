// Topic board (docs/guides/work-status.md): one issue per big topic, its steps in order under "## 흐름", on the GitHub Project "Vampire Spawn 작업".
//   node tools/board/board.mjs open "<제목>" ["<목표>"]     opens a topic issue (label 주제) and puts it on the board as 미작업
//   node tools/board/board.mjs status <#> <상태>             미작업 | 작업 중 | 사용자 대기 | 작업 완료
//   node tools/board/board.mjs sync                          puts every topic issue on the board, closed → 작업 완료, prints the list
import { execFileSync } from 'node:child_process';

const OWNER = 'OruteZ', REPO = 'Project-Vampire-Turn-Strategy', PROJECT = 'Vampire Spawn 작업';
export const STATES = ['미작업', '작업 중', '사용자 대기', '작업 완료'];
// 작업·요청 are the labels of the old small-task issues; they stay on the board until closed.
const LABELS = ['주제', '작업', '요청'];

const gh = (args) => execFileSync('gh', args, { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] });
const gql = (query, vars = {}) => {
  const args = ['api', 'graphql', '-f', `query=${query}`];
  for (const [k, v] of Object.entries(vars)) args.push(typeof v === 'number' ? '-F' : '-f', `${k}=${v}`);
  return JSON.parse(gh(args)).data;
};

export const topicBody = (goal = '') => `## 목표
${goal || '<이 주제가 끝나면 무엇이 되나, 한두 줄>'}

## 흐름
- [ ]

## 관련
- 안내서: docs/areas/<분야>.md
- 브랜치: <세션이 시작하면 한 줄씩>
`;

// Steps are the checkboxes under "## 흐름"; "## 확인" boxes are the user's checklist and are not steps.
export function progress(body) {
  const flow = (body.split(/^## 흐름\s*$/m)[1] || '').split(/^## /m)[0];
  const done = (flow.match(/^\s*- \[x\]/gim) || []).length;
  const open = flow.match(/^\s*- \[ \]\s*(.*)$/m);
  return { done, total: done + (flow.match(/^\s*- \[ \]/gm) || []).length, next: open ? open[1].trim() : '' };
}

const issueId = (number) => gql(`query($o:String!,$r:String!,$n:Int!){repository(owner:$o,name:$r){issue(number:$n){id}}}`, { o: OWNER, r: REPO, n: number }).repository.issue.id;

function issues() {
  const raw = gh(['issue', 'list', '-R', `${OWNER}/${REPO}`, '--state', 'all', '--limit', '500', '--json', 'number,title,state,body,labels,id']);
  return JSON.parse(raw).filter((i) => i.labels.some((l) => LABELS.includes(l.name)));
}

// null when the token lacks the project scope or the project does not exist; callers fall back to issues alone.
function project() {
  let d;
  try {
    d = gql(`query($o:String!){user(login:$o){projectsV2(first:50){nodes{id title
      fields(first:30){nodes{... on ProjectV2SingleSelectField{id name options{id name}}}}
      items(first:100){nodes{id fieldValues(first:20){nodes{... on ProjectV2ItemFieldSingleSelectValue{name field{... on ProjectV2SingleSelectField{name}}}}}
        content{... on Issue{number}}}}}}}}`, { o: OWNER });
  } catch { return null; }
  const p = d.user.projectsV2.nodes.find((n) => n.title === PROJECT);
  if (!p) return null;
  const fields = Object.fromEntries(p.fields.nodes.filter((f) => f.name).map((f) => [f.name, f]));
  const items = new Map();
  for (const it of p.items.nodes) {
    if (!it.content || it.content.number == null) continue;
    const status = it.fieldValues.nodes.find((v) => v.field && v.field.name === 'Status');
    items.set(it.content.number, { id: it.id, status: status ? status.name : undefined });
  }
  return { id: p.id, fields, items };
}

function setStatusField(p, itemId, value) {
  const f = p.fields.Status;
  const opt = f && f.options.find((o) => o.name === value);
  if (!opt) throw new Error(`보드 Status 에 "${value}" 이 없다`);
  gql(`mutation($p:ID!,$i:ID!,$f:ID!,$o:String!){updateProjectV2ItemFieldValue(input:{projectId:$p,itemId:$i,fieldId:$f,value:{singleSelectOptionId:$o}}){projectV2Item{id}}}`,
    { p: p.id, i: itemId, f: f.id, o: opt.id });
}

function addItem(p, issueNodeId) {
  try {
    return gql(`mutation($p:ID!,$c:ID!){addProjectV2ItemById(input:{projectId:$p,contentId:$c}){item{id}}}`, { p: p.id, c: issueNodeId }).addProjectV2ItemById.item.id;
  } catch (e) {
    if (!String(e.stdout || e.message).includes('already exists')) throw e;
    return gql(`query($c:ID!){node(id:$c){... on Issue{projectItems(first:20){nodes{id project{id}}}}}}`, { c: issueNodeId })
      .node.projectItems.nodes.find((n) => n.project.id === p.id).id;
  }
}

function open(title, goal) {
  const url = gh(['issue', 'create', '-R', `${OWNER}/${REPO}`, '--title', title, '--label', '주제', '--body', topicBody(goal)]).trim();
  const number = Number(url.split('/').pop());
  const p = project();
  if (p) setStatusField(p, addItem(p, issueId(number)), '미작업');
  console.log(`#${number} ${title}${p ? '' : '  보드 없음: 다음 sync 때 올라간다'}`);
}

function setStatus(number, state) {
  if (!STATES.includes(state)) throw new Error(`상태는 ${STATES.join(' / ')}`);
  const p = project();
  if (!p) throw new Error('보드가 없다 (gh auth refresh -s project)');
  setStatusField(p, p.items.get(number)?.id || addItem(p, issueId(number)), state);
  console.log(`#${number} → ${state}`);
}

export function sync() {
  const p = project();
  const rows = [];
  for (const i of issues()) {
    let status;
    if (p) {
      let it = p.items.get(i.number);
      if (!it) { it = { id: addItem(p, i.id), status: undefined }; p.items.set(i.number, it); }
      const want = i.state === 'CLOSED' ? '작업 완료' : STATES.includes(it.status) && it.status !== '작업 완료' ? it.status : '미작업';
      if (it.status !== want) setStatusField(p, it.id, want);
      status = want;
    }
    if (i.state === 'OPEN') rows.push({ n: i.number, title: i.title, status: status || '상태 모름', ...progress(i.body || '') });
  }
  if (!p) console.log('보드 없음 (gh auth refresh -s project 전이거나 Project 가 없음). 상태 없이 이슈만 보인다');
  for (const s of ['사용자 대기', '작업 중', '미작업', '상태 모름']) {
    const mine = rows.filter((r) => r.status === s);
    if (!mine.length) continue;
    console.log(`${s} ${mine.length}개`);
    for (const r of mine) console.log(`  #${r.n} ${r.title}  [${r.done}/${r.total}]${r.next ? `  다음: ${r.next}` : ''}`);
  }
}

if (process.argv[1] && process.argv[1].replace(/\\/g, '/').endsWith('tools/board/board.mjs')) {
  const [cmd, ...a] = process.argv.slice(2);
  if (cmd === 'open') open(a[0], a[1]);
  else if (cmd === 'status') setStatus(Number(String(a[0]).replace('#', '')), a[1]);
  else if (cmd === 'sync') sync();
  else { console.log('node tools/board/board.mjs open "<제목>" ["<목표>"] | status <#> <미작업|작업 중|사용자 대기|작업 완료> | sync'); process.exit(1); }
}
