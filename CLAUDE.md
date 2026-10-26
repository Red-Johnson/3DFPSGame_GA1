# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

Unity 6 (에디터 버전 `6000.3.22f1`)와 C#으로 만드는 3D FPS 게임이다. 빌드·린트·테스트 스크립트나 테스트 어셈블리는 없다. 빌드와 실행은 Unity 에디터에서 `Assets/01.Scenes/GameScene.unity`를 열어 Play로 확인한다. 씬(`.unity`), 머티리얼, 렌더 텍스처 같은 에셋은 에디터에서 편집하는 파일이므로 텍스트로 직접 고치지 않는다.

## 폴더 구조

`Assets/` 아래는 번호 접두사 폴더(`01.Scenes`, `02.Scripts`, `03.Prefabs` …`09.Materials`)로 나뉜다. 새 에셋은 해당 번호 폴더에 넣는다. 스크립트는 `02.Scripts/` 아래에서 기능별(`Player`, `Monster`, `Weapon`, `Environment`, `Camera`, `UI`, `Game`, `Core`)로 나눈다. `Example/`과 `ReviewTest.cs`는 학습·테스트용이라 게임 로직과 무관하다.

## 아키텍처

- **전역 상태:** `GameManager`는 싱글톤(`GameManager.Instance`)이며 `GameState`(`Ready` → `Playing` → `GameOver`)를 들고 있다. 시작 연출 코루틴이 끝나야 `Playing`이 된다. 입력을 받는 스크립트(예: `CursorManager`)는 `State != Playing`이면 일찍 반환하는 방식으로 게임 상태를 따른다. `Player.TakeDamage`가 체력이 0 이하가 되면 `GameManager.GameOver()`를 호출한다.
- **대미지 전달:** `IDamageable.TakeDamage(DamageInfo)` 인터페이스가 공통 규약이다. `Player`, `Zombie`, `Box`, `Drum` 같은 피격 대상이 이를 구현하고, `PlayerGunFire`·`Bomb` 등 공격 쪽은 `DamageInfo`(값, 맞은 위치, 법선)를 만들어 넘긴다. 새 피격 대상은 구체 클래스가 아니라 이 인터페이스에 맞춘다.
- **플레이어:** 기능별로 컴포넌트가 분리되어 있다(`PlayerMove`, `PlayerRotate`, `PlayerGunFire`, `PlayerBombFire`, `Player`는 스탯·피격).
- **미니맵:** `MinimapCamera`(플레이어 머리 위에서 따라다니는 직교 카메라)가 `MinimapRenderTexture`에 그리고, `UI_Minimap`이 `orthographicSize`를 바꿔 확대/축소한다. 확대/축소 로직이 `MinimapCamera.Update`(키 입력)에 임시로 남아 있고 Todo로 `UI_Minimap` 쪽으로 옮기는 중이다.
- **커서:** `CursorManager`가 커서를 잠그고, 왼쪽 Alt를 누르는 동안 풀어준다. 총기는 커서가 보이는 동안 발사하지 않는다(`PlayerGunFire`가 커서 상태를 확인).
- **입력:** 레거시 `UnityEngine.Input`(`GetAxisRaw`, `GetButton`, `GetKeyDown`)을 쓴다. `InputSystem_Actions.inputactions`가 있지만 코드는 새 Input System을 쓰지 않는다.

## 코드 컨벤션

- 한국어 주석을 쓰고, 설명 주석은 클래스 위에 요약 형태로 단다.
- 비공개 필드는 `_camelCase`, 인스펙터 노출은 `[SerializeField] private`, 코루틴 메서드 이름은 `_Coroutine`으로 끝낸다.
- 튜닝 값(체력, 속도, 쿨타임 등)은 매직 넘버로 두지 않고 `[SerializeField]` 필드나 상수로 둔다. 자세한 기준은 `Docs/CodeReviewGuide.md`를 따른다.

## 코드 리뷰와 협업 규칙

`GEMINI.md`와 `Docs/CodeReviewGuide.md`에 리뷰 기준이 있다. `main`에 푸시하면서 `Assets/02.Scripts/**/*.cs`가 바뀌면 `.github/workflows/gemini-commit-review.yml`이 Gemini로 커밋 리뷰를 자동 실행한다.

- 리뷰할 때는 `Docs/CodeReviewGuide.md`의 중요도 기준(Critical/High/Medium/Low)을 적용하고, 리뷰와 관련 없는 파일은 수정하지 않는다.
- 요청받지 않았다면 직접 코드를 변경하거나 커밋하지 않는다.
- 실행해서 확인하지 않았다면 실행했다고 말하지 않는다.
- 답변은 한국어로 쓴다.
- 커밋 메시지는 `feat:`, `fix:`, `asset:` 접두사를 쓰고 한국어로 쓴다.
