import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';
import { fileURLToPath } from 'node:url';
import { extractBattles } from './battle.mjs';
import { extractCampaign } from './campaign.mjs';
import { extractOverlays } from './overlay.mjs';
import { extractStrings } from './strings.mjs';
import { applyDesignPatches } from './design-patches.mjs';

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const HTML = path.join(ROOT, 'docs', 'Prototype.html');
const OUT = path.join(ROOT, 'tools', 'golden', 'out');
const GAMEDATA_CS = path.join(ROOT, 'Assets', '_Project', 'Scripts', 'Core', 'PrototypeData.cs');

export function loadPrototype() {
  const html = applyDesignPatches(fs.readFileSync(HTML, 'utf8'));
  const src = html.slice(html.indexOf('<script>') + 8, html.lastIndexOf('</script>'));
  const stub = () => new Proxy(function () {}, {
    get: (_, k) => (k === Symbol.toPrimitive ? () => '' : k === 'checked' ? false : k === 'hidden' ? true : stub()),
    set: () => true,
    apply: () => stub(),
  });
  const ctx = {
    console, Math, JSON, Map, Set, Promise, Object, Number, String, Array,
    setTimeout: (f) => setImmediate(f), clearTimeout() {},
    document: new Proxy({}, { get: () => stub() }),
    localStorage: { getItem: () => null, setItem() {} },
    matchMedia: () => ({ matches: true }),
    window: { scrollTo() {} },
  };
  vm.createContext(ctx);
  // Top-level const/let are invisible to the vm context object; var makes them readable.
  vm.runInContext(src.replace(/^(const|let) /gm, 'var '), ctx);
  return ctx;
}

const ABILITIES = ['pike', 'ambush', 'cleave', 'charge'];

export function extractData(p) {
  const s = (o) => ({ name: o.name, glyph: o.glyph, hp: o.hp, atk: o.atk, def: o.def, mov: o.mov, rng: o.rng });
  return {
    constants: { drainHeal: p.DRAIN_HEAL, moonMax: p.MOON_MAX, eatRange: p.EAT_RANGE, moonWake: p.MOON_WAKE },
    types: Object.entries(p.TYPES).map(([key, t]) => ({
      key, name: t.name, glyph: t.glyph, tier: t.tier, pop: t.pop, silver: t.silver,
      hp: t.hp, atk: t.atk, def: t.def, mov: t.mov, rng: t.rng, bane: t.bane ?? 0, desc: t.desc, unlockAt: p.UNLOCK_AT[key],
    })),
    upgrades: Object.entries(p.UPGRADES).flatMap(([unit, ups]) => ups.map((u) => {
      const known = new Set(['hp', 'atk', 'def', 'mov', 'rng', 'bane', 'abil']);
      for (const k of Object.keys(u.mod)) if (!known.has(k)) throw new Error(`unknown upgrade mod ${k} in ${u.id}`);
      if (u.mod.abil && !ABILITIES.includes(u.mod.abil)) throw new Error(`unknown ability ${u.mod.abil}`);
      return {
        unit, id: u.id, name: u.name, desc: u.desc, cost: u.cost, inherit: u.inherit,
        hp: u.mod.hp ?? 0, atk: u.mod.atk ?? 0, def: u.mod.def ?? 0, mov: u.mod.mov ?? 0, rng: u.mod.rng ?? 0, bane: u.mod.bane ?? 0,
        abil: u.mod.abil ?? '',
      };
    })),
    skeleton: s(p.SKEL),
    stages: p.STAGES.map((st) => ({
      id: st.id, label: st.label, title: st.title, units: [...st.units], pop: st.pop, silver: st.silver, reward: st.reward ?? 0,
      bonusType: st.bonus.type, bonusVal: st.bonus.val, bonusGold: st.bonus.gold, bonusText: p.bonusText(st.bonus),
      brief: st.brief,
      boss: { name: st.boss.name, hp: st.boss.hp, atk: st.boss.atk, def: st.boss.def, mov: st.boss.mov, heal: st.boss.heal },
      map: [...st.map],
      enemies: st.enemies.map(([kind, c, r]) => ({ kind, c, r })),
      sample: st.sample.map(([kind, c, r]) => ({ kind, c, r })),
    })),
  };
}

