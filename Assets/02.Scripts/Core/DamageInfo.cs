using UnityEngine;

public struct DamageInfo
{
    public float Value;            // 대미지 양
    public Vector3 HitPoint;       // 맞은 자리
    public Vector3 HitNormal;      // 맞은 자리가 수직으로 바라보는 방향
    public bool IsWeakPointHit;    // 약점에 맞았는지

    public DamageInfo(float value, Vector3 hitPoint, Vector3 hitNormal, bool isWeakPointHit = false)
    {
        Value = value;
        HitPoint = hitPoint;
        HitNormal = hitNormal;
        IsWeakPointHit = isWeakPointHit;
    }
}