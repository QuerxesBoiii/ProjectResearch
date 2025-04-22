using UnityEngine;
using System.Collections;

public class CreatureCombat : MonoBehaviour
{
    private CreatureBehavior creatureBehavior;
    public float AttackRange => creatureBehavior.Size * 2f;
    public float attackDamageMultiplier = 1f; // Added for Berserker and Tactician
    public bool canPoison = false; // Added for Venomous
    public bool canReflectDamage = false; // Added for Spiky
    public bool canImmobilize = false; // Added for TrapMaker

    void Start()
    {
        creatureBehavior = GetComponent<CreatureBehavior>();
        if (!creatureBehavior) { Debug.LogError($"{name}: No CreatureBehavior!"); }
    }

    public void Attack(CreatureBehavior target)
    {
        if (target == null || target.currentState == CreatureBehavior.State.Dead) return;

        // Friendly creatures only attack if previously attacked
        if (creatureBehavior.traitIds.Contains(9) && creatureBehavior.lastAttackedBy == null)
        {
            return;
        }

        float damage = creatureBehavior.Size * 2f * attackDamageMultiplier; // Base damage with multiplier

        // Apply damage
        target.Health -= damage;

        // Spiky: Reflect 20% damage back
        if (target.traitIds.Contains(29) && canReflectDamage)
        {
            creatureBehavior.Health -= damage * 0.2f;
            Debug.Log($"{target.name} (Spiky) reflected {damage * 0.2f} damage to {name}");
        }

        // Venomous: Apply poison (0.5 damage/s for 5s)
        if (creatureBehavior.traitIds.Contains(13) && canPoison)
        {
            target.StartCoroutine(ApplyPoison(target));
        }

        // Parasitic: Steal 1 food if target has food
        if (creatureBehavior.traitIds.Contains(20) && target.FoodLevel > 0)
        {
            target.foodLevel = Mathf.Max(target.foodLevel - 1, 0);
            creatureBehavior.foodLevel = Mathf.Min(creatureBehavior.foodLevel + 1, creatureBehavior.MaxFoodLevel);
            Debug.Log($"{name} (Parasitic) stole 1 food from {target.name}");
        }

        // TrapMaker: Immobilize target for 3s on first attack
        if (creatureBehavior.traitIds.Contains(30) && canImmobilize && !target.lastAttackedBy)
        {
            target.immobilized = true;
            target.immobilizedTimer = 3f;
            Debug.Log($"{name} (TrapMaker) immobilized {target.name} for 3 seconds");
        }

        target.lastAttackedBy = creatureBehavior;

        target.UpdateTextDisplay();
        creatureBehavior.UpdateTextDisplay();

        if (target.Health <= 0)
        {
            StartCoroutine(target.DieWithRotation());
        }
    }

    private IEnumerator ApplyPoison(CreatureBehavior target)
    {
        float duration = 5f;
        float elapsed = 0f;
        while (elapsed < duration && target.currentState != CreatureBehavior.State.Dead)
        {
            target.Health -= 0.5f;
            target.UpdateTextDisplay();
            if (target.Health <= 0)
            {
                StartCoroutine(target.DieWithRotation());
                yield break;
            }
            elapsed += 1f;
            yield return new WaitForSeconds(1f);
        }
    }
}