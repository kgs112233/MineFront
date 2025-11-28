using UnityEngine;

// 몬스터 종류
public enum MonsterType
{
    Normal,  // 일반형
    Fast,    // 속행형
    Tank     // 단단형
}

// 몬스터 기본 정보와 체력 스케일링만 담당하는 간단한 스크립트
public class Monster : MonoBehaviour
{
    [Header("Basic Info")]
    public MonsterType type;

    [Tooltip("기본 체력(스케일링 전)")]
    public float baseHp = 3f;

    [Header("Runtime State")]
    public float currentHp;

    // 이동/공격은 3주차에서 구현 예정
    // 여기서는 체력만 세팅해 둔다.
    public void Initialize(float hpMultiplier)
    {
        currentHp = baseHp * hpMultiplier;
    }
}
