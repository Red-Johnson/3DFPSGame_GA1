using System.Collections.Generic;
using UnityEngine;

// 무기 모드: 1·2·3키나 마우스 휠로 모드(돌격소총, 지정사수소총, 근접)를 바꾸고, 바뀌면 구독자(UI 등)에게 알린다.
// 모드마다 다른 성질(폭탄, 줌, 시점, 가로등 색, 이름, 아이콘, 총 성능)은 WeaponProfile 에셋에 모아 두고, 다른 컴포넌트는 CurrentProfile만 읽는다.
// 이 클래스는 UI를 모른다.
public class PlayerWeaponMode : MonoBehaviour
{
    [SerializeField] private WeaponProfile[] _profiles;     // 모드마다 하나씩. 휠로 넘기는 순서도 이 순서를 따른다.

    private WeaponMode _mode = WeaponMode.AssaultRifle;
    private Dictionary<WeaponMode, WeaponProfile> _profileByMode;

    public WeaponMode Mode => _mode;
    public WeaponProfile CurrentProfile => GetProfile(_mode);
    public IReadOnlyList<WeaponProfile> Profiles => _profiles ?? System.Array.Empty<WeaponProfile>();

    public event System.Action<WeaponMode> OnModeChanged;

    // 다른 컴포넌트의 Awake에서도 쓸 수 있도록, 실행 순서에 의존하지 않고 처음 부를 때 목록을 만든다.
    public WeaponProfile GetProfile(WeaponMode mode)
    {
        if (_profileByMode == null)
        {
            BuildLookup();
        }

        if (_profileByMode.TryGetValue(mode, out WeaponProfile profile))
        {
            return profile;
        }

        GameLogger.LogError($"[PlayerWeaponMode] {mode} 모드의 WeaponProfile이 목록에 없습니다.");
        return null;
    }

    private void BuildLookup()
    {
        _profileByMode = new Dictionary<WeaponMode, WeaponProfile>();

        if (_profiles == null) return;

        foreach (WeaponProfile profile in _profiles)
        {
            if (profile == null) continue;

            if (!_profileByMode.TryAdd(profile.Mode, profile))
            {
                GameLogger.LogError($"[PlayerWeaponMode] {profile.Mode} 모드의 WeaponProfile이 둘 이상입니다: {profile.name}");
            }
        }
    }

    private void Update()
    {
        // 게임이 진행 중이 아니면 GameInput이 입력 없음으로 돌려준다.
        if (GameInput.AssaultRifleModePressed)
        {
            ChangeMode(WeaponMode.AssaultRifle);
        }
        else if (GameInput.MarksmanRifleModePressed)
        {
            ChangeMode(WeaponMode.MarksmanRifle);
        }
        else if (GameInput.MeleeModePressed)
        {
            ChangeMode(WeaponMode.Melee);
        }
        else if (GameInput.WeaponScrollStep != 0)
        {
            ChangeModeByScroll(GameInput.WeaponScrollStep);
        }
    }

    // 휠을 굴린 방향으로 목록의 다음, 이전 모드로 넘어간다. 끝에서는 반대쪽 끝으로 이어진다.
    private void ChangeModeByScroll(int step)
    {
        int count = _profiles != null ? _profiles.Length : 0;
        if (count == 0) return;

        int current = System.Array.FindIndex(_profiles, profile => profile != null && profile.Mode == _mode);
        int next = (((current + step) % count) + count) % count;

        if (_profiles[next] != null)
        {
            ChangeMode(_profiles[next].Mode);
        }
    }

    private void ChangeMode(WeaponMode mode)
    {
        // 이미 그 모드면 아무것도 하지 않는다. (무기를 다시 꺼내는 연출이 되풀이되지 않게)
        if (mode == _mode) return;

        // 프로필이 없는 모드로는 바꾸지 않는다.
        if (GetProfile(mode) == null) return;

        _mode = mode;
        OnModeChanged?.Invoke(mode);
        GameLogger.Log($"무기 모드: {mode}");
    }
}
