using UnityEngine;
using System.Collections.Generic;
using System.Collections;
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
    [Tooltip("기본 이동 속도 (타일/초 기준)")]
    public float moveSpeed = 2f;    // 타입별로 프리팹에서 값만 바꿔주면 됩니다.
    
    // 이동/공격은 3주차에서 구현 예정
    // 여기서는 체력만 세팅해 둔다.
    public void Initialize(float hpMultiplier)
    {
        currentHp = baseHp * hpMultiplier;
    }
    // 활성 몬스터 목록 (ArrowTower가 탐색할 때 사용)
    public static readonly List<Monster> ActiveMonsters = new List<Monster>();

    private void OnEnable()
    {
        if (!ActiveMonsters.Contains(this))
            ActiveMonsters.Add(this);
    }

    private void OnDisable()
    {
        ActiveMonsters.Remove(this);
    }

    // 데미지 처리
    public void TakeDamage(float dmg)
    {
        currentHp -= dmg;
        Debug.Log($"[Monster] {type} 데미지: {dmg}, 남은 HP: {currentHp}");

        if (currentHp <= 0)
            Die();
    }

    private void Die()
    {
        Debug.Log($"[Monster] {type} 사망!");
        Destroy(gameObject);
    }

}
