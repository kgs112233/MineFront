using System.Collections.Generic;
using UnityEngine;

public class BarracksUnit : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float arriveThreshold = 0.05f;

    [Header("Combat")]
    [SerializeField] private float attackDamage = 1f;
    [SerializeField] private float attackInterval = 0.5f;
    private float attackTimer = 0f;

    private TileManager tileManager;
    private Vector3 homePos;
    private float detectRangeTiles;

    private Monster target;

    private bool initialized = false;

    public void Initialize(TileManager manager, Vector2Int tilePos, float rangeTiles)
    {
        tileManager = manager;
        detectRangeTiles = rangeTiles;

        homePos = tileManager.tileView.GetTileWorldPosition(tilePos);
        homePos.z = -2;

        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
            return;

        if (target == null || !target.isActiveAndEnabled)
            FindTarget();

        if (target != null)
            MoveAndAttack();
        else
            ReturnToHome();
    }

    private void FindTarget()
    {
        float rangeSqr = detectRangeTiles * detectRangeTiles;
        Monster nearest = null;
        float nearestDistSqr = float.MaxValue;

        foreach (var m in Monster.ActiveMonsters)
        {
            if (m == null || !m.isActiveAndEnabled)
                continue;

            float distSqr = (m.transform.position - homePos).sqrMagnitude;
            if (distSqr > rangeSqr)
                continue;

            if (distSqr < nearestDistSqr)
            {
                nearest = m;
                nearestDistSqr = distSqr;
            }
        }

        target = nearest;
    }

    private void MoveAndAttack()
    {
        Vector3 pos = transform.position;
        Vector3 tpos = target.transform.position;

        float dist = Vector3.Distance(pos, tpos);

        // 공격 거리
        if (dist <= 0.2f)
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackInterval)
            {
                attackTimer = 0f;
                target.TakeDamage(attackDamage);
                Debug.Log($"[BarracksUnit] {target.name} 공격!");
            }
            return;
        }

        // 직선 이동
        Vector3 dir = (tpos - pos).normalized;
        Vector3 next = pos + dir * moveSpeed * Time.deltaTime;

        // 이동 불가 타일 통과 금지
        if (!tileManager.IsWalkableWorldPosition(next))
            return;

        transform.position = next;
    }

    private void ReturnToHome()
    {
        Vector3 pos = transform.position;
        Vector3 dir = homePos - pos;
        float dist = dir.magnitude;

        if (dist <= arriveThreshold)
            return;

        Vector3 next = pos + dir.normalized * moveSpeed * Time.deltaTime;

        if (!tileManager.IsWalkableWorldPosition(next))
            return;

        transform.position = next;
    }
}
