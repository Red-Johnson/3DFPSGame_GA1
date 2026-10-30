# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

Unity 6 (에디터 버전 `6000.3.22f1`)와 C#으로 만드는 3D FPS 게임이다. 빌드·린트·테스트 스크립트나 테스트 어셈블리는 없다. 빌드와 실행은 Unity 에디터에서 `Assets/01.Scenes/GameScene.unity`를 열어 Play로 확인한다. 씬(`.unity`), 머티리얼, 렌더 텍스처 같은 에셋은 에디터에서 편집하는 파일이므로 텍스트로 직접 고치지 않는다.

## 폴더 구조

`Assets/` 아래는 번호 접두사 폴더(`01.Scenes`, `02.Scripts`, `03.Prefabs` …`09.Materials`)로 나뉜다. 새 에셋은 해당 번호 폴더에 넣는다. 스크립트는 `02.Scripts/` 아래에서 기능별(`Player`, `Monster`, `Weapon`, `Environment`, `Camera`, `UI`, `Game`, `Core`)로 나눈다. `Example/`과 `ReviewTest.cs`는 학습·테스트용이라 게임 로직과 무관하다.

## 아키텍처

- **전역 상태:** `GameManager`는 싱글톤(`GameManager.Instance`)이며 `GameState`(`Ready` → `Playing` → `GameOver`)를 들고 있다. 시작 연출 코루틴이 끝나야 `Playing`이 된다. 게임 상태를 따르는 스크립트는 `!GameManager.Instance.IsPlaying`이면 일찍 반환하고, 상태가 바뀌면 `OnStateChanged` 이벤트로 알 수 있다. 플레이어는 `GameManager`를 직접 부르지 않고 체력이 0이 되면 `Player.OnDied` 이벤트만 알린다. `GameManager`가 이를 구독해 `GameOver()`를 처리한다.
- **대미지 전달:** `IDamageable.TakeDamage(DamageInfo)` 인터페이스가 공통 규약이다. `Player`, `Zombie`, `Box`, `Drum` 같은 피격 대상이 이를 구현하고, `PlayerGunFire`·`Bomb` 등 공격 쪽은 `DamageInfo`(값, 맞은 위치, 법선)를 만들어 넘긴다. 새 피격 대상은 구체 클래스가 아니라 이 인터페이스에 맞춘다.
- **플레이어:** 기능별로 컴포넌트가 분리되어 있다(`PlayerMove`, `PlayerRotate`, `PlayerGunFire`, `PlayerBombFire`, `Player`는 스탯·피격). 애니메이터의 파라미터, 레이어, 상태 이름은 `PlayerAnimator`(Player 오브젝트에 붙이는 컴포넌트) 한곳에 모여 있고, 나머지 컴포넌트는 그 메서드(`SetMove`, `StartThrow`, `SetInjured` 등)만 부른다. 컨트롤러 이름을 바꾸면 `PlayerAnimator`의 상수만 고친다. 좌우 시점은 `PlayerRotate`만 누적하고, `CameraRotate`는 위아래만 누적하며 좌우는 `PlayerRotate.Yaw`를 따른다. 마우스 입력에는 `Time.deltaTime`을 곱하지 않는다.
- **무기 모드:** `PlayerWeaponMode`가 `WeaponMode`(`AssaultRifle` 돌격소총, `MarksmanRifle` 지정사수소총, `Melee` 근접)를 들고 1·2·3키로 바꾸고, 모드 이름과 아이콘 표시는 `UI_WeaponStatus`가 맡는다(`PlayerWeaponMode`는 UI를 모른다). 모드가 바뀌면 `PlayerWeaponSwitcher`가 `Melee`에서는 `PlayerMeleeAttack`과 칼을 켜고 `PlayerGunFire`를 끄며, `AssaultRifle`·`MarksmanRifle`에서는 반대로 한다. 총의 성능(`GunSpec`: 탄창, 재장전 시간, 사거리, 공격력, 발사 간격, 연사 여부)과 잔탄은 총마다 따로이고(`PlayerGunFire`가 돌격소총은 연사, 지정사수소총은 한 번 누를 때마다 한 발로 쏜다), 무기를 바꿨다가 돌아와도 잔탄이 유지되며 다른 총으로 바꾸면 재장전이 취소된다. 총마다 약점 공격이 있어서(`GunSpec`의 약점 대미지가 0보다 클 때), 피격 대상의 약점 위치에 둔 `WeakPointHitbox` 콜라이더(좀비는 머리 뼈 아래)에 맞으면 약점 대미지를 주고 약점 여부와 함께 `PlayerGunFire.OnHit`(몬스터에 맞을 때마다. 드럼통과 박스는 제외)로 알리며, `UI_HitMarker`가 45도 기울어진 조준점을 잠깐 보여 준다(일반 명중과 약점 공격은 같은 효과를 색깔, 크기, 시간만 따로 정해 쓴다. 머리가 없는 몬스터는 다른 위치에 약점 콜라이더를 두면 된다). 폭탄은 `MarksmanRifle`를 뺀 모드에서 던질 수 있고(`PlayerBombFire`가 모드를 확인하며, 충전 중에 모드가 바뀌면 던지지 않고 충전을 취소한다), `MarksmanRifle`에서는 마우스 오른쪽을 누르는 동안 `PlayerMarksmanZoom`이 줌(FOV 변경, 조준점 숨김)하고, 줌 중에는 `LookSensitivityScale`로 시점 감도를 시야각 비율만큼 낮춘다(`PlayerRotate`, `CameraRotate`, `ThirdPersonCamera`가 같은 배율을 쓴다).
- **조준 보정:** 총마다 `GunSpec`의 조준 보정 반지름과 세기가 있고(지정사수소총만 켜짐), `PlayerMarksmanZoom`이 줌 중일 때 `PlayerGunFire.ApplyAimAssist`가 조준선이 약점 중심에서 벗어나 지나가는 거리(약점이 있는 거리에서의 미터)가 보정 반지름 안인 가장 가까운 좀비 약점(`WeakPointHitbox.Center`, 가려진 것 제외)을 골라, 총알 방향만 그쪽으로 그 어긋남의 세기(0~1)만큼 당긴다(거리와 상관없이 같은 크기). 화면과 시점은 바뀌지 않는다.
- **사격 반동:** `PlayerGunFire`가 총알이 나갈 때마다 `OnShot(GunSpec)`로 알리고, `PlayerRecoil`이 구독해 그 프레임에 화면(카메라)과 총을 든 몸(가슴, 오른팔 뼈)을 위로 튀게 한 뒤 부드럽게 되돌린다. 세기는 총마다 `GunSpec`의 반동 항목이고, 사격 애니메이션 클립은 쓰지 않는다. 카메라 반동은 렌더링 직전(`Application.onBeforeRender`)에 얹고 다음 프레임 `Update` 시작에 걷어 내서, 시점 입력과 조준 레이에는 영향이 없다. 줌 중에는 `LookSensitivityScale`로 줄인다.
- **근접 공격:** `Melee` 모드의 좌클릭은 `PlayerMeleeAttack`이 처리한다(애니메이터 `Slash` 트리거로 `slash1`을 재생하고, 타격 시점에 이펙트 범위 안의 `IDamageable`에게 대미지를 준다). 몬스터(좀비)가 맞으면 `OnHit`(죽였는지 여부 포함)으로 알리고, `CameraShake`(카메라 회전만 흔듦, 시점 컴포넌트 뒤 `LateUpdate`에서 얹음), `UI_HitMarker`(일반 명중 표시)가 각자 구독해 타격 연출을 낸다. 서 있을 때 온몸, 움직일 때 상체만 battle stance가 되는 것은 `PlayerAnimator.SetMeleeStance`가 `Melee Idle Layer`와 `Stance Layer`의 가중치로 켠다. 총만 쓰던 마지막 상태는 git 태그 `milestone/gun-version`이다.
- **시점:** `CameraViewManager`(메인 카메라)가 V키로 1인칭(`CameraFollow`, `CameraRotate`)과 3인칭(`ThirdPersonCamera`)을 바꾼다. 3인칭은 마우스가 카메라만 돌리고, `PlayerRotate`와 `PlayerMove`가 `CameraViewManager`를 보고 카메라 기준으로 걷는다(몸은 이동 방향과 상관없이 항상 카메라 방향을 유지). V키는 무기 모드와 상관없이 항상 되고, 3인칭에서는 총 모드일 때만 조준점을 보여 준다(`CameraViewManager.SetThirdPersonCrosshair`). 3인칭에서는 카메라가 플레이어 뒤에 있어서 `PlayerGunFire`의 레이가 플레이어 자신의 콜라이더는 건너뛴다.
- **좀비:** `Zombie`(상태 머신, 체력·피격, 풀링)가 책임을 나눠 쓴다. 설정값은 `ZombieStats`(ScriptableObject 에셋, 프리팹에 연결), 이동·점프는 `ZombieMotor`(`NavMeshAgent` 감싸기), 애니메이션은 `ZombieAnimator`(애니메이터 상태 이름으로 `CrossFade`)가 맡는다. 마지막 둘은 컴포넌트가 아닌 일반 클래스라 씬에 붙일 것이 없다. 상태는 `States/`의 클래스로 나뉘고, 공격 대미지는 `Z_Attack` 클립의 애니메이션 이벤트(`ZombieAnimationEvents.OnAttackHit`, 모델 오브젝트에 붙임)가 정한다. 스포너가 좀비를 꺼낼 때 `SetTarget`으로 쫓을 대상을 넘긴다.
- **처치 콤보:** 좀비가 죽으면 `Zombie`가 `ZombieManager.NotifyKilled`로 알리고(총, 칼, 폭탄 등 원인과 상관없이. 풀로 돌아가는 것은 알리지 않는다), `UI_KillCombo`가 `OnZombieKilled`를 구독해 조준점 아래에 랜턴 아이콘을 띄운다. 마지막 처치 후 제한 시간 안에 또 죽이면 콤보가 쌓여 "X2"처럼 파란 글자가 붙고, 시간이 지나면 콤보가 끝나며 서서히 사라진다.
- **부서지는 차량:** 씬의 모든 차량(`Car_06`, `Car_13`, `Car_16`, `Car_19`, `Futuristic_Car_1`, `Van`)에 붙인 `DestructibleCar`가 `IDamageable`을 구현한다. 체력(100)에 따라 단계가 바뀐다: 70 초과는 효과 없음, 70 이하(0 초과)는 `VFX_BlackSmoke`, 0이 되면 같은 위치에서 `VFX_Fire`로 바뀌어 2초 탄 뒤 폭발 이펙트를 한 번 내고 폭발 대미지(100)를 준다. 그 뒤로도 불은 계속 타고 차량은 잔해로 남는다. 연기와 불은 앞바퀴, 뒷바퀴 위치로 알아낸 보닛에서 나오고, 폭발 반경 하나(`_explosionRange`)가 폭발 대미지 범위이면서 폭발 이펙트 안 충격파(`Shockwave`)의 반지름이다(이펙트를 반경에 맞춰 크기를 조정하고, 반복 재생되지 않게 한다). 차량은 별도 레이어가 없어서(카메라와 미니맵의 렌더 범위를 바꾸지 않으려고) `ExplosionDamage.Apply`가 `DestructibleCar.ApplyExplosion`을 따로 불러 폭탄, 드럼통, 다른 차량의 폭발이 차량에도 닿는다(연쇄 폭발).
- **풀링과 이펙트:** `PrefabPool<T>`가 오브젝트를 재사용한다. 파티클 이펙트는 `EffectPool.For(prefab).Play(...)`로 꺼내 쓰고, `PooledEffect`가 재생이 끝나면 스스로 돌아간다. 자주 터지는 이펙트(총알 자국, 폭발)의 `Instantiate`/`Destroy` 반복을 줄이려고 쓴다. `PooledEffect`가 프리팹의 Stop Action(Destroy)을 꺼서 재사용한다. 폭발 대미지는 `ExplosionDamage.Apply`를 함께 쓴다.
- **미니맵:** `MinimapCamera`(플레이어 머리 위에서 따라다니는 직교 카메라)가 `MinimapRenderTexture`에 그리고, `UI_Minimap`이 `orthographicSize`를 바꿔 확대/축소한다. 확대/축소 로직이 `MinimapCamera.Update`(키 입력)에 임시로 남아 있고 Todo로 `UI_Minimap` 쪽으로 옮기는 중이다.
- **커서:** `CursorManager`가 커서를 잠그고, 왼쪽 Alt를 누르는 동안 풀어준다. 발사와 폭탄은 커서가 보이는 동안 입력되지 않는다(`GameInput`이 커서 상태를 확인).
- **입력:** 레거시 `UnityEngine.Input`을 쓰되, 플레이어 입력은 정적 클래스 `GameInput`(이동, 시점, 점프, 달리기, 장전 R, 발사 마우스 왼쪽(연사는 누르고 있는 동안 `FireHeld`, 단발은 누르는 순간 `FirePressed`), 폭탄 마우스 오른쪽: 누르고 있으면 충전, 떼면 던짐, 무기 모드 전환 1·2·3키, 지정사수소총 줌 마우스 오른쪽)이 한곳에서 읽는다. 게임이 진행 중이 아니면 입력 없음으로 돌려준다. 커서를 푸는 Alt와 미니맵 확대·축소 키(0, 9), 무기 모드 전환 키도 모두 `GameInput`의 상수로 정하므로, `GameInput` 밖에서는 `Input`을 직접 읽지 않는다. 사격은 Input Manager의 `Fire1`(left ctrl도 포함) 대신 마우스 왼쪽 버튼을 직접 읽는다. `InputSystem_Actions.inputactions`가 있지만 코드는 새 Input System을 쓰지 않는다.

