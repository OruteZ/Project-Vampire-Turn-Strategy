const campIds = (p) => p.campStages().map((s) => s.id);

function view(p) {
  return {
    save: JSON.stringify(p.C),
    open: p.campStages().map((_, i) => (p.stageOpen(i) ? 1 : 0)).join(''),
    upgradesOpen: Object.keys(p.TYPES).filter((k) => p.upgradesOpen(k)).join(','),
    tier: p.unlockedTier(),
    spent: p.spentGold(),
  };
}

// Mirrors the campaign screen's click handler (docs/Prototype.html, $('camp') listener). The DOM is stubbed, so the
// handler itself never runs in node.
function buy(p, id) {
  const up = Object.values(p.UPGRADES).flat().find((u) => u.id === id);
  const uk = Object.keys(p.UPGRADES).find((k) => p.UPGRADES[k].includes(up));
  if (up && p.upgradesOpen(uk) && p.C.gold >= up.cost && !p.C.ups[up.id]) { p.C.gold -= up.cost; p.C.ups[up.id] = true; p.saveCamp(); }
}
function reset(p) { p.C = { gold: 0, cleared: {}, bonus: {}, ups: {} }; p.saveCamp(); }

function finish(p, stageId, { win, turn, drained, skeletonsLeft }) {
  p.M = 'camp';
  p.newBattle(p.STAGES.find((s) => s.id === stageId));
  p.S.turn = turn;
  p.S.stats.drained = drained;
  if (!skeletonsLeft) p.S.units = p.S.units.filter((u) => u.kind !== 'skeleton');
  const bonusMet = p.bonusMet(p.S.stage.bonus);
  p.finish(win);
  return bonusMet;
}

export function extractCampaign(load) {
  const p = load();
  let store = null;
  p.localStorage.getItem = () => store;
  p.localStorage.setItem = (_, v) => { store = v; };
  p.C = p.loadCamp();
  const steps = [];
  const record = (op, extra = {}) => steps.push({ op: JSON.stringify(op), ...extra, ...view(p) });
  record({ op: 'load' });

  const ids = campIds(p);
  const play = (stage, o) => { const met = finish(p, stage, o); record({ op: 'finish', stage, ...o }, { bonusMet: met }); };
  play('s1', { win: false, turn: 4, drained: 0, skeletonsLeft: true });
  play('s1', { win: true, turn: 10, drained: 0, skeletonsLeft: true });
  play('s1', { win: true, turn: 9, drained: 0, skeletonsLeft: true });
  play('s1', { win: true, turn: 3, drained: 0, skeletonsLeft: true });
  for (const [op, id] of [['buy', 'sword_def'], ['buy', 'pspear_hp'], ['buy', 'pspear_pike'], ['buy', 'pspear_hp'], ['buy', 'parcher_mov']]) {
    buy(p, id); record({ op, id });
  }
  play('s2', { win: true, turn: 5, drained: 1, skeletonsLeft: true });
  play('s2', { win: true, turn: 5, drained: 0, skeletonsLeft: true });
  for (let i = 2; i < ids.length; i++) {
    const s = p.STAGES.find((x) => x.id === ids[i]);
    play(ids[i], { win: true, turn: i, drained: i % 2, skeletonsLeft: i % 3 === 0 });
    for (const up of Object.values(p.UPGRADES).flat()) { buy(p, up.id); record({ op: 'buy', id: up.id }); }
    if (s.bonus.type === 'turns') { play(ids[i], { win: true, turn: s.bonus.val + 1, drained: 0, skeletonsLeft: true }); }
  }
  reset(p); record({ op: 'reset' });

  const loads = ['', 'not json', '{"gold":"12"}', '{"gold":35,"cleared":{"s1":true},"bonus":{},"ups":{"pspear_hp":true}}', 'null'].map((raw) => {
    store = raw === '' ? null : raw;
    return { raw, loaded: JSON.stringify(p.loadCamp()) };
  });

  const bonus = [];
  for (const s of p.STAGES) for (const turn of [s.bonus.val, s.bonus.val + 1]) for (const drained of [0, 1]) for (const skeletonsLeft of [true, false]) {
    bonus.push({ stage: s.id, turn, drained, skeletonsLeft, met: finish(p, s.id, { win: false, turn, drained, skeletonsLeft }) });
  }
  return { steps, loads, bonus };
}
