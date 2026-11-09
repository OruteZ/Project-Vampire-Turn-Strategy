// Maintainer tool: lists the paid-asset GUIDs this repo references and which .unitypackage holds each.
// Run after adding or changing art references: node tools/assets/scan.mjs (needs Assets/Synty imported and UPackage/ present).
import fs from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const OUT = path.join(ROOT, 'tools', 'assets');
const git = (...a) => execFileSync('git', ['-c', 'core.quotepath=off', ...a], { cwd: ROOT, encoding: 'utf8', maxBuffer: 1 << 28 }).split('\n').filter(Boolean);
const guidOf = (meta) => (fs.readFileSync(meta, 'utf8').match(/^guid: ([0-9a-f]{32})/m) || [])[1];

const tracked = git('ls-files', 'Assets', 'ProjectSettings');
const own = new Set(tracked.filter((f) => f.endsWith('.meta')).map((f) => guidOf(path.join(ROOT, f))));
const refs = new Map();
for (const f of tracked) {
  if (/\.(meta|cs|asmdef|png|shader|json|inputactions|md)$/.test(f)) continue;
  const text = fs.readFileSync(path.join(ROOT, f), 'utf8');
  for (const m of text.matchAll(/guid: ([0-9a-f]{32})/g)) {
    if (own.has(m[1])) continue;
    if (!refs.has(m[1])) refs.set(m[1], new Set());
    refs.get(m[1]).add(f);
  }
}

const local = new Map();
const walk = (d) => {
  for (const e of fs.readdirSync(d, { withFileTypes: true })) {
    const p = path.join(d, e.name);
    if (e.isDirectory()) walk(p);
    else if (e.name.endsWith('.meta')) { const g = guidOf(p); if (refs.has(g)) local.set(g, path.relative(ROOT, p).slice(0, -5).split(path.sep).join('/')); }
  }
};
walk(path.join(ROOT, 'Assets', 'Synty'));

const pkgDir = path.join(ROOT, 'UPackage');
const packages = fs.readdirSync(pkgDir).filter((f) => f.endsWith('.unitypackage')).sort();
const inPkg = new Map();
for (const p of packages) {
  const list = execFileSync('tar', ['-tzf', p], { cwd: pkgDir, encoding: 'utf8', maxBuffer: 1 << 28 });
  for (const line of list.split('\n')) {
    const g = line.split('/')[0];
    if (local.has(g)) { if (!inPkg.has(g)) inPkg.set(g, []); if (!inPkg.get(g).includes(p)) inPkg.get(g).push(p); }
  }
  console.log('listed', p);
}

const rows = [...local].sort((a, b) => a[1].localeCompare(b[1])).map(([g, p]) => [g, p, (inPkg.get(g) || ['?']).join('+'), [...refs.get(g)].join(',')]);
fs.writeFileSync(path.join(OUT, 'required.tsv'), ['guid\tpath\tpackage\tused_by', ...rows.map((r) => r.join('\t'))].join('\n') + '\n');
const sizes = packages.map((p) => [p, fs.statSync(path.join(pkgDir, p)).size, rows.filter((r) => r[2].split('+').includes(p)).length]);
fs.writeFileSync(path.join(OUT, 'packages.tsv'), ['file\tbytes\trequired_assets', ...sizes.map((s) => s.join('\t'))].join('\n') + '\n');
const unresolved = [...refs.keys()].filter((g) => !local.has(g));
console.log(`required ${rows.length}, packages ${packages.length}, not in Assets/Synty ${unresolved.length} (Unity built-ins and packages)`);
for (const r of rows.filter((r) => r[2] === '?' || r[2].includes('+'))) console.log('check', r.join(' '));