export function extractHex(p) {
  const cell = ([c, r]) => `${c},${r}`;
  const step = ([k, d]) => `${k}:${d}`;
  const pos = (u) => cell([u.x, u.y]);
  const scenario = (st, withCorpse) => {
    p.S = { stage: st, units: [], corpses: [] };
    for (const [k, c, r] of st.enemies) p.S.units.push(p.mkEnemy(k, c, r, st.boss));
    for (const [t, c, r] of st.sample) p.S.units.push(p.mkAlly(t, c, r));
    if (withCorpse) {
      const gone = p.S.units.find((u) => u.side === 'ally');
      p.S.units = p.S.units.filter((u) => u !== gone);
      p.S.corpses.push({ id: 0, x: gone.x, y: gone.y });
    }
    const cells = [];
    for (let r = 0; r < p.ROWS; r++) for (let c = 0; c < p.COLS; c++) cells.push([c, r]);
    const targets = cells.filter(([c, r]) => (c + r) % 3 === 0);
    const field = (side, other) => {
      const goals = [];
      for (const a of p.S.units.filter((u) => u.side === other)) for (const [c, r] of p.nbrs(a.x, a.y)) if (!p.blocked(c, r, side) && !p.unitAt(c, r)) goals.push([c, r]);
      return [...p.distField(goals, side)].sort(([a], [b]) => (a < b ? -1 : 1)).map(step);
    };
    return {
      units: p.S.units.map((u) => ({
        side: u.side, kind: u.kind, x: u.x, y: u.y, mov: u.mov,
        reach: [...p.reachable(u)].map(step),
        paths: targets.map(([c, r]) => ({ to: cell([c, r]), path: p.pathTo(u, c, r).map(cell) })),
      })),
      corpses: p.S.corpses.map(pos),
      fieldEnemy: field('enemy', 'ally'),
      fieldAlly: field('ally', 'enemy'),
    };
  };
  return p.STAGES.map((st) => {
    p.setDims(st);
    p.S = { stage: st, units: [], corpses: [] };
    const cells = [];
    for (let r = 0; r < p.ROWS; r++) for (let c = 0; c < p.COLS; c++) {
      cells.push({ c, r, walkable: p.walkable(c, r), lit: p.lit(c, r), nbrs: p.nbrs(c, r).map(cell), dist00: p.hd({ x: c, y: r }, { x: 0, y: 0 }) });
    }
    return { id: st.id, cols: p.COLS, rows: p.ROWS, cells, base: scenario(st, false), corpse: scenario(st, true) };
  });
}

const pascal = (k) => ({ pspear: 'PSpear', parcher: 'PArcher' })[k] ?? k[0].toUpperCase() + k.slice(1);
const enumName = (v) => v.replace(/(^|[^a-zA-Z])([a-z])/g, (_, a, b) => b.toUpperCase()).replace(/^./, (c) => c.toUpperCase());
const str = (v) => JSON.stringify(v);

