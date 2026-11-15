#!/bin/bash
# matrix.sh <tag> <stages csv> : runs the lookahead bot over a set of formations, one process per formation. Output scratch/<tag>.tsv
D="$(cd "$(dirname "$0")" && pwd)"; mkdir -p "$D/scratch"; out="$D/scratch/$1.tsv"; : > "$out"
COMPS=("" "comp:sword:9 parcher:*" "comp:archer:9 pspear:*" "comp:knight:9 parcher:* pspear:*" "comp:hunter:9 knight:2 sword:2 archer:* pspear:*" "comp:knight:2 sword:2 archer:2 doctor:1 gravedigger:1 pspear:* parcher:*")
i=0
for c in "${COMPS[@]}"; do i=$((i+1)); ( "$D/sp" mc "$2" "$c" "$3" | sed "s/^/c$i\t/" > "$out.$i" ) & done; wait
cat "$out".* > "$out"; rm -f "$out".*; sort -t$'\t' -k2,2V -k1,1 "$out"
