// Task board (docs/guides/work-status.md): request issues with task sub-issues on the GitHub Project "Vampire Spawn 작업".
//   node tools/board/board.mjs task <요청#> <담당 에이전트|OZ> <종류> "<제목>" ["<본문>"] [--after 12,13]   opens a task under the request
//   node tools/board/board.mjs after <#> <선행#> [<선행#> …]                                 marks predecessors (GitHub "blocked by")
//   node tools/board/board.mjs status <#> <작업 중|작업 완료|…>                                moves a task on the board
//   node tools/board/board.mjs sync                                                    puts every 작업/요청 issue on the board, fills 담당·종류 from the body line, prints the lists
import { execFileSync } from 'node:child_process';

const OWNER = 'OruteZ', REPO = 'Project-Vampire-Turn-Strategy', PROJECT = 'Vampire Spawn 작업';
export const WHO = ['에이전트', 'OZ'], KINDS = ['기획', '구현', '검증', '결정', '준비'], STATES = ['미작업', '작업 가능', '작업 중', '작업 완료'];

// 미작업 and 작업 가능 are not set by hand: they follow from whether any predecessor (blocked by) is still open.
const waitingState = (openBlockers) => (openBlockers > 0 ? '미작업' : '작업 가능');
const DERIVED = ['미작업', '작업 가능'];

const gh = (args) => execFileSync('gh', args, { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] });
const gql = (query, vars = {}) => {
  const args = ['api', 'graphql', '-f', `query=${query}`];
  for (const [k, v] of Object.entries(vars)) args.push(typeof v === 'number' ? '-F' : '-f', `${k}=${v}`);
  return JSON.parse(gh(args)).data;
};

export const bodyLine = (who, kind) => `담당: ${who} · 종류: ${kind}`;
export function parseLine(body) {
  const who = (body.match(/담당:\s*(에이전트|OZ)/) || [])[1];
  const kind = (body.match(/종류:\s*(기획|구현|검증|결정|준비)/) || [])[1];
  return { who, kind };
}

function openBlockers() {
  const nodes = gql(`query($q:String!){search(query:$q,type:ISSUE,first:100){nodes{... on Issue{number blockedBy(first:20){nodes{state}}}}}}`,
    { q: `repo:${OWNER}/${REPO} is:issue is:open label:작업` }).search.nodes;
  return new Map(nodes.map((n) => [n.number, n.blockedBy.nodes.filter((b) => b.state === 'OPEN').length]));
}

const issueId = (number) => gql(`query($o:String!,$r:String!,$n:Int!){repository(owner:$o,name:$r){issue(number:$n){id}}}`, { o: OWNER, r: REPO, n: number }).repository.issue.id;

function after(number, predecessors) {
  const id = issueId(number);
  for (const pre of predecessors) {
    gql(`mutation($i:ID!,$b:ID!){addBlockedBy(input:{issueId:$i,blockingIssueId:$b}){issue{number}}}`, { i: id, b: issueId(pre) });
    console.log(`#${number} 선행 #${pre}`);
  }
}

function issues() {
  const raw = gh(['issue', 'list', '-R', `${OWNER}/${REPO}`, '--state', 'all', '--limit', '500', '--json', 'number,title,state,body,labels,id,url']);
  return JSON.parse(raw).filter((i) => i.labels.some((l) => l.name === '작업' || l.name === '요청'));
}

