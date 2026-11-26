# tension.py <tag>...: per stage from loop.sh output (tools/stage/<tag>.tsv or scratch/<tag>.tsv):
# games, win rate, share of games with a spawn, win rate with / without a spawn, average turns in wins, idle turns and idle / all turns.
import sys, os, collections
T = os.path.dirname(os.path.abspath(__file__))
rows = []
for tag in sys.argv[1:]:
    p = os.path.join(T, tag + '.tsv')
    for l in open(p if os.path.exists(p) else os.path.join(T, 'scratch', tag + '.tsv'), encoding='utf8'):
        c = l.rstrip('\r\n').split('\t')
        if len(c) >= 14: rows.append((c[0], int(c[4]), int(c[6]), int(c[12]), int(c[13])))
def rate(rs): return f'{sum(r[1] for r in rs) / len(rs):.2f}' if rs else '-'
def line(name, rs):
    sp = [r for r in rs if r[4]]; no = [r for r in rs if not r[4]]; w = [r for r in rs if r[1]]
    turns = sum(r[2] for r in rs)
    return (f'| {name} | {len(rs)} | {rate(rs)} | {len(sp) / len(rs):.2f} | {rate(sp)} ({len(sp)}) | {rate(no)} ({len(no)}) | '
            f'{(sum(r[2] for r in w) / len(w)) if w else float("nan"):.1f} | {sum(r[3] for r in rs) / len(rs):.1f} | {sum(r[3] for r in rs) / max(1, turns):.2f} |')
by = collections.defaultdict(list)
for r in rows: by[r[0]].append(r)
sys.stdout.reconfigure(encoding='utf8')
print('| 장 | 판 | 승률 | 스폰 판 비율 | 스폰 판 승률 (판) | 스폰 없는 판 승률 (판) | 이긴 판 평균 턴 | 평균 대기 턴 | 대기 / 전체 턴 |')
print('|---|---|---|---|---|---|---|---|---|')
for s in sorted(by, key=lambda x: int(x[1:])): print(line(s, by[s]))
print(line('전체', rows))
