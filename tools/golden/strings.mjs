import fs from 'node:fs';
import { commands } from './battle.mjs';
import { recorder } from './overlay.mjs';

const VOID = new Set(['br', 'input', 'img', 'meta', 'link']);
const INLINE = new Set(['b', 'em', 'i', 'small', 'br', 'svg', 'strong']);
const decode = (s) => s.replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&quot;/g, '"').replace(/&amp;/g, '&');

export function parse(html) {
  const root = { tag: '#root', kids: [] };
  const stack = [root];
  for (const m of html.matchAll(/<!--[\s\S]*?-->|<(\/?)([a-zA-Z][\w-]*)([^>]*?)(\/?)>|([^<]+)/g)) {
    const top = stack[stack.length - 1];
    if (m[5] !== undefined) { top.kids.push({ text: decode(m[5]) }); continue; }
    if (!m[2]) continue;
    const tag = m[2].toLowerCase();
    if (m[1]) {
      const i = stack.map((n) => n.tag).lastIndexOf(tag);
      if (i > 0) stack.length = i;
      continue;
    }
    const node = { tag, attrs: m[3], kids: [] };
    top.kids.push(node);
    if (!m[4] && !VOID.has(tag)) stack.push(node);
  }
  return root;
}

const textOf = (n) => (n.text !== undefined ? n.text : n.tag === 'svg' ? '' : n.kids.map(textOf).join(''));
const squash = (s) => s.replace(/\s+/g, '');

// A block is an element whose children are only text and inline tags; each block becomes one Text in Unity.
export function blocks(n, out = []) {
  if (n.text !== undefined) { if (squash(n.text)) out.push(squash(n.text)); return out; }
  if (n.tag === 'svg') return out;
  const flat = n.kids.every((k) => k.text !== undefined || INLINE.has(k.tag));
  if (flat && n.tag !== '#root') { const t = squash(textOf(n)); if (t) out.push(t); return out; }
  for (const k of n.kids) blocks(k, out);
  return out;
}

const html = (s) => blocks(parse(s || ''));

function menuBlocks(p, els, file) {
  const src = fs.readFileSync(file, 'utf8');
  const menu = src.slice(src.indexOf('<section id="menu"'), src.indexOf('<section id="camp"'));
  p.renderMenu();
  return html(menu.replace('<span class="meta" id="campMeta"></span>', `<span class="meta">${p.$('campMeta').textContent}</span>`));
}

function battleBlocks(p, els) {
  const head = `<div class="sub">${p.$('bSub').textContent}</div><h1>${p.$('bTitle').innerHTML}</h1>`
    + `<div>${p.$('stats').innerHTML}</div><label> 적 위협 범위</label><button>${p.$('leave').textContent || '나가기'}</button><button>${p.$('primary').textContent}</button>`;
  return [...html(head), ...html(p.$('side').innerHTML), ...html(`<p>${p.$('hint').textContent || p.$('hint').innerHTML}</p>`)];
}

const campOf = (C) => JSON.stringify(C);

