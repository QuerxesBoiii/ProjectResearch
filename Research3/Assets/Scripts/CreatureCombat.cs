using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CreatureBehavior))]
public class CreatureCombat : MonoBehaviour
{
    public enum BehaviorStyle { Friendly, Neutral, Hunter }

    [Header("Combat Settings")]
    [SerializeField] public BehaviorStyle behaviorStyle = BehaviorStyle.Neutral;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackDamage = 1f;
    [SerializeField] private float attackCooldown = 1f;

    private CreatureBehavior creatureBehavior;
    private Transform currentTarget;
    private Transform currentAttacker;
    private float nextAttackTime = 0f;

    void Start()
    {
        creatureBehavior = GetComponent<CreatureBehavior>();
        if (!creatureBehavior) Debug.LogError($"{name}: CreatureCombat requires CreatureBehavior!");
    }

    public void OnAttacked(Transform attacker)
    {
        currentAttacker = attacker;
    }

    public void HandleCombatReactions()
    {
        if (currentAttacker == null || !currentAttacker.gameObject.activeSelf) return;

        if (creatureBehavior.currentState != CreatureBehavior.State.Attacking && 
            creatureBehavior.currentState != CreatureBehavior.State.Fleeing)
        {
            if (behaviorStyle == BehaviorStyle.Friendly)
            {
                creatureBehavior.currentState = CreatureBehavior.State.Fleeing;
                Debug.Log($"{name}: Fleeing from {currentAttacker.name}");
            }
            else // Neutral or Hunter
            {
                creatureBehavior.currentState = CreatureBehavior.State.Attacking;
                currentTarget = currentAttacker;
                Debug.Log($"{name}: Attacking back {currentTarget.name}");
            }
        }
    }

    public void PerformAttack()
    {
        if (currentTarget == null || !currentTarget.gameObject.activeSelf)
        {
            creatureBehavior.currentState = CreatureBehavior.State.Idle;
            currentTarget = null;
            Debug.Log($"{name}: Target gone, stopping attack");
            return;
        }

        float distance = Vector3.Distance(transform.position, currentTarget.position);
        if (distance > attackRange)
        {
            creatureBehavior.Agent.SetDestination(currentTarget.position);
        }
        else if (Time.time >= nextAttackTime)
        {
            if (currentTarget.TryGetComponent<CreatureBehavior>(out var target))
            {
                target.TakeDamage(attackDamage, transform);
                nextAttackTime = Time.time + attackCooldown;
                Debug.Log($"{name}: Dealt {attackDamage} damage to {currentTarget.name}");
            }
        }
    }

    public void PerformFlee()
    {
        if (currentAttacker == null || !currentAttacker.gameObject.activeSelf)
        {
            creatureBehavior.currentState = CreatureBehavior.State.Idle;
            currentAttacker = null;
            Debug.Log($"{name}: Attacker gone, stopping flee");
            return;
        }

        Vector3 fleeDirection = (transform.position - currentAttacker.position).normalized;
        Vector3 fleePosition = transform.position + fleeDirection * 10f;
        if (NavMesh.SamplePosition(fleePosition, out NavMeshHit hit, 10f, creatureBehavior.Agent.areaMask))
        {
            creatureBehavior.Agent.SetDestination(hit.position);
        }
    }

    public void SetAttackTarget(Transform target)
    {
        if (target == null || !target.gameObject.activeSelf) return;

        currentTarget = target;
        creatureBehavior.currentState = CreatureBehavior.State.Attacking;
        Debug.Log($"{name}: Hunting {target.name}");
    }
}