// UserPromptSubmit: a pstack slash command gets the repo's override skill named before the plugin's own text runs.
let input = '';
process.stdin.on('data', d => (input += d)).on('end', () => {
  if (!/\/pstack:|poteto-mode/.test(JSON.parse(input).prompt ?? '')) return;
  const additionalContext = 'pstack 을 쓰는 요청이다. pstack 의 Claude Code 어댑터 다음, 플레이북 단계 전에 .claude/skills/pstack-unity/SKILL.md 를 읽고 검증·PR·커밋·브랜치는 그 표대로 바꿔 따른다.';
  process.stdout.write(JSON.stringify({ hookSpecificOutput: { hookEventName: 'UserPromptSubmit', additionalContext } }));
});
