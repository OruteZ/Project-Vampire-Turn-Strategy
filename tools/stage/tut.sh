#!/bin/bash
# tut.sh <tag>: on each unit's unlock stage, 20 random builds that field two of it against 20 that leave it out.
D="$(cd "$(dirname "$0")" && pwd)"; out="$D/scratch/$1"; rm -f "$out".*
for p in sword:s3 archer:s4 gravedigger:s5 scout:s6 doctor:s7 knight:s8 hunter:s9; do k=${p%%:*}; s=${p##*:}
  ( SP_FORCE=$k "$D/sp" builds $s 20 11 > "$out.$k.with" 2>/dev/null ) &
  ( SP_BAN=$k "$D/sp" builds $s 20 12 > "$out.$k.without" 2>/dev/null ) &
done; wait
for f in "$out".*.with; do k=$(basename "$f" | cut -d. -f2)
  for w in with without; do awk -F'\t' -v k=$k -v w=$w '{n++; won+=$5; if($5){lost+=$6}; b+=$8; h+=$9; t+=$10} END{printf "%-12s %-8s win %2d/%2d  lost %.1f  bury %.1f heal %.1f trap %.1f\n", k, w, won, n, (won?lost/won:0), b/n, h/n, t/n}' "$out.$k.$w"; done; done
