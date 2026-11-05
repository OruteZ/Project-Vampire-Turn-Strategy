# P4~P7 진행 체크포인트 (2026-11-05 12:25 KST)

사용자 지시: "이후로 p7까지 쭉 진행. pr은 작성없이 P7까지 완료하고 작성. 각 페이즈마다 DEV브랜치에 푸쉬 처리만". 페이즈마다 작업 브랜치를 `git push origin HEAD:dev` (fast-forward) 로 올리고, P7 이 끝나면 dev→main PR 하나를 쓴다. 보고는 한국어.

## 지금 상태 (갱신 2)
- P4 dev push (c6413fb, 이슈 #8 닫음). P5 dev push (67ddba4, 이슈 #9 닫음).
- 다음은 P6. dev 에서 `p6-board` 브랜치.

## 다음 순서
3. P6: 육각 보드 화면 (BoardView, ArtCatalog, 지형 프리팹), 11개 장 스크린샷을 `docs/review/` 에 커밋, dev 로 push.
4. P7: 유닛 모델·이벤트 재생 (UnitView, EventPlayer), PlayMode 테스트, 스크린샷, dev 로 push.
5. dev→main PR 하나 (P4~P7 요약, 스크린샷 링크, lane 7 NOT VERIFIED 와 창벽 HTML 동작 명시), 한국어 보고.

검증은 `.claude/skills/verify-unity/` 의 vu.sh 로만, 에디터는 백그라운드. 결정 기록은 `.audit/p4-p7-run.tsv` 에 한 줄씩.
