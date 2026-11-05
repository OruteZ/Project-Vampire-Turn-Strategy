# P4~P7 진행 체크포인트 (2026-11-05 12:25 KST)

사용자 지시: "이후로 p7까지 쭉 진행. pr은 작성없이 P7까지 완료하고 작성. 각 페이즈마다 DEV브랜치에 푸쉬 처리만". 페이즈마다 작업 브랜치를 `git push origin HEAD:dev` (fast-forward) 로 올리고, P7 이 끝나면 dev→main PR 하나를 쓴다. 보고는 한국어.

## 지금 상태 (갱신)
- P4 끝: dev 에 push (c6413fb), 이슈 #8 닫음. 리뷰 반영까지 완료.
- 브랜치 `p5-campaign` (dev 에서, 커밋·push 됨, dev 에는 아직 안 올림). P5 파일: tools/golden/campaign.mjs, out/campaign.json, prototype-golden.mjs 의 campaign 명령, Core/Campaign.cs, Game/CampaignStore.cs, Tests/CampaignParityTests.cs. CampaignParity 4/4 통과, 빨강 확인 끝.

## 다음 순서
2. P5: dev 에서 `p5-campaign` 브랜치, 위 P5 파일 커밋, 계획 P5 lane 10개·perf, dev 로 push, 이슈 열고 닫기.
3. P6: 육각 보드 화면 (BoardView, ArtCatalog, 지형 프리팹), 11개 장 스크린샷을 `docs/review/` 에 커밋, dev 로 push.
4. P7: 유닛 모델·이벤트 재생 (UnitView, EventPlayer), PlayMode 테스트, 스크린샷, dev 로 push.
5. dev→main PR 하나 (P4~P7 요약, 스크린샷 링크, lane 7 NOT VERIFIED 와 창벽 HTML 동작 명시), 한국어 보고.

검증은 `.claude/skills/verify-unity/` 의 vu.sh 로만, 에디터는 백그라운드. 결정 기록은 `.audit/p4-p7-run.tsv` 에 한 줄씩.
