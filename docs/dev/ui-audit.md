# UI 화면 검사 (#33)

모든 화면을 여러 화면비로 띄워, 화면 밖으로·자기 틀 밖으로·글자 상자 밖으로 나간 UI 를 찾는다.

```bash
VU_RUN=<id> bash tools/ui-audit/sweep.sh
```

- 에디터가 열려 있어야 한다 (`.claude/skills/verify-unity`). 재생을 켜고 화면 7개(메뉴·장 선택·배치·아군 턴·규칙·기록·결과) × 화면비 6개(1920x1080, 1280x720, 1920x1200, 1440x1080, 1280x1024, 2560x1080) 를 돈다
- 결과: `verify-artifacts/<id>/audit.txt` 와 화면마다 스크린샷. 문제가 하나라도 있으면 exit 1
- 화면비는 `UiAudit.Resize` (`PlayModeWindow.SetCustomRenderingResolution`) 로 바꾼다. `capture game_view` 의 크기 인자는 그림을 늘일 뿐 다시 배치하지 않는다
- 검사 (`Assets/_Project/Scripts/Editor/UiAudit.cs`)
  - off-screen: 그려진 영역이 화면 밖
  - text>box: 글자 정점이 자기 RectTransform 밖 (Overflow 로 넘친 글자)
  - out-of-frame: "Frame" 이미지를 가진 가장 가까운 조상(패널·버튼) 밖
  - 여유: 그림 2px, 글자는 옆 4px·위아래 글자 크기의 40% (OS 글꼴 정점이 줄 상자보다 크다. 영어 g·p·y 꼬리가 가장 길다. 눈에 보이는 튀어나감은 옆 넘침이라 옆은 엄격하게 둔다)
- 글 예산은 `UiTextParityTests.AssertNoSentences` 가 지킨다: 경고 줄(이름 `Warn`)·버튼 낱말·모드 안내·강화 효과 말고는 문장이 없어야 한다

| 때 | 문제 줄 |
|---|---|
| 개편 전 (dev 9acf95e) | 185 |
| 개편 후 | 0 |

보고서는 `verify-artifacts/<id>/audit.txt` 에 남는다 (전에 커밋하던 docs/review/ui-audit 는 지웠다, #66).

영어는 `LOCALE=en` 으로 돈다.

못 하는 것: 툴팁(호버해야 생긴다).
