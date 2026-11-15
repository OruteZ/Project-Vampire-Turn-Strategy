# analyze.py <tag>...: reads scratch/<tag>.tsv written by loop.sh and prints which unit kinds and upgrades win more than the rest.
# Comparisons are made inside each stage and then averaged, because early stages are easy and only field peasants.
import sys, os, re, collections, json
T = os.path.dirname(os.path.abspath(__file__))
d = json.load(open(os.path.join(T, 'data.json'), encoding='utf8'))
POP = {u['Key']: u['Pop'] for u in d['Types']}
UPUNIT = {u['Id']: u['Unit'] for u in d['Upgrades']}
STUNITS = {s['Id']: s['Units'] for s in d['Stages']}
rows = []
for tag in sys.argv[1:]:
    for l in open(os.path.join(T, 'scratch', tag + '.tsv'), encoding='utf8'):
        c = l.rstrip('\n').split('\t')
        if len(c) < 11: continue
        comp = {m.group(1): int(m.group(2)) for m in re.finditer(r'([a-zA-Z]+?)(\d+)(?:\+|$)', c[1])}
        rows.append(dict(stage=c[0], comp=comp, ups=[x for x in c[2].split(',') if x], won=int(c[4]), lost=int(c[5]), turns=int(c[6]), b=int(c[7]), h=int(c[8]), t=int(c[9]), x=int(c[10]), raw=c[1]))
def rate(rs): return sum(r['won'] for r in rs) / len(rs) if rs else float('nan')
by = collections.defaultdict(list)
for r in rows: by[r['stage']].append(r)
def strat(grp):
    tot = w = na = nb = 0
    for s in by:
        A = [r for r in by[s] if grp(r) == 'a']; B = [r for r in by[s] if grp(r) == 'b']
        if len(A) < 2 or len(B) < 2: continue
        k = min(len(A), len(B)); tot += k * (rate(A) - rate(B)); w += k; na += len(A); nb += len(B)
    return (tot / w if w else float('nan')), na, nb
def share(r, k): return r['comp'].get(k, 0) * POP[k] / max(1, sum(n * POP[x] for x, n in r['comp'].items()))

print(f'games {len(rows)}  overall win {rate(rows):.2f}')
print('\nstage: wins, average loss in wins')
for s in sorted(by, key=lambda x: int(x[1:])):
    w = [r for r in by[s] if r['won']]
    print(f'  {s:4} {len(w):2}/{len(by[s]):2}  lost {sum(r["lost"] for r in w) / len(w) if w else float("nan"):.1f}')

print('\nunit: win-rate difference against builds without it (40%+ of the population / less than 40%), games on each side')
for k in POP:
    h = strat(lambda r: 'a' if share(r, k) >= 0.4 else 'b' if k not in r['comp'] and k in STUNITS[r['stage']] else None)
    m = strat(lambda r: 'a' if 0 < share(r, k) < 0.4 else 'b' if k not in r['comp'] and k in STUNITS[r['stage']] else None)
    print(f'  {k:12} heavy {h[0]:+.2f} ({h[1]:3} vs {h[2]:3})   some {m[0]:+.2f} ({m[1]:3} vs {m[2]:3})')

print('\nupgrade: win-rate difference against the same unit kind fielded with no upgrade')
for u in UPUNIT:
    k = UPUNIT[u]
    g = strat(lambda r: 'a' if u in r['ups'] else 'b' if k in r['comp'] and not any(UPUNIT[x] == k for x in r['ups']) else None)
    if g[1] >= 6: print(f'  {u:20} {g[0]:+.2f} ({g[1]:3} vs {g[2]:3})')

print('\npairs: two kinds both at 25%+ of the population, against builds with neither')
kinds = list(POP)
out = []
for i, a in enumerate(kinds):
    for b in kinds[i + 1:]:
        g = strat(lambda r: 'a' if share(r, a) >= 0.25 and share(r, b) >= 0.25 else 'b' if a not in r['comp'] and b not in r['comp'] and a in STUNITS[r['stage']] and b in STUNITS[r['stage']] else None)
        if g[1] >= 8: out.append((g[0], a, b, g[1], g[2]))
for g in sorted(out, reverse=True)[:8]: print(f'  {g[1]}+{g[2]:12} {g[0]:+.2f} ({g[3]} vs {g[4]})')

zero = [r for r in rows if r['won'] and r['lost'] == 0]
print(f'\nwins with no loss: {len(zero)}')
c = collections.Counter(k for r in zero for k in r['comp'])
print('  kinds in them:', ', '.join(f'{k} {n}' for k, n in c.most_common()))
print('  by stage:', ', '.join(f'{s} {n}' for s, n in sorted(collections.Counter(r['stage'] for r in zero).items(), key=lambda x: int(x[0][1:]))))
