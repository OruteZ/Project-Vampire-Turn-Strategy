const cell = (c, r) => `${c},${r}`;
const MAX_TURNS = 40;

export function snapshot(p) {
  const S = p.S;
  const b = (v) => (v ? 1 : 0);
  const won = p.__won === undefined ? '-' : p.__won ? 1 : 0;
  const head = `t=${S.turn} ph=${S.phase} moon=${S.moon} eat=${S.eatTarget ?? '-'} won=${won} st=${S.stats.lost},${S.stats.executed},${S.stats.risen},${S.stats.drained}`;
  const units = S.units.map((u) => {
    const flags = u.side === 'ally'
      ? `m${b(u.moved)}a${b(u.acted)}d${u.movedDist}c${b(u.cleaved)}`
      : `s${b(u.asleep)}r${b(u.justRisen)}w${b(u.justWoke)}`;
    return `${u.id}:${u.side}:${u.kind}:${u.name}:${u.glyph}:${u.origin || ''}:${cell(u.x, u.y)}:${u.hp}/${u.maxHp}:${u.atk},${u.def},${u.mov},${u.rng},${u.bane || 0},${u.heal || 0}:${(u.abil || []).join('+')}:${flags}`;
  });
  const corpses = S.corpses.map((c) => `${c.id}:${c.kind}:${c.name}:${c.glyph}:${cell(c.x, c.y)}:${c.maxHp},${c.atk},${c.def},${c.mov}`);
  return [head, ...units.map((u) => 'U ' + u), ...corpses.map((c) => 'C ' + c)].join('\n');
}

function instrument(p, sink) {
  const wrap = (name, fmt) => {
    const orig = p[name];
    p[name] = function (...a) { sink.push(fmt(...a)); return orig.apply(this, a); };
  };
  wrap('log', (text, cls) => `log|${text}|${cls || ''}`);
  wrap('showBanner', (text, cls) => `banner|${text}|${cls}`);
  wrap('popup', (o, text, cls) => `popup|${o.id}|${text}|${cls}`);
  wrap('hitFx', (o) => `hit|${o.id}`);
  wrap('flashHint', (text) => `hint|${text}`);
  const finish = p.finish;
  p.finish = function (win) { p.__won = win; return finish.call(this, win); };
  const walk = p.walk;
  p.walk = function (u, c, r) {
    sink.push(`walk|${u.id}|${p.pathTo(u, c, r).map(([x, y]) => cell(x, y)).join(' ')}`);
    return walk.call(this, u, c, r);
  };
}

export function commands(p) {
  const find = (id) => p.S.units.find((u) => u.id === id);
  const need = (ok, why) => { if (!ok) throw new Error('policy issued an illegal command: ' + why); };
  const click = (id, c, r) => { p.S.selected = id; p.S.mode = 'act'; p.onHex(c, r); };
  return {
    start: () => { need(p.S.phase === 'deploy', 'start outside deploy'); p.startBattle(); },
    endTurn: async () => { need(p.S.phase === 'player', 'endTurn outside player'); await p.enemyPhase(); },
    move: async ({ id, to }) => {
      const u = find(id), [c, r] = to.split(',').map(Number);
      click(id, c, r);
      need(u.moved && p.S.busy, 'onHex refused move ' + id + ' to ' + to);
      while (p.S.busy) await new Promise((res) => setImmediate(res));
    },
    attack: ({ id, target }) => {
      const u = find(id), t = find(target), hp = t.hp;
      click(id, t.x, t.y);
      need(t.hp < hp || !p.S.units.includes(t), 'onHex refused attack ' + id);
    },
    execute: ({ id, target }) => {
      const t = find(target);
      p.S.selected = id; p.S.mode = 'execute';
      p.onHex(t.x, t.y);
      need(!p.S.units.includes(t), 'onHex refused execute ' + id);
    },
    wait: ({ id }) => { const u = find(id); need(p.S.phase === 'player' && u && !u.acted, 'wait ' + id); u.acted = u.moved = true; },
    place: ({ kind, to }) => {
      const [c, r] = to.split(',').map(Number);
      need(p.S.phase === 'deploy' && p.inDeploy(c, r) && !p.unitAt(c, r), 'place ' + to);
      p.S.deployType = kind; p.onHex(c, r);
      need(p.unitAt(c, r), 'place rejected at ' + to);
    },
    recall: ({ id }) => { need(p.S.phase === 'deploy' && find(id), 'recall ' + id); p.S.units = p.S.units.filter((u) => u.id !== id); },
    relocate: ({ id, to }) => {
      const u = find(id), [c, r] = to.split(',').map(Number);
      need(p.S.phase === 'deploy' && u && u.side === 'ally', 'relocate ' + id);
      p.drag = { u, sx: 0, sy: 0, moved: true, target: [c, r] };
      p.endDrag({}, false);
    },
    clear: () => { need(p.S.phase === 'deploy', 'clear'); p.S.units = p.S.units.filter((u) => u.side !== 'ally'); p.S.inspect = null; },
    sample: () => {
      need(p.S.phase === 'deploy', 'sample');
      p.S.units = p.S.units.filter((u) => u.side !== 'ally');
      for (const [t, c, r] of p.S.stage.sample) p.S.units.push(p.mkAlly(t, c, r));
      p.trimToCaps();
    },
  };
}

const over = (p) => p.S.phase === 'over';
const awake = (p) => p.enemies().filter((e) => !e.asleep);
const inRange = (p, a) => awake(p).filter((e) => p.hd(a, e) <= a.rng);
const weakest = (list) => list.reduce((b, t) => (t.hp < b.hp ? t : b));
const nearest = (p, a, list) => list.reduce((b, t) => (p.hd(a, t) < p.hd(a, b) ? t : b));

async function fight(p, act, a, { waits }) {
  if (over(p) || !p.S.units.includes(a) || a.acted) return;
  for (let tries = 0; tries < 2 && !over(p) && !a.acted && inRange(p, a).length; tries++) {
    await act({ op: 'attack', id: a.id, target: weakest(inRange(p, a)).id });
  }
  if (over(p) || a.acted || a.moved) return;
  const targets = awake(p).length ? awake(p) : p.enemies();
  if (!targets.length) return;
  const goal = nearest(p, a, targets);
  let best = null, bd = p.hd(a, goal);
  for (const [k] of p.reachable(a)) {
    const [c, r] = k.split(',').map(Number);
    if (c === a.x && r === a.y) continue;
    const d = p.hd({ x: c, y: r }, goal);
    if (d < bd) { bd = d; best = k; }
  }
  if (best) {
    await act({ op: 'move', id: a.id, to: best });
    if (!over(p) && !a.acted && inRange(p, a).length) await act({ op: 'attack', id: a.id, target: weakest(inRange(p, a)).id });
  } else if (waits) {
    await act({ op: 'wait', id: a.id });
  }
}

async function saveEatTarget(p, act, preferExecute) {
  const v = p.vampire();
  const t = typeof p.S.eatTarget === 'number' ? p.S.units.find((u) => u.id === p.S.eatTarget) : null;
  if (!v || !t || t.moved || t.acted) return;
  const ex = p.allies().find((a) => a !== t && !a.acted && p.hd(a, t) <= a.rng);
  if (preferExecute && ex) { await act({ op: 'execute', id: ex.id, target: t.id }); return; }
  for (const [k] of p.reachable(t)) {
    const [c, r] = k.split(',').map(Number);
    if ((c !== t.x || r !== t.y) && (p.lit(c, r) || p.hd(v, { x: c, y: r }) > p.EAT_RANGE)) { await act({ op: 'move', id: t.id, to: k }); return; }
  }
  if (ex) await act({ op: 'execute', id: ex.id, target: t.id });
}

const POLICIES = {
  passive: { deploy: () => [{ op: 'clear' }, { op: 'sample' }], turn: async () => {} },
  aggressive: {
    deploy: (p) => { const a = p.allies(); return a.length > 1 ? [{ op: 'relocate', id: a[0].id, to: cell(a[1].x, a[1].y) }] : []; },
    turn: async (p, act) => { for (const a of [...p.allies()]) await fight(p, act, a, { waits: false }); },
  },
  moonwise: {
    deploy: (p) => {
      const last = p.allies().at(-1);
      if (!last) return [];
      for (let r = p.ROWS - 1; r >= 0; r--) for (let c = 0; c < p.COLS; c++) {
        if (p.inDeploy(c, r) && !p.unitAt(c, r) && (c !== last.x || r !== last.y)) return [{ op: 'recall', id: last.id }, { op: 'place', kind: last.kind, to: cell(c, r) }];
      }
      return [];
    },
    turn: async (p, act) => { await saveEatTarget(p, act, false); for (const a of [...p.allies()]) await fight(p, act, a, { waits: true }); },
  },
};
const emptyDeploy = (p, skip) => {
  for (let r = p.ROWS - 1; r >= 0; r--) for (let c = 0; c < p.COLS; c++) if (p.inDeploy(c, r) && !p.unitAt(c, r) && !skip(c, r)) return cell(c, r);
  return null;
};
POLICIES.upgraded = {
  deploy: (p) => {
    const a = p.allies();
    if (a.length < 2) return [];
    const empty = emptyDeploy(p, () => false);
    const out = [];
    if (empty) out.push({ op: 'relocate', id: a[0].id, to: empty });
    out.push({ op: 'relocate', id: a[1].id, to: '0,0' });
    return out;
  }, turn: async (p, act) => { await saveEatTarget(p, act, true); for (const a of [...p.allies()]) await fight(p, act, a, { waits: true }); } };

async function runTrace(p, stage, policyName, upgrades) {
  const events = [];
  instrument(p, events);
  const run = commands(p);
  const policy = POLICIES[policyName];
  p.uid = 1;
  p.__won = undefined;
  p.M = stage.id === 'demo' ? 'demo' : 'camp';
  p.C = { gold: 0, cleared: {}, bonus: {}, ups: Object.fromEntries(upgrades.map((id) => [id, true])) };
  p.newBattle(stage);
  const steps = [];
  const act = async (cmd) => {
    events.length = 0;
    const { op, ...args } = cmd;
    await run[op](args);
    steps.push({ cmd: JSON.stringify(cmd), events: [...events], snap: snapshot(p) });
  };
  const initial = snapshot(p);
  for (const cmd of policy.deploy(p)) await act(cmd);
  await act({ op: 'start' });
  while (!over(p) && p.S.turn <= MAX_TURNS) {
    await policy.turn(p, act);
    if (!over(p)) await act({ op: 'endTurn' });
  }
  return { stage: stage.id, policy: policyName, upgrades, initial, finalLog: p.S.log.map((l) => `${l.text}|${l.cls}`), steps };
}

export async function extractBattles(load) {
  const traces = [];
  const p0 = load();
  const allUps = Object.values(p0.UPGRADES).flat().map((u) => u.id);
  for (const policy of ['passive', 'aggressive', 'moonwise', 'upgraded']) {
    for (const stage of p0.STAGES) {
      if (policy === 'upgraded' && stage.id === 'demo') continue;
      const p = load();
      traces.push(await runTrace(p, p.STAGES.find((s) => s.id === stage.id), policy, policy === 'upgraded' ? allUps : []));
    }
  }
  return traces;
}
