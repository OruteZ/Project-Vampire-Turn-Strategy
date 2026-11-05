---
id: 001
created: 20261105
status: done
issue: #19
done: 20261106
---

# 001. GameData ScriptableObject issue

## 개요

현재 게임 내부 데이터를 `static partial class GameData` 로 관리중이다.

이 정적 클래스 내부 값들은 게임 디자인의 의도와 플레이테스트 결과에 따라서 계속해서 가변적으로 변하고 사용자가 가장 많이 건드려야 하는 부분 중 하나이다.

따라서 하드코딩된 방식이 아닌 다음과 같은 구조개편을 요구



## 요구사항

1.  각 GameData를 ScriptableObject로서 만들어 에디터에서 시각화 가능하게 변경

2. 해당 GameData를 수정하기에 용이한 기획자용 에디터 제작


