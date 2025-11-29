using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 화살 타워 전용 공격 스크립트
/// - 주기적으로 범위 내 가장 가까운 몬스터에게 데미지
/// - 몬스터 목록은 Monster.ActiveMonsters 를 사용
/// </summary>
public class ArrowTower : MonoBehaviour
{
    [Header("Attack Settings")]
    [Tooltip("타워의 공격력 (한 번 공격당 피해량)")]
    [SerializeField] private float attackDamage = 3f;

    [Tooltip("공격 간격(초). 0.1초면 초당 10회 공격")]
    [SerializeField] private float attackInterval = 0.1f;

    [Tooltip("공격 가능 범위 (타일 기준 반지름 8칸)")]
    [SerializeField] private float attackRangeTiles = 8f;

    // 내부에서 사용할 공격 쿨타임 타이머
    private float attackTimer = 0f;

    private void Update()
    {
        // 현재 씬에 몬스터가 아예 없으면 굳이 타이머를 돌릴 필요가 없음
        if (Monster.ActiveMonsters.Count == 0)
            return;

        attackTimer += Time.deltaTime;
        if (attackTimer < attackInterval)
            return;

        attackTimer = 0f;
        TryAttack();
    }

    /// <summary>
    /// 범위 내에서 가장 가까운 몬스터를 찾아 공격
    /// </summary>
    private void TryAttack()
    {
        // 타일 크기가 1이라고 가정하고, 범위는 그대로 월드 거리로 사용
        float range = attackRangeTiles;
        float rangeSqr = range * range;

        Vector3 myPos = transform.position;
        Monster nearest = null;
        float nearestDistSqr = float.MaxValue;

        // 활성화된 모든 몬스터를 순회하며 가장 가까운 대상 탐색
        List<Monster> monsters = Monster.ActiveMonsters;
        for (int i = 0; i < monsters.Count; i++)
        {
            Monster m = monsters[i];
            if (m == null) continue;
            if (!m.isActiveAndEnabled) continue;

            Vector3 diff = m.transform.position - myPos;
            float distSqr = diff.sqrMagnitude;

            // 사거리 밖이면 무시
            if (distSqr > rangeSqr)
                continue;

            // 가장 가까운 몬스터 갱신
            if (distSqr < nearestDistSqr)
            {
                nearestDistSqr = distSqr;
                nearest = m;
            }
        }

        if (nearest == null)
            return;

        // 실제 데미지 적용
        nearest.TakeDamage(attackDamage);
        Debug.Log($"[ArrowTower] {nearest.name}에게 {attackDamage} 피해 (거리^2={nearestDistSqr})");
    }
}
