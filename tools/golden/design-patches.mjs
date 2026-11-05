// The goldens are the HTML demo plus these changes, each required by a 기획서 that follows docs/design.
// A patch whose text is no longer in the HTML stops the run, so a changed demo cannot silently drop a rule.
export const PATCHES = [
  {
    why: 'docs/systems/spawn.md 9: 스폰 방어 0',
    find: 'atk:co.atk,def:co.def,mov:co.mov',
    replace: 'atk:co.atk,def:0,mov:co.mov',
  },
  {
    why: 'docs/systems/spawn.md 12: 시신 카드',
    find: '체력 ${o.maxHp}, 공격 ${o.atk}, 방어 ${o.def}, 이동 ${o.mov}를 물려받고 근접 공격만 합니다.',
    replace: '체력 ${o.maxHp}, 공격 ${o.atk}, 이동 ${o.mov}을 물려받고 방어 0, 근접 공격만 합니다.',
  },
  {
    why: 'docs/systems/spawn.md 12: 스폰 카드',
    find: '${o.origin}의 체력·공격·방어·이동을 물려받았다. 근접 공격만 한다. 죽인 아군을 스폰으로 만든다.',
    replace: '${o.origin}의 체력·공격·이동을 물려받았다. 방어는 0 이고 근접 공격만 한다. 죽인 아군을 스폰으로 만든다.',
  },
  {
    why: 'docs/systems/spawn.md 12: 규칙 요약',
    find: '스폰은 원본의 체력·공격·방어·이동을 물려받고 근접 공격만 합니다. 기술 강화는 물려받지 않습니다.',
    replace: '스폰은 원본의 체력·공격·이동을 물려받고, 방어는 0 이며 근접 공격만 합니다. 기술 강화는 물려받지 않습니다.',
  },
  {
    why: 'docs/systems/progression.md 15: 방어 강화는 스폰이 물려받지 않음',
    find: "{id:'sword_def',name:'사슬 갑옷',desc:'방어 +1',cost:60,inherit:true,",
    replace: "{id:'sword_def',name:'사슬 갑옷',desc:'방어 +1',cost:60,inherit:false,",
  },
  {
    why: 'docs/systems/progression.md 15: 방어 강화는 스폰이 물려받지 않음',
    find: "{id:'knight_def',name:'판금 갑옷',desc:'방어 +1',cost:80,inherit:true,",
    replace: "{id:'knight_def',name:'판금 갑옷',desc:'방어 +1',cost:80,inherit:false,",
  },
  {
    why: 'docs/ui/upgrade.md 3: 강화 초기화 없음',
    find: /\$\{spentGold\(\)\?`<div class="acts" style="margin:0 0 8px"><button class="btn \$\{respecArm[\s\S]*?<\/div>`:''\}/,
    replace: '',
  },
  {
    why: 'docs/ui/upgrade.md 2: 안내 문장',
    find: '강화는 같은 병종 전체에 적용됩니다. 유닛이 해금된 장을 클리어한 뒤부터 그 유닛을 강화할 수 있습니다. 능력치 강화는 스폰이 되었을 때 그대로 물려받고, 기술 강화는 물려받지 않습니다.',
    replace: '강화는 같은 병종 전체에 적용되고, 한 번 사면 되돌릴 수 없습니다. 유닛이 해금된 장을 클리어한 뒤부터 그 유닛을 강화할 수 있습니다. 스폰이 되면 체력·공격·이동 강화만 물려받습니다.',
  },
  {
    why: 'docs/ui/main-menu.md 1, 2, 4: 가제 제목, 정체성 문장·프로토타입 표시 없음',
    find: '<div class="eyebrow">PROJECT VAMPIRE SPAWN · 프로토타입</div>\n    <h1>한 명이 물리면<br>전열이 <em>무너진다</em></h1>',
    replace: '<h1>PROJECT VAMPIRE SPAWN</h1>',
  },
];

export function applyDesignPatches(html) {
  let out = html.replace(/\r\n/g, '\n');
  for (const p of PATCHES) {
    const hit = typeof p.find === 'string' ? out.includes(p.find) : p.find.test(out);
    if (!hit) throw new Error(`design patch no longer matches the HTML: ${p.why}`);
    out = out.replace(p.find, p.replace);
  }
  return out;
}
