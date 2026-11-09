// PreToolUse: Unity editors and batchmode start only through vu.sh, which holds the machine-wide editor slots (CLAUDE.md, max 2).
let input = '';
process.stdin.on('data', d => (input += d)).on('end', () => {
  const cmd = JSON.parse(input).tool_input?.command ?? '';
  if (/vu\.sh/.test(cmd) || /^\s*git\s/.test(cmd)) return;
  const cliLaunch = /(^|[\s;&|(`])unity(\.exe)?\s+(--?[\w-]+\s+)*(open|test|build)\b/i.test(cmd);
  const rawLaunch = /-batchmode\b/i.test(cmd) ||
    (/Unity\.exe/i.test(cmd) && /-projectpath/i.test(cmd) && !/Win32_Process|Get-CimInstance|Get-Process|tasklist/i.test(cmd));
  if (!cliLaunch && !rawLaunch) return;
  process.stderr.write(`차단: Unity 에디터·batchmode 를 직접 띄우지 않는다. 이 컴퓨터는 동시에 최대 2개이고, 세션끼리 vu.sh 의 슬롯 잠금으로 순서를 정한다.
- 에디터: VU_PROJECT=<작업 트리> bash .claude/skills/verify-unity/scripts/vu.sh launch
- batchmode(시험·빌드): VU_PROJECT=<작업 트리> bash .claude/skills/verify-unity/scripts/vu.sh batch <unity 인자...>
- 열린 에디터에 보내는 시험은 vu.sh test 를 쓴다.
- vu.sh 가 exit 3 "WAIT" 를 내면 자리가 빌 때까지 기다린다: 같은 명령을 VU_WAIT=1800 을 붙여 run_in_background 로 돌리고, 그동안 에디터 없이 되는 일을 한다. 남의 에디터는 끄지 않는다. 끝까지 자리가 없으면 "에디터 대기(2개 한도)" 로 보고한다.
- 다 쓰면 vu.sh cleanup 으로 자리를 돌려준다.
`);
  process.exit(2);
});
