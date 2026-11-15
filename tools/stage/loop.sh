#!/bin/bash
# loop.sh <tag> [n=20] [seed=1]: n random builds per stage on every stage, one process per stage. Output scratch/<tag>.tsv
D="$(cd "$(dirname "$0")" && pwd)"; mkdir -p "$D/scratch"; out="$D/scratch/$1.tsv"; rm -f "$out" "$out".*
for i in $(seq 1 20); do ( "$D/sp" builds s$i "${2:-20}" "${3:-1}" > "$out.$i" 2>/dev/null ) & done; wait
cat "$out".* > "$out"; rm -f "$out".*; wc -l < "$out"
