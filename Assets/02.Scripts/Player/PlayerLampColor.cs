using System;
using UnityEngine;

// 가로등 불빛 색: 무기 모드에 맞춰 손에 든 가로등의 빛 색을 바꾸고, 머리의 빛 색은 한 번 정해 고정한다.
// - 모드별 색은 무기 프로필(WeaponProfile)의 LampColor에서 읽는다. (돌격소총: 따뜻한 색, 지정사수소총: 붉은 빛, 근접: 청록 빛)
// - 머리(HeadGlow)는 연속 처치 게이지(UI_ComboTimeBar)와 같은 푸른빛으로 고정한다. 근접 가로등의 청록빛과 구별된다.
// 가로등은 총 모드가 쓰는 GunLamp와 근접 모드가 쓰는 MeleeLamp가 따로 있고, 켜고 끄는 일은 PlayerWeaponSwitcher가 맡는다.
// 이 컴포넌트는 색만 바꾼다. (세기, 범위, 번짐 크기는 각 광원과 파티클에서 정한다.)
// 총구 섬광(광원과 그림)도 총 모드일 때 가로등 색으로 맞춘다.
// 총알 궤적 색도 이 색을 따르도록 GetLampColor로 모드별 색을 알려 준다. (색은 이 컴포넌트 한곳에서만 정한다.)
[RequireComponent(typeof(PlayerWeaponMode))]
public class PlayerLampColor : MonoBehaviour
{
    // 색을 입힐 가로등 하나(발광 모델, 광원, 번짐)
    [Serializable]
    private class LampTarget
    {
        public Renderer Model;                      // 전구가 빛나는 가로등 모델(발광 색을 바꾼다)
        public Light[] Lights;                      // 가로등에 달린 광원들
        public ParticleSystemRenderer[] Halos;      // 가로등에 달린 번짐(파티클 시작 색은 흰색, 여기서 색을 곱한다)
    }

    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int TintColorId = Shader.PropertyToID("_TintColor");

    [Header("머리 빛(고정)")]
    [SerializeField] private Color _headColor = new Color(0.25f, 0.6f, 1f);         // 처치 콤보 글자와 같은 푸른 빛. 근접 가로등의 청록빛과 구별되게
    [SerializeField] private Light _headLight;
    [SerializeField] private ParticleSystemRenderer _headHalo;

    [Header("가로등")]
    [SerializeField] private LampTarget _gunLamp;           // 총 종류의 무기들이 함께 쓴다.
    [SerializeField] private LampTarget _meleeLamp;         // 근접 종류의 무기가 쓴다.
    [SerializeField] private float _emissionIntensity = 3.5f;   // 전구 발광의 밝기 배율

    [Header("총구 섬광(총 모드에서만 쓴다)")]
    [SerializeField] private Light[] _muzzleFlashLights;                // 섬광마다 달린 광원
    [SerializeField] private Renderer[] _muzzleFlashRenderers;          // 섬광 그림(Additive 머티리얼의 _TintColor에 색을 입힌다)

    private PlayerWeaponMode _weaponMode;
    private MaterialPropertyBlock _block;
    private Color[] _flashBaseTints;        // 섬광 머티리얼에 원래 정해진 색(밝기와 투명도를 지키려고 기억해 둔다)

    private void Awake()
    {
        _weaponMode = GetComponent<PlayerWeaponMode>();
        _block = new MaterialPropertyBlock();

        _flashBaseTints = new Color[_muzzleFlashRenderers != null ? _muzzleFlashRenderers.Length : 0];
        for (int i = 0; i < _flashBaseTints.Length; i++)
        {
            Renderer flash = _muzzleFlashRenderers[i];
            _flashBaseTints[i] = flash != null && flash.sharedMaterial.HasProperty(TintColorId)
                ? flash.sharedMaterial.GetColor(TintColorId)
                : Color.white;
        }
    }

    private void OnEnable()
    {
        _weaponMode.OnModeChanged += Apply;
    }

    private void OnDisable()
    {
        _weaponMode.OnModeChanged -= Apply;
    }

    private void Start()
    {
        // 머리 빛은 한 번만 정하고, 가로등은 처음 모드에 맞춘다.
        if (_headLight != null) _headLight.color = _headColor;
        TintHalo(_headHalo, _headColor);

        Apply(_weaponMode.Mode);
    }

    // 모드에 해당하는 가로등 색. (총알 궤적이 같은 색을 쓰도록 PlayerGunFire가 부른다.)
    public Color GetLampColor(WeaponMode mode)
    {
        WeaponProfile profile = _weaponMode.GetProfile(mode);
        return profile != null ? profile.LampColor : Color.white;
    }

    private void Apply(WeaponMode mode)
    {
        WeaponProfile profile = _weaponMode.GetProfile(mode);
        if (profile == null) return;

        Color color = profile.LampColor;

        Tint(profile.IsGun ? _gunLamp : _meleeLamp, color);

        // 총구 섬광은 총 모드에서만 나오므로, 총 모드일 때만 그 총의 색으로 맞춘다.
        if (profile.IsGun)
        {
            TintMuzzleFlash(color);
        }
    }

    private void TintMuzzleFlash(Color color)
    {
        if (_muzzleFlashLights != null)
        {
            foreach (Light light in _muzzleFlashLights)
            {
                if (light != null) light.color = color;
            }
        }

        if (_muzzleFlashRenderers == null) return;

        for (int i = 0; i < _muzzleFlashRenderers.Length; i++)
        {
            Renderer flash = _muzzleFlashRenderers[i];
            if (flash == null) continue;

            // 원래 색의 밝기(가장 큰 성분)와 투명도는 그대로 두고 색상만 바꾼다.
            Color baseTint = _flashBaseTints[i];
            float brightness = Mathf.Max(baseTint.r, Mathf.Max(baseTint.g, baseTint.b));

            flash.GetPropertyBlock(_block);
            _block.SetColor(TintColorId, new Color(color.r * brightness, color.g * brightness, color.b * brightness, baseTint.a));
            flash.SetPropertyBlock(_block);
        }
    }

    private void Tint(LampTarget lamp, Color color)
    {
        if (lamp == null) return;

        if (lamp.Lights != null)
        {
            foreach (Light light in lamp.Lights)
            {
                if (light != null) light.color = color;
            }
        }

        if (lamp.Halos != null)
        {
            foreach (ParticleSystemRenderer halo in lamp.Halos)
            {
                TintHalo(halo, color);
            }
        }

        // 공유 머티리얼은 건드리지 않고 이 렌더러에만 발광 색을 입힌다.
        if (lamp.Model != null)
        {
            lamp.Model.GetPropertyBlock(_block);
            _block.SetColor(EmissionColorId, color * _emissionIntensity);
            lamp.Model.SetPropertyBlock(_block);
        }
    }

    private void TintHalo(ParticleSystemRenderer halo, Color color)
    {
        if (halo == null) return;

        halo.GetPropertyBlock(_block);
        _block.SetColor(BaseColorId, color);
        halo.SetPropertyBlock(_block);
    }
}
