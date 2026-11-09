# 사운드 구현 기록

기획서: docs/audio/sounds.md. 코드: `Assets/_Project/Scripts/Game/Audio/`.

## 구조

| 파일 | 하는 일 |
|---|---|
| `SoundCatalog.cs` | `Cue` (변형 클립 + 볼륨, 차례로 돈다), `UnitSounds` (공격·맞음·죽음·걸음), `SoundCatalog` (기획서 표의 칸 = 필드 하나) |
| `Sound.cs` | 재생기. 효과음은 한 AudioSource 의 PlayOneShot, 음악·환경음은 루프 소스 둘 (0.25 초 줄이고 0.25 초 키움), 화로는 그 칸에 3D 루프 소스 |
| `BattleSounds.cs` | 전투 이벤트 → 칸. 배너·팝업은 Core 의 `BannerKind`·`PopupKind` enum 으로 가른다 |
| `UiSound.cs` | 버튼 올림·누름 (uGUI `Selectable`, UI Toolkit `VisualElement`). 버튼 만드는 함수가 부른다 |

- 칸은 **필드로 고른다**: `Sound.Play(s => s.click)`. 문자열 키·이름 찾기가 없어서 칸 이름을 바꾸거나 지우면 컴파일 오류로 드러난다.
- 카탈로그 위치: `Assets/_Project/Audio/SoundCatalog.asset`, `ArtCatalog.sound` 에 걸려 있다. 각 화면 Flow 가 `Sound.Use(art.sound)` 를 부른다 (씬에 새 참조를 걸지 않는다).
- 유닛 소리: `UnitLook.sounds` (모습마다). 빈 칸은 `SoundCatalog.unitDefault`. 스폰은 `SoundCatalog.spawn`. BattleView 가 애니메이션 트리거 바로 옆에서 부른다 (소리 = 동작 시점).
- 카탈로그가 없거나 칸이 비면 소리 없이 지나간다.

## 새 사운드 더하기

1. docs/audio/sounds.md 표에 줄을 더한다.
2. 전역 사운드: `SoundCatalog` 에 `Cue` 필드 하나 → 재생 자리에서 `Sound.Play(s => s.<필드>)`.
   - 전투 이벤트로 나는 소리는 `BattleSounds.Of` 의 switch 에 한 줄. 새 배너·팝업 종류는 Core 의 enum 에 더한다 (`SoundTests` 가 매핑을 확인).
3. 유닛마다 다른 소리: `UnitSounds` 에 필드 → BattleView 의 그 동작 옆에서 `Sound.Play(view.Sounds, s => s.<필드>)`.
4. 새 버튼 함수: 버튼을 만든 뒤 `UiSound.Add(button)`.

## 검증

- `SoundTests` (EditMode): 이벤트 → 칸 매핑, 배너·팝업 style 이 enum 도입 전과 같음, 변형이 무작위 없이 차례로 돎.
- 소리는 사람이 듣고 확인한다 (이슈 #105 `## 확인`).
