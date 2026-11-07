// Checks, without Unity, that every paid asset this repo references is in Assets/Synty with the GUID the repo expects.
// node tools/assets/check.mjs [package dir]   exit 0 = ready, 1 = something to import (the needed packages are printed).
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const HERE = path.join(ROOT, 'tools', 'assets');
const pkgDir = path.resolve(process.argv[2] || process.env.SYNTY_PACKAGES || path.join(ROOT, 'UPackage'));
const tsv = (f) => fs.readFileSync(path.join(HERE, f), 'utf8').trim().split('\n').slice(1).map((l) => l.split('\t'));

const packages = tsv('packages.tsv').map(([file, bytes, required]) => ({ file, bytes: +bytes, required: +required }));
const byPkg = new Map(packages.map((p) => [p.file, { ...p, missing: [], ok: 0 }]));
for (const [guid, rel, pkg] of tsv('required.tsv')) {
  const meta = path.join(ROOT, rel + '.meta');
  const found = fs.existsSync(meta) ? (fs.readFileSync(meta, 'utf8').match(/^guid: ([0-9a-f]{32})/m) || [])[1] : null;
  // scan.mjs writes "A+B" for an asset shipped in both packages and counts it under each.
  for (const entry of pkg.split('+').map((p) => byPkg.get(p))) {
    if (found === guid) entry.ok++;
    else entry.missing.push(found ? `${rel} (GUID ${found}, 기대 ${guid})` : rel);
  }
}

let todo = 0;
for (const p of byPkg.values()) {
  if (!p.required) { console.log(`선택  ${p.file} (지금 게임이 쓰지 않음)`); continue; }
  const file = path.join(pkgDir, p.file);
  const have = fs.existsSync(file) ? fs.statSync(file).size : -1;
  const pkgNote = have < 0 ? '패키지 파일 없음' : have !== p.bytes ? `패키지 크기 ${have} (기록 ${p.bytes}, 다른 판일 수 있음)` : '패키지 있음';
  if (p.missing.length === 0) { console.log(`준비됨 ${p.file} ${p.ok}/${p.required}`); continue; }
  todo++;
  console.log(`필요  ${p.file} ${p.ok}/${p.required}, ${pkgNote}`);
  for (const m of p.missing) console.log(`        빠짐 ${m}`);
}
console.log(todo ? `임포트할 패키지 ${todo}개. tools/assets/setup.sh 를 돌린다 (docs/guides/asset-setup.md).` : '유료 에셋 준비됨.');
process.exit(todo ? 1 : 0);
