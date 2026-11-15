# Prints the stage table for docs/content/stages.md 8 from data.json and a matrix result (scratch/<tag>.tsv).
#   py -3 tools/stage/doc.py final > table.md
import json, os, sys, collections
T = os.path.dirname(os.path.abspath(__file__))
d = json.load(open(os.path.join(T, 'data.json'), encoding='utf8'))
TITLE = {'s1': '잿빛 방앗간', 's2': '물레방아 다리', 's3': '무너진 예배당', 's4': '갈림길 묘지', 's5': '횃불 고개', 's6': '버려진 장원', 's7': '국경 망루',
         's8': '성 아래 마을', 's9': '버려진 은광', 's10': '성문 앞', 's11': '해자 다리', 's12': '성벽 위', 's13': '안뜰', 's14': '지하 감옥', 's15': '예배실',
         's16': '대회랑', 's17': '종탑', 's18': '무도회장', 's19': '왕좌로 가는 계단', 's20': '백작의 왕좌'}
EN = {'skeleton': '스켈레톤', 'skeletonArcher': '궁수', 'skeletonHeavy': '중갑병', 'ghoul': '구울', 'skeletonCaptain': '대장', 'gargoyle': '가고일', 'deathKnight': '검은 기사', 'spawn': '스폰'}
GOAL = {0: '보스 처치', 1: '방어전 {h}턴', 2: '호위'}
BON = {0: '{v}턴 안', 1: '흡혈 0회', 2: '모든 하수인 처치', 3: '아군 손실 {v} 이하'}
res = collections.defaultdict(dict)
tag = sys.argv[1] if len(sys.argv) > 1 else 'final'
p = os.path.join(T, tag + '.tsv') if os.path.exists(os.path.join(T, tag + '.tsv')) else os.path.join(T, 'scratch', tag + '.tsv')
if os.path.exists(p):
    for l in open(p, encoding='utf8'):
        c = l.rstrip('\n').split('\t')
        if len(c) < 6: continue
        res[c[1]][c[0]] = (c[2], c[3].replace('win ', ''), c[4].replace('lost ', ''))
out = []
out.append('| 장 | 제목 | 크기 | 목표 | 인구 | 보스 (체력/공격/방어·흡혈 회복) | 하수인 (처음 + 증원) | 추가 목표 |')
out.append('|---|---|---|---|---|---|---|---|')
for s in d['Stages']:
    if s['Id'] == 'demo': continue
    n = s['Id'][1:]
    es = [e for e in s['Enemies'] if e['Kind'] != 'vampire']
    def cnt(xs):
        c = collections.Counter(EN[e['Kind']] for e in xs)
        return ' '.join(f'{k} {v}' for k, v in c.items()) or '-'
    first = [e for e in es if not e['Arrive'] and not e['Wave']]; later = [e for e in es if e['Arrive'] or e['Wave']]
    has_v = any(e['Kind'] == 'vampire' for e in s['Enemies'])
    b = s['Boss']
    boss = f'{b["Hp"]}/{b["Atk"]}/{b["Def"]}·{b["Heal"]}' if has_v else '없음'
    out.append(f'| {n}장 | {TITLE[s["Id"]]} | {len(s["Map"][0])}×{len(s["Map"])} | {GOAL[s["Goal"]].format(h=s["Hold"])} | {s["Pop"]} | {boss} | {cnt(first)} + {cnt(later)} | {BON[s["Bonus"]["Type"]].format(v=s["Bonus"]["Value"])} |')
out.append('')
out.append('한 수 앞 봇(`tools/stage/sp mc`, 4판·턴마다 100가지 수)이 편성별로 이긴 판 수 / 이긴 판의 평균 손실:')
out.append('')
out.append('| 장 | 예시 편성 | 검사 쌓기 | 궁수 쌓기 | 기사 쌓기 | 연금술사 둘 + 섞기 | 고르게 섞기 |')
out.append('|---|---|---|---|---|---|---|')
for s in d['Stages']:
    if s['Id'] == 'demo': continue
    r = res.get(s['Id'], {})
    cell = lambda k: f'{r[k][1]} · {r[k][2]} ({r[k][0]})' if k in r else '-'
    out.append(f'| {s["Id"][1:]}장 | ' + ' | '.join(cell(f'c{i}') for i in range(1, 7)) + ' |')
sys.stdout.reconfigure(encoding='utf8')
print('\n'.join(out))