export function emitCSharp(d) {
  const L = [];
  L.push('// Generated by `node tools/golden/prototype-golden.mjs csharp` from docs/Prototype.html. Do not edit.');
  L.push('namespace VampireSpawn.Core', '{');
  L.push(`    public enum UnitKind { ${d.types.map((t) => pascal(t.key)).join(', ')} }`, '');
  L.push('    public static class PrototypeData', '    {');
  const k0 = d.constants;
  L.push(`        public static DataSet Create() => new DataSet(new RuleSet(${k0.drainHeal}, ${k0.moonMax}, ${k0.eatRange}, ${k0.moonWake}), Types(), Upgrades(), Skeleton(), Stages());`, '');
  L.push('        static UnitType[] Types() => new[]', '        {');
  for (const t of d.types) {
    L.push(`            new UnitType(UnitKind.${pascal(t.key)}, ${str(t.key)}, Tier.${enumName(t.tier)}, ${t.pop}, ${t.silver}, ${t.hp}, ${t.atk}, ${t.def}, ${t.mov}, ${t.rng}, ${t.bane}, ${t.unlockAt}),`);
  }
  L.push('        };', '');
  L.push('        static Upgrade[] Upgrades() => new[]', '        {');
  for (const u of d.upgrades) {
    const abil = u.abil ? `Ability.${enumName(u.abil)}` : 'Ability.None';
    L.push(`            new Upgrade(UnitKind.${pascal(u.unit)}, ${str(u.id)}, ${u.cost}, new StatMod(${u.hp}, ${u.atk}, ${u.def}, ${u.mov}, ${u.rng}, ${u.bane}), ${abil}),`);
  }
  L.push('        };', '');
  const k = d.skeleton;
  L.push(`        static EnemyStats Skeleton() => new EnemyStats(${k.hp}, ${k.atk}, ${k.def}, ${k.mov}, ${k.rng});`, '');
  L.push('        static Stage[] Stages() => new[]', '        {');
  for (const s of d.stages) {
    const b = s.boss;
    L.push('            new Stage(');
    L.push(`                ${str(s.id)},`);
    L.push(`                new[] { ${s.units.map((u) => `UnitKind.${pascal(u)}`).join(', ')} },`);
    L.push(`                ${s.pop}, ${s.silver}, ${s.reward}, new Bonus(BonusType.${enumName(s.bonusType)}, ${s.bonusVal}, ${s.bonusGold}),`);
    L.push(`                new Boss(${b.hp}, ${b.atk}, ${b.def}, ${b.mov}, ${b.heal}),`);
    L.push(`                new[] { ${s.map.map(str).join(', ')} },`);
    L.push(`                new[] { ${s.enemies.map((e) => `new Placement<EnemyKind>(EnemyKind.${enumName(e.kind)}, ${e.c}, ${e.r})`).join(', ')} },`);
    L.push(`                new[] { ${s.sample.map((e) => `new Placement<UnitKind>(UnitKind.${pascal(e.kind)}, ${e.c}, ${e.r})`).join(', ')} }),`);
  }
  L.push('        };', '    }', '}', '');
  return L.join('\n');
}

const write = (file, text) => {
  fs.mkdirSync(path.dirname(file), { recursive: true });
  fs.writeFileSync(file, text);
  console.log(`wrote ${path.relative(ROOT, file).replaceAll('\\', '/')}`);
};

const cmd = process.argv[2];
if (cmd === 'data') {
  write(path.join(OUT, 'data.json'), JSON.stringify(extractData(loadPrototype()), null, 1) + '\n');
} else if (cmd === 'csharp') {
  write(GAMEDATA_CS, emitCSharp(JSON.parse(fs.readFileSync(path.join(OUT, 'data.json'), 'utf8'))));
} else if (cmd === 'hex') {
  write(path.join(OUT, 'hex.json'), JSON.stringify(extractHex(loadPrototype())) + '\n');
} else if (cmd === 'battle') {
  write(path.join(OUT, 'battles.json'), JSON.stringify(await extractBattles(loadPrototype)) + '\n');
} else if (cmd === 'campaign') {
  write(path.join(OUT, 'campaign.json'), JSON.stringify(extractCampaign(loadPrototype), null, 1) + String.fromCharCode(10));
} else if (cmd === 'overlay') {
  write(path.join(OUT, 'overlay.json'), JSON.stringify(await extractOverlays(loadPrototype)) + String.fromCharCode(10));
} else if (cmd === 'strings') {
  write(path.join(OUT, 'strings.json'), JSON.stringify(await extractStrings(loadPrototype, HTML), null, 1) + String.fromCharCode(10));
} else if (cmd) {
  console.error('usage: node tools/golden/prototype-golden.mjs data|csharp|hex|battle|campaign|overlay|strings');
  process.exit(2);
}
