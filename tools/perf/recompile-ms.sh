#!/usr/bin/env bash
P="${1:-$PWD}"; u(){ unity --no-banner command --project-path "$P" "$@" --json 2>/dev/null; }
s=$(date +%s%N)
u script eval --code 'UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation(UnityEditor.Compilation.RequestScriptCompilationOptions.CleanBuildCache); "requested"' >/dev/null
seen=0
for i in $(seq 1 600); do
  st=$(u editor status)
  if echo "$st" | grep -qE '"compiling":true|"domainReloadInProgress":true' || [ -z "$st" ]; then seen=1
  elif [ $seen = 1 ] && echo "$st" | grep -q '"status":"ready"'; then break; fi
  sleep 0.25
done
e=$(date +%s%N); echo "seen_compile=$seen ms=$(( (e-s)/1000000 ))"
