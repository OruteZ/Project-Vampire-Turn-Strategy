# 설정 창

> 출처: docs/design/ui.md (커밋 f7fc7f8). docs/design 에 설정 화면 방향은 없다. 항목은 docs/art/battle-camera.md 1-2·6-5·11 과 #114 에서 왔다.
> 스타일: docs/art/ui-style.md. 색·글꼴·틀·버튼은 그 문서의 토큰 이름으로 쓴다.

## 요구사항

### 여는 곳

1. 메인 메뉴 오른쪽 위 언어 줄(main-menu.md 12) 왼쪽에 **"설정"** 글 버튼 하나를 둔다. 모양은 언어 줄 글자와 같다(`small`, `text.3`, 호버 `text.1`). 언어 줄과 사이 16.
2. 누르면 화면 가운데에 설정 창이 뜬다(ui-style.md 16 "특정한 순간"의 가운데 창). 뒤 화면은 어둡게 덮고 클릭을 받지 않는다.
3. 닫기: 창 아래 "닫기" 버튼, Esc, 창 밖(덮개) 클릭. 바꾼 값은 누르는 즉시 저장되므로 "취소" 는 없다.
4. 전투·캠페인 화면에는 설정 창이 없다. 배속은 전투 중에도 F 키로 바꾼다(battle-hud.md 4-5).

### 항목

창 안에 줄 넷. 줄마다 왼쪽에 이름, 오른쪽에 고를 값 버튼(고른 값은 primary 버튼, 나머지는 보통 버튼, ui-style.md 3). 줄에 마우스를 올리면 설명(툴팁)이 나온다.

| 줄 | 이름 | 값 | 처음 값 | 설명(툴팁) |
|---|---|---|---|---|
| 카메라 | 카메라 연출 | 켬 · 끔 | 켬 | 변이·보름달·처치 때 카메라가 다가가고 색이 바뀌는 연출 |
| 흔들림 | 화면 흔들림 | 끔 · 약하게 · 보통 | 보통 | 큰 공격 때 화면이 흔들리는 세기 |
| 배속 | 전투 배속 | ×1 · ×2 · ×4 | ×1 | 적 턴 재생 빠르기. 전투 중에는 F 로 바꾼다 |
| HUD 크기 (#150 보고서 C2) | 전투 HUD 크기 | 작게 · 보통 · 크게 | 작게 | 전투 화면의 패널·버튼·글자 크기. 다음 전투부터 듣는다 |

5. **카메라 연출 끔**: battle-camera.md 2~5, 7~10 절(당김·가까이·채도·비네트·전투 시작 훑기·마무리 느린 화면)과 6 절의 짧은 멈춤을 하지 않는다. 카메라는 늘 기본 시점(1-1)이다. 연출을 보이려고 더 기다리던 시간(흡혈·처치·마무리 대기)도 없앤다.
6. **화면 흔들림**: 6 절 흔들림 세기에 곱한다. 끔 0, 약하게 0.5, 보통 1. 카메라 연출을 꺼도 흔들림은 이 줄대로 한다.
7. **전투 배속**: battle-hud.md 4-5 의 F 키와 같은 값이다. 한쪽에서 바꾸면 다른 쪽도 바뀐다.
7-1. **전투 HUD 크기**: 전투 HUD 배율(battle-hud.md 1, `BattleFlow.HudZoom`). 작게 0.55, 보통 0.6, 크게 0.64. 처음 정한 0.7·0.85 는 가장 긴 배치 편성 패널(병종 아홉, 8a)과 규칙 창이 1080 높이에서 화면 위로 나갔고(스윕 0.7 에서 9줄, 0.85 에서 82줄), 0.67 은 스윕은 통과했지만 편성 패널이 장 제목을 덮어 0.64 로 낮췄다 (#150 보고서 C2). 전투 화면만 바뀐다. 메뉴·원정 지도 같은 다른 화면의 크기 설정은 두지 않는다(고정 px 배치라 100% 를 넘기면 칸이 겹친다, ui-style.md 15).
8. 네 값은 게임을 껐다 켜도 남는다(PlayerPrefs). 저장 진행(캠페인)과 따로다.

### 배치 (ui-style.md)

9. 창: `frame.menu`, 폭 640, 높이는 내용대로. 안쪽 여백 32. 위에 제목 "설정" `h.1` `text.1` 가운데.
10. 줄: 높이 56, 사이 12. 이름 `body` `text.1` 왼쪽, 값 버튼 줄 오른쪽 맞춤. 값 버튼 높이 40, 최소 폭 96, 사이 8.
11. "닫기" 버튼: 보통 버튼, 창 아래 가운데, 마지막 줄 아래 24.
12. 덮개: 화면 전체 `ink` 60%.

### 글 (Text 표)

| 키 | 한국어 | English |
|---|---|---|
| settings.open | 설정 | Settings |
| settings.title | 설정 | Settings |
| settings.camera | 카메라 연출 | Camera effects |
| settings.cameraTip | 변이·보름달·처치 때 카메라가 다가가고 색이 바뀌는 연출 | Camera moves in and tints on rises, full moon and kills |
| settings.shake | 화면 흔들림 | Screen shake |
| settings.shakeTip | 큰 공격 때 화면이 흔들리는 세기 | How hard the screen shakes on big hits |
| settings.speed | 전투 배속 | Battle speed |
| settings.speedTip | 적 턴 재생 빠르기. 전투 중에는 F 로 바꾼다 | Enemy turn playback speed. Press F in battle |
| settings.on | 켬 | On |
| settings.off | 끔 | Off |
| settings.weak | 약하게 | Low |
| settings.normal | 보통 | Normal |
| settings.hud | 전투 HUD 크기 | Battle HUD size |
| settings.hudTip | 전투 화면의 패널·버튼·글자 크기. 다음 전투부터 듣는다 | Size of battle panels, buttons and text. Applies from the next battle |
| settings.hudSmall | 작게 | Small |
| settings.hudLarge | 크게 | Large |
| settings.close | 닫기 | Close |

## 정한 세부 (docs/design 에 없는 것)

- **메인 메뉴에서만 연다(4번).** 근거: localization.md 9 가 언어도 메인 메뉴에서만 바꾸게 했다. 전투 화면에 자리를 더 쓰지 않고, 전투 중 바꿀 일이 잦은 배속은 이미 F 키가 있다.
- **흔들림 세기는 셋(6번).** 근거: battle-camera.md 6-5 "0 포함". 지금 흔들림이 이미 작아(기본 거리 0.6%) 더 센 값은 두지 않는다.
- **짧은 멈춤은 카메라 연출을 따른다(5번).** 근거: 멈춤은 흔들림과 달리 화면 멀미가 아니라 연출 박자다. 연출을 끈 사람은 재생이 끊기지 않기를 바란다.
- **언어 줄은 설정 창으로 옮기지 않는다.** 근거: localization.md 6~8 의 언어 줄은 이미 확인을 받은 화면이다. 설정 창은 그 옆에 문 하나만 더한다.

## 열린 질문 (사용자가 정할 것)

1. 전투 중(일시정지 창)에도 설정 창을 열지.
2. 소리 크기(음악·효과음) 줄을 더할지 (#105 사운드와 함께).