// null when the token lacks the project scope or the project does not exist yet; callers fall back to issues alone.
function project() {
  let d;
  try {
    d = gql(`query($o:String!){user(login:$o){projectsV2(first:50){nodes{id number title
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
    const values = Object.fromEntries(it.fieldValues.nodes.filter((v) => v.field).map((v) => [v.field.name, v.name]));
    items.set(it.content.number, { id: it.id, values });
  }
  return { id: p.id, fields, items };
}

function setField(p, itemId, fieldName, value) {
  const f = p.fields[fieldName];
  const opt = f && f.options.find((o) => o.name === value);
  if (!opt) throw new Error(`보드에 칸 "${fieldName}" 값 "${value}" 이 없다`);
  gql(`mutation($p:ID!,$i:ID!,$f:ID!,$o:String!){updateProjectV2ItemFieldValue(input:{projectId:$p,itemId:$i,fieldId:$f,value:{singleSelectOptionId:$o}}){projectV2Item{id}}}`,
    { p: p.id, i: itemId, f: f.id, o: opt.id });
}

// The project auto-adds sub-issues (asynchronously, right after addSubIssue), so the item may appear between our lookup and our add.
function addItem(p, issueNodeId) {
  const find = () => gql(`query($c:ID!){node(id:$c){... on Issue{projectItems(first:20){nodes{id project{id}}}}}}`, { c: issueNodeId })
    .node.projectItems.nodes.find((n) => n.project.id === p.id);
  const existing = find();
  if (existing) return existing.id;
  try {
    return gql(`mutation($p:ID!,$c:ID!){addProjectV2ItemById(input:{projectId:$p,contentId:$c}){item{id}}}`, { p: p.id, c: issueNodeId }).addProjectV2ItemById.item.id;
  } catch (e) {
    if (!String(e.stdout || e.message).includes('already exists')) throw e;
    return find().id;
  }
}

function task(parent, who, kind, title, body = '', predecessors = []) {
  if (!WHO.includes(who)) throw new Error(`담당은 ${WHO.join('/')}`);
  if (!KINDS.includes(kind)) throw new Error(`종류는 ${KINDS.join('/')}`);
  const url = gh(['issue', 'create', '-R', `${OWNER}/${REPO}`, '--title', title, '--label', '작업', '--body', `${bodyLine(who, kind)}\n요청: #${parent}\n\n${body}`.trim()]).trim();
  const number = Number(url.split('/').pop());
  const ids = gql(`query($o:String!,$r:String!,$a:Int!,$b:Int!){repository(owner:$o,name:$r){a:issue(number:$a){id} b:issue(number:$b){id}}}`,
    { o: OWNER, r: REPO, a: parent, b: number }).repository;
  gql(`mutation($a:ID!,$b:ID!){addSubIssue(input:{issueId:$a,subIssueId:$b}){issue{number}}}`, { a: ids.a.id, b: ids.b.id });
  if (predecessors.length) after(number, predecessors);
  const p = project();
  if (p) {
    const item = addItem(p, ids.b.id);
    setField(p, item, 'Status', waitingState(openBlockers().get(number) || 0));
    setField(p, item, '담당', who);
    setField(p, item, '종류', kind);
  }
  const pre = predecessors.length ? `, 선행 ${predecessors.map((n) => '#' + n).join(' ')}` : '';
  console.log(`#${number} ${title} (요청 #${parent}, ${who}, ${kind}${pre})${p ? '' : '  보드 없음: 다음 sync 때 올라간다'}`);
}

function setStatus(number, state) {
  if (!STATES.includes(state)) throw new Error(`상태는 ${STATES.join('/')}`);
  const p = project();
  if (!p) throw new Error('보드가 없다 (gh auth refresh -s project)');
  setField(p, p.items.get(number)?.id || addItem(p, issueId(number)), 'Status', state);
  console.log(`#${number} → ${state}`);
}

export function sync({ quiet = false } = {}) {
  const all = issues();
  const p = project();
  const blockers = openBlockers();
  const rows = [];
  for (const i of all) {
    const isRequest = i.labels.some((l) => l.name === '요청');
    const line = parseLine(i.body || '');
    let values = {};
    if (p) {
      let it = p.items.get(i.number);
      if (!it) { it = { id: addItem(p, i.id), values: {} }; p.items.set(i.number, it); }
      const want = { Status: it.values.Status };
      if (i.state === 'CLOSED') want.Status = '작업 완료';
      else if (isRequest) want.Status = it.values.Status || '미작업';
      else if (!it.values.Status || DERIVED.includes(it.values.Status)) want.Status = waitingState(blockers.get(i.number) || 0);
      if (!isRequest && line.who && !it.values['담당']) want['담당'] = line.who;
      if (!isRequest && line.kind && !it.values['종류']) want['종류'] = line.kind;
      for (const [k, v] of Object.entries(want)) if (it.values[k] !== v) { setField(p, it.id, k, v); it.values[k] = v; }
      values = it.values;
    }
    if (!isRequest && i.state === 'OPEN') rows.push({ n: i.number, title: i.title, who: values['담당'] || line.who || '담당 없음', state: values.Status || '상태 모름', waits: blockers.get(i.number) || 0 });
  }
  if (quiet) return;
  if (!p) console.log('보드 없음 (gh auth refresh -s project 전이거나 Project 가 아직 없음). 이슈 본문 줄로만 보인다');
  for (const who of [...WHO, '담당 없음']) {
    const mine = rows.filter((r) => r.who === who);
    if (!mine.length) continue;
    console.log(`${who} 할 일 ${mine.length}개`);
    for (const r of mine.sort((a, b) => STATES.indexOf(b.state) - STATES.indexOf(a.state))) console.log(`  ${r.state.padEnd(5)} #${r.n} ${r.title}${r.waits ? `  (선행 ${r.waits}개 열림)` : ''}`);
  }
  const requests = gql(`query($q:String!){search(query:$q,type:ISSUE,first:50){nodes{... on Issue{number title subIssuesSummary{total completed}}}}}`,
    { q: `repo:${OWNER}/${REPO} is:issue is:open label:요청` }).search.nodes;
  const fresh = requests.filter((r) => r.subIssuesSummary.total === 0);
  const finished = requests.filter((r) => r.subIssuesSummary.total > 0 && r.subIssuesSummary.completed === r.subIssuesSummary.total);
  if (fresh.length) console.log(`새 요청 ${fresh.length}개 (태스크 없음, 맡기면 시작): ${fresh.map((r) => `#${r.number} ${r.title}`).join(', ')}`);
  if (finished.length) console.log(`태스크가 다 닫힌 요청 (대조 뒤 닫기): ${finished.map((r) => `#${r.number}`).join(', ')}`);
}

if (process.argv[1] && process.argv[1].replace(/\\/g, '/').endsWith('tools/board/board.mjs')) {
  const argv = process.argv.slice(2);
  const flag = argv.indexOf('--after');
  const predecessors = flag < 0 ? [] : argv.splice(flag, 2)[1].split(',').map((s) => Number(s.replace('#', '')));
  const [cmd, ...a] = argv;
  const num = (s) => Number(String(s).replace('#', ''));
  if (cmd === 'task') task(num(a[0]), a[1], a[2], a[3], a[4], predecessors);
  else if (cmd === 'after') after(num(a[0]), a.slice(1).map(num));
  else if (cmd === 'sync') sync();
  else if (cmd === 'status') setStatus(Number(String(a[0]).replace('#', '')), a[1]);
  else { console.log('node tools/board/board.mjs task <요청#> <에이전트|OZ> <종류> "<제목>" ["<본문>"] [--after 12,13] | after <#> <선행#…> | status <#> <상태> | sync'); process.exit(1); }
}