## 코드 컨벤션

- 한국어 주석을 쓰고, 설명 주석은 클래스 위에 요약 형태로 단다.
- 비공개 필드는 `_camelCase`, 인스펙터 노출은 `[SerializeField] private`, 코루틴 메서드 이름은 `_Coroutine`으로 끝낸다.
- 초기화 순서: 체력, 기력, 총알, 폭탄 같은 상태는 `Awake`에서 채우고, UI는 `OnEnable`/`OnDisable`에서 이벤트를 구독하고 `Start`에서 한 번 전부 갱신한다(`RefreshAll`). `Start`는 컴포넌트 사이의 실행 순서가 보장되지 않으므로, 다른 컴포넌트가 `Start`에서 채우는 값을 UI가 `Start`에서 읽지 않는다. `GameManager`와 `ZombieManager`는 `[DefaultExecutionOrder(-100)]`으로 가장 먼저 초기화하므로, 다른 컴포넌트가 `OnEnable`에서 `Instance`를 써도 된다. 같은 오브젝트의 다른 컴포넌트에 의존하는 경우(예: `ZombieHealthBar`가 `Zombie`의 값을 쓰는 경우)는 그 컴포넌트의 `Awake`가 끝난 뒤인 `Start`에서 구독한다.
- 참조: 같은 오브젝트의 컴포넌트는 `Awake`에서 `GetComponent`로, 씬의 다른 오브젝트는 `[SerializeField]`로 인스펙터에서 연결한다. `Find` 계열은 연결을 빠뜨렸을 때의 안전장치로만 한 번 쓰고 경고 로그를 남긴다. 프리팹은 씬 오브젝트를 직렬화할 수 없으므로 스포너가 `SetTarget`처럼 코드로 넘겨 준다. `Camera.main`은 `Awake`에서 한 번만 캐시한다.
- 튜닝 값(체력, 속도, 쿨타임 등)은 매직 넘버로 두지 않고 `[SerializeField]` 필드나 상수로 둔다. 자세한 기준은 `Docs/CodeReviewGuide.md`를 따른다.

## 코드 리뷰와 협업 규칙

`GEMINI.md`와 `Docs/CodeReviewGuide.md`에 리뷰 기준이 있다. `main`에 푸시하면서 `Assets/02.Scripts/**/*.cs`가 바뀌면 `.github/workflows/gemini-commit-review.yml`이 Gemini로 커밋 리뷰를 자동 실행한다.

- 리뷰할 때는 `Docs/CodeReviewGuide.md`의 중요도 기준(Critical/High/Medium/Low)을 적용하고, 리뷰와 관련 없는 파일은 수정하지 않는다.
- 요청받지 않았다면 직접 코드를 변경하거나 커밋하지 않는다.
- 실행해서 확인하지 않았다면 실행했다고 말하지 않는다.
- 답변은 한국어로 쓴다.
- 커밋 메시지는 `feat:`, `fix:`, `refactor:`, `asset:` 접두사를 쓰고 한국어로 쓴다. 동작은 그대로 두고 구조만 바꾸는 변경은 `refactor:`를 쓴다.
