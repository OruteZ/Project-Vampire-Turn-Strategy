# UI 화면 검사 (#33)

모든 화면을 여러 화면비로 띄워, 화면 밖으로·자기 틀 밖으로·글자 상자 밖으로 나간 UI 를 찾는다.

```bash
VU_RUN=<id> bash tools/ui-audit/sweep.sh
```

- 에디터가 열려 있어야 한다 (`.claude/skills/verify-unity`). 재생을 켜고 화면(메뉴·장 선택(원정 길 면·성 면 지도, 1·8·10·15장 장 정보, 병영)·규칙 카드·배치·아군 턴·규칙·기록·결과, 조건·설명이 가장 긴 증원 카드·적 카드 호버, 유닛 상세 창 셋: 병영 병종·아군 유닛·적 유닛) × 화면비 6개(1920x1080, 1280x720, 1920x1200, 1440x1080, 1280x1024, 2560x1080) 를 돈다
- 규칙 카드 (docs/ui/rule-cards.md): `2z-cards` 가 1장 배치에 모든 카드를 문장이 긴 것부터 띄우고, `3-deploy` 가 저장에 남기지 않고 닫는다. 그래서 `3-deploy` 는 `2z-cards` 다음에 돈다. `8a-wave` 가 여는 장에도 카드가 뜰 수 있어 `8b-arrival` 이 먼저 닫는다 (카드의 막이 보드 호버를 막는다)
- 결과: `verify-artifacts/<id>/audit.txt` 와 화면마다 스크린샷. 문제가 하나라도 있으면 exit 1
- 화면비는 `UiAudit.Resize` (`PlayModeWindow.SetCustomRenderingResolution`) 로 바꾼다. `capture game_view` 의 크기 인자는 그림을 늘일 뿐 다시 배치하지 않는다
- 검사 (`Assets/_Project/Scripts/Editor/UiAudit.cs`)
  - off-screen: 그려진 영역이 화면 밖
  - text>box: 한 줄 글이 자기 라벨 폭보다 넓다(R), 줄바꿈된 글이 줄이 늘어 라벨 높이를 넘는다(B)
  - text-hidden: "…" 로 자르는 라벨이 글자 두 개 폭도 안 남았다 (긴 꼬리표가 이름을 밀어낸 경우, #129)
  - no-card: 호버 단계(8b 증원, 8c 적)나 상세 창 단계(2e2, 4b, 8d), 규칙 카드 단계(2z)가 카드·창을 띄우지 못했다
  - out-of-frame: "Frame" 이미지를 가진 가장 가까운 조상(패널·버튼) 밖
  - in-padding: 글이 틀(card·panel·unit·box·window)의 안쪽 여백까지 들어왔다 (docs/art/ui-style.md 5-5, #129)
  - 여유: 그림 2px, 글자는 옆 4px·위아래 글자 크기의 40% (OS 글꼴 정점이 줄 상자보다 크다. 영어 g·p·y 꼬리가 가장 길다. 눈에 보이는 튀어나감은 옆 넘침이라 옆은 엄격하게 둔다)
- 글 예산은 `UiTextParityTests.AssertNoSentences` 가 지킨다: 경고 줄(이름 `Warn`)·버튼 낱말·모드 안내·강화 효과 말고는 문장이 없어야 한다

| 때 | 문제 줄 |
|---|---|
| 개편 전 (dev 9acf95e) | 185 |
| 개편 후 | 0 |

보고서는 `verify-artifacts/<id>/audit.txt` 에 남는다 (전에 커밋하던 docs/review/ui-audit 는 지웠다, #66).

영어는 `LOCALE=en` 으로 돈다.

못 하는 것: 툴팁(`.tip`, 호버 지연 뒤 생긴다). 호버 카드는 8b·8c 가 포인터를 옮겨 본다.

#129 (2026-11-16): 증원 카드 설명이 한 줄로 511px 넘치고 이름이 0 폭이 됐는데 sweep 은 통과였다. 호버 카드가 한 번도 화면에 없었고, 이름은 "…" 예외에 가려졌다.

## 밝기 재기 (`tools/ui-audit/luma.py`, #150)

`py -3 -I tools/ui-audit/luma.py <png>... [--box x0,y0,x1,y1]` 은 PNG 마다 회색조 밝기(0~255)의 평균·p50·p90·p99 와 평균 R·G·B 를 탭으로 한 줄씩 낸다. `--box` 는 그림 픽셀 좌표(왼쪽 위가 0,0)로 그 영역만 잰다. 전투 조명(docs/art/battle-terrain.md "전투 조명")의 목표는 sweep 이 찍은 `4-turn-1920x1080.png` 같은 HUD 포함 전체 프레임 값이고, 바닥·화톳불 옆 같은 색 비교는 `--box` 로 그 칸 둘레만 잰다. `--selftest` 는 스스로를 확인한다(Pillow 필요).