export async function extractStrings(load, file) {
  const out = [];
  const fresh = () => ({ gold: 0, cleared: {}, bonus: {}, ups: {} });
  const progress = () => ({ gold: 500, cleared: { s1: true, s2: true, s3: true, s4: true }, bonus: { s1: true, s3: true }, ups: { pspear_hp: true, sword_cleave: true } });
  const setup = (C) => { const p = load(); const els = recorder(p); p.C = C; p.uid = 1; return { p, els }; };

  for (const [name, C] of [['menu-fresh', fresh()], ['menu-progress', progress()]]) {
    const { p, els } = setup(C);
    out.push({ name, screen: 'menu', camp: campOf(C), blocks: menuBlocks(p, els, file) });
  }
  for (const [name, C, arm] of [['camp-fresh', fresh(), false], ['camp-progress', progress(), false], ['camp-armed', progress(), true]]) {
    const { p, els } = setup(C);
    p.M = 'camp';
    p.resetArm = arm; p.respecArm = arm;
    p.renderCamp();
    out.push({ name, screen: 'camp', camp: campOf(C), arm, blocks: html(p.$('camp').innerHTML) });
  }

  {
    const { p, els } = setup(fresh());
    p.M = 'demo';
    p.newBattle(p.STAGES[0]);
    const ops = [];
    const shot = (name) => out.push({ name, screen: 'battle', stage: 'demo', camp: campOf(p.C), ops: [...ops], blocks: battleBlocks(p, els) });
    const settle = async () => { while (p.S.busy) await new Promise((r) => setImmediate(r)); };
    const click = async (u) => { ops.push(`click ${u.x},${u.y}`); p.onHex(u.x, u.y); await settle(); };
    shot('demo-deploy');
    for (const k of ['knight', 'hunter']) { ops.push(`pick ${k}`); p.S.deployType = k; p.render(); }
    await click(p.allies()[0]); shot('demo-deploy-inspect');
    ops.push('start'); p.startBattle(); shot('demo-start');
    await click(p.allies()[0]); shot('demo-select');
    ops.push('exec'); p.S.mode = 'execute'; p.render(); shot('demo-exec');
    ops.push('exec'); p.S.mode = 'act'; p.render();
    await click(p.vampire()); shot('demo-vampire');
    await click(p.enemies().find((e) => e.kind === 'skeleton')); shot('demo-skeleton');
    const seen = new Set();
    while (p.S.phase !== 'over' && p.S.turn < 30) {
      ops.push('endTurn'); await p.enemyPhase();
      if (p.S.phase === 'over') break;
      shot(`demo-turn${p.S.turn}`);
      const co = p.S.corpses[0], sp = p.enemies().find((e) => e.kind === 'spawn'), v = p.vampire();
      if (co && !seen.has('corpse')) { seen.add('corpse'); ops.push(`click ${co.x},${co.y}`); p.onHex(co.x, co.y); shot('demo-corpse'); }
      if (sp && !seen.has('spawn')) { seen.add('spawn'); await click(sp); shot('demo-spawn'); }
      if (v && !v.asleep && !seen.has('awake')) { seen.add('awake'); await click(v); shot('demo-vampire-awake'); }
      const t = typeof p.S.eatTarget === 'number' && p.S.units.find((u) => u.id === p.S.eatTarget);
      if (t && !seen.has('target')) { seen.add('target'); await click(t); shot('demo-target'); }
      ops.push('escape'); p.S.selected = null; p.S.inspect = null; p.S.mode = 'act'; p.render();
    }
    shot('demo-over-board');
  }

  const traces = JSON.parse(fs.readFileSync(new URL('./out/battles.json', import.meta.url), 'utf8'));
  for (const [name, trace, C] of [
    ['over-demo-win', 'aggressive/demo', fresh()],
    ['over-demo-lose', 'passive/demo', fresh()],
    ['over-s1-first', 'aggressive/s1', fresh()],
    ['over-s1-again', 'aggressive/s1', { gold: 90, cleared: { s1: true }, bonus: { s1: true }, ups: {} }],
    ['over-s1-lose', 'passive/s1', fresh()],
    ['over-s2-first', 'moonwise/s2', { gold: 40, cleared: { s1: true }, bonus: {}, ups: {} }],
  ]) {
    const tr = traces.find((t) => t.policy + '/' + t.stage === trace);
    const { p, els } = setup(C);
    const before = campOf(C);
    p.M = tr.stage === 'demo' ? 'demo' : 'camp';
    p.newBattle(p.STAGES.find((s) => s.id === tr.stage));
    const run = commands(p);
    for (const s of tr.steps) { const { op, ...args } = JSON.parse(s.cmd); await run[op](args); }
    out.push({ name, screen: 'over', trace, camp: before, campAfter: campOf(p.C), blocks: html(p.$('over').innerHTML) });
    if (p.M === 'camp') { p.renderCamp(); out.push({ name: name + '-camp', screen: 'camp', camp: campOf(p.C), arm: false, blocks: html(p.$('camp').innerHTML) }); }
  }
  return out;
}
