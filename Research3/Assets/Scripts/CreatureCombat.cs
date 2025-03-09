using UnityEngine;

// Code is using Unity 6 (6000.0.37f1)
// This script manages combat behavior for creatures, including attacking and fleeing.
// Code should work with multiplayer netcode for gameobjects

public class CreatureCombat : MonoBehaviour
{
    // ---- Combat Behavior ----
    public enum CombatBehavior { Friendly, Neutral, Hunter }
    [Header("Combat Behavior")]
    [SerializeField] private CombatBehavior combatBehavior = CombatBehavior.Neutral;

    // Public property for accessibility by CreatureBehavior, renamed to avoid conflict with enum
    public CombatBehavior CurrentCombatBehavior => combatBehavior;

    // ---- Combat Stats ----
    [Header("Combat Stats")]
    [SerializeField] private float attackRange = 2f;       // Distance within which creature can attack
    [SerializeField] private float attackDamage = 5f;     // Damage dealt per attack
    [SerializeField] private float attackInterval = 1f;   // Time between attacks
    private float lastAttackTime = 0f;

    private CreatureBehavior behavior;

    void Start()
    {
        behavior = GetComponent<CreatureBehavior>();
        if (!behavior) Debug.LogError($"{name}: No CreatureBehavior component!");
    }

    // Updates attacking behavior: moves to target and attacks if in range
    public void AttackUpdate()
    {
        if (behavior.AttackTarget == null || behavior.AttackTarget.GetComponent<CreatureBehavior>()?.CurrentState == CreatureBehavior.State.Dead)
        {
            behavior.CurrentState = CreatureBehavior.State.Idle;
            behavior.AttackTarget = null;
            return;
        }

        float distance = Vector3.Distance(behavior.transform.position, behavior.AttackTarget.position);
        if (distance > attackRange)
        {
            behavior.Agent.SetDestination(behavior.AttackTarget.position);
        }
        else if (Time.time - lastAttackTime >= attackInterval)
        {
            CreatureBehavior target = behavior.AttackTarget.GetComponent<CreatureBehavior>();
            if (target != null)
            {
                target.TakeDamage(attackDamage, behavior);
                lastAttackTime = Time.time;
                Debug.Log($"{name}: Attacked {target.name} for {attackDamage} damage");
            }
        }
    }

    // Updates fleeing behavior: moves away from threat
    public void FleeUpdate()
    {
        if (behavior.FleeFrom == null)
        {
            behavior.CurrentState = CreatureBehavior.State.Idle;
            behavior.FleeFrom = null;
            return;
        }

        Vector3 direction = (behavior.transform.position - behavior.FleeFrom.position).normalized;
        Vector3 fleePosition = behavior.transform.position + direction * 10f;
        if (UnityEngine.AI.NavMesh.SamplePosition(fleePosition, out UnityEngine.AI.NavMeshHit hit, 10f, behavior.Agent.areaMask))
        {
            behavior.Agent.SetDestination(hit.position);
        }
    }

    // Called when creature is attacked, determines response based on behavior
    public void OnAttacked(CreatureBehavior attacker)
    {
        if (behavior.CurrentState == CreatureBehavior.State.Dead) return;

        switch (combatBehavior)
        {
            case CombatBehavior.Friendly:
                behavior.FleeFrom = attacker.transform;
                behavior.CurrentState = CreatureBehavior.State.Fleeing;
                Debug.Log($"{name}: Fleeing from {attacker.name}");
                break;
            case CombatBehavior.Neutral:
            case CombatBehavior.Hunter:
                behavior.AttackTarget = attacker.transform;
                behavior.CurrentState = CreatureBehavior.State.Attacking;
                Debug.Log($"{name}: Retaliating against {attacker.name}");
                break;
        }
    }
}