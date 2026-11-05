const cell = (c, r) => `${c},${r}`;
const MAX_TURNS = 12;

export function recorder(p) {
  const els = {};
  const stub = p.document.getElementById('x');
  p.$ = (id) => (els[id] ||= new Proxy({}, {
    get: (o, k) => (k in o ? o[k] : k === 'textContent' || k === 'innerHTML' ? '' : stub[k]),
    set: (o, k, v) => { o[k] = v; return true; },
  }));
  return els;
}

function nearestHex(p, x, y) {
  let best = null, bd = 1e9;
  for (let r = 0; r < p.ROWS; r++) for (let c = 0; c < p.COLS; c++) {
    const [hx, hy] = p.px(c, r), d = (hx - x) ** 2 + (hy - y) ** 2;
    if (d < bd) { bd = d; best = cell(c, r); }
  }
  return best;
}

export function parseOverlay(p, html) {
  const out = [];
  for (const m of html.matchAll(/<polygon class="([^"]+)" points="([^"]+)"\/>/g)) {
    const pts = m[2].trim().split(' ').map((s) => s.split(',').map(Number));
    const x = pts.reduce((a, q) => a + q[0], 0) / pts.length, y = pts.reduce((a, q) => a + q[1], 0) / pts.length;
    out.push(`${m[1]}@${nearestHex(p, x, y)}`);
  }
  for (const m of html.matchAll(/<line class="mark-line" x1="([^"]+)" y1="([^"]+)" x2="([^"]+)" y2="([^"]+)"\/>/g)) {
    out.push(`line@${nearestHex(p, +m[1], +m[2])}>${nearestHex(p, +m[3], +m[4])}`);
  }
  for (const m of html.matchAll(/<text class="mark-label" x="([^"]+)" y="([^"]+)">([^<]*)<\/text>/g)) {
    out.push(`label:${m[3]}@${nearestHex(p, +m[1], +m[2] + p.R * 0.8)}`);
  }
  return out.sort();
}

async function runStage(p, stage, upgrades) {
  const els = recorder(p);
  p.uid = 1;
  p.M = stage.id === 'demo' ? 'demo' : 'camp';
  p.C = { gold: 0, cleared: {}, bonus: {}, ups: Object.fromEntries(upgrades.map((id) => [id, true])) };
  p.newBattle(stage);
  const steps = [];
  const hintEl = els.hint;
  const record = (op) => {
    const S = p.S;
    // flashHint writes textContent and the next render overwrites innerHTML; the flash is what the player sees first.
    const hint = hintEl.textContent || hintEl.innerHTML;
    hintEl.textContent = '';
    steps.push({
      op,
      ov: parseOverlay(p, els.lOv.innerHTML || ''),
      hint: (hint || '').replace(/<\/?b>/g, ''),
      sel: S.selected ?? -1,
      ins: S.inspect === null || S.inspect === undefined ? '' : String(S.inspect),
      mode: S.mode,
    });
  };
  const settle = async () => { while (p.S.busy) await new Promise((res) => setImmediate(res)); };
  const click = async (c, r) => { p.onHex(c, r); await settle(); record(`click ${cell(c, r)}`); };
  const over = () => p.S.phase === 'over';
  const awake = () => p.enemies().filter((e) => !e.asleep);

  record('init');
  for (const k of stage.units) { p.S.deployType = k; p.render(); record(`pick ${k}`); }
  for (let r = p.ROWS - 1; r >= 0; r--) for (let c = 0; c < p.COLS; c++) {
    if (p.inDeploy(c, r) && !p.unitAt(c, r)) { await click(c, r); r = -1; break; }
  }
  const a0 = p.allies()[0];
  if (a0) await click(a0.x, a0.y);
  p.startBattle(); record('start');

  while (!over() && p.S.turn <= MAX_TURNS) {
    p.S.showThreat = true; p.render(); record('threat on');
    p.S.showThreat = false; p.render(); record('threat off');
    for (const e of p.enemies().slice(0, 2)) await click(e.x, e.y);
    for (const co of p.S.corpses.slice(0, 1)) await click(co.x, co.y);
    for (const a of [...p.allies()]) {
      if (over() || !p.S.units.includes(a) || a.acted) continue;
      await click(a.x, a.y);
      const others = p.allies().filter((o) => o !== a && p.hd(a, o) <= a.rng);
      if (others.length && !a.acted) {
        p.S.mode = 'execute'; p.render(); record('exec');
        await click(a.x, a.y);
        await click(a.x, a.y);
      }
      const inRange = () => awake().filter((e) => p.hd(a, e) <= a.rng);
      if (inRange().length) { const t = inRange().reduce((b, x) => (x.hp < b.hp ? x : b)); await click(t.x, t.y); continue; }
      const goals = awake().length ? awake() : p.enemies();
      if (!goals.length) continue;
      const goal = goals.reduce((b, t) => (p.hd(a, t) < p.hd(a, b) ? t : b));
      let best = null, bd = p.hd(a, goal);
      for (const [k] of p.reachable(a)) {
        const [c, r] = k.split(',').map(Number);
        if (c === a.x && r === a.y) continue;
        const d = p.hd({ x: c, y: r }, goal);
        if (d < bd) { bd = d; best = [c, r]; }
      }
      if (best) await click(best[0], best[1]);
      if (!over() && p.S.units.includes(a) && inRange().length) { const t = inRange().reduce((b, x) => (x.hp < b.hp ? x : b)); await click(t.x, t.y); }
    }
    if (over()) break;
    for (let r = 0; r < p.ROWS; r++) for (let c = 0; c < p.COLS; c++) {
      if (p.walkable(c, r) && !p.unitAt(c, r) && !p.corpseAt(c, r)) { await click(c, r); r = p.ROWS; break; }
    }
    const a1 = p.allies()[0];
    if (a1) await click(a1.x, a1.y);
    p.S.selected = null; p.S.inspect = null; p.S.mode = 'act'; p.render(); record('escape');
    await p.enemyPhase(); record('endTurn');
  }
  return { stage: stage.id, upgrades, steps };
}

export async function extractOverlays(load) {
  const out = [];
  const p0 = load();
  const allUps = Object.values(p0.UPGRADES).flat().map((u) => u.id);
  for (const st of p0.STAGES) {
    const p = load();
    out.push(await runStage(p, p.STAGES.find((s) => s.id === st.id), []));
  }
  for (const id of ['s4', 's9']) {
    const p = load();
    out.push(await runStage(p, p.STAGES.find((s) => s.id === id), allUps));
  }
  return out;
}
