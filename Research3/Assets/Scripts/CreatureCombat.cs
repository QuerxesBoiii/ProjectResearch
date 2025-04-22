using UnityEngine;
using System.Collections;

public class CreatureCombat : MonoBehaviour
{
    private CreatureBehavior creatureBehavior;
    public float AttackRange => creatureBehavior.Size * 2f;

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

        float damage = creatureBehavior.Size * 2f; // Base damage

        // Berserker: +25% damage if health < 30%
        if (creatureBehavior.traitIds.Contains(14) && creatureBehavior.Health < creatureBehavior.Size * 10f * 0.3f)
        {
            damage *= 1.25f;
        }

        // Tactician: +25% damage if target's health % is lower
        if (creatureBehavior.traitIds.Contains(34))
        {
            float targetHealthPercent = target.Health / (target.Size * 10f);
            float ownHealthPercent = creatureBehavior.Health / (creatureBehavior.Size * 10f);
            if (targetHealthPercent < ownHealthPercent)
            {
                damage *= 1.25f;
            }
        }

        // Ambusher: +50% damage on first attack
        if (creatureBehavior.traitIds.Contains(36) && !target.lastAttackedBy)
        {
            damage *= 1.5f;
        }

        // Apply damage
        target.Health -= damage;

        // Spiky: Reflect 20% damage back
        if (target.traitIds.Contains(60))
        {
            creatureBehavior.Health -= damage * 0.2f;
            Debug.Log($"{target.name} (Spiky) reflected {damage * 0.2f} damage to {name}");
        }

        // Venomous: Apply poison (0.5 damage/s for 5s)
        if (creatureBehavior.traitIds.Contains(13))
        {
            target.StartCoroutine(ApplyPoison(target));
        }

        // Parasitic: Steal 1 food if target has food
        if (creatureBehavior.traitIds.Contains(29) && target.FoodLevel > 0)
        {
            target.foodLevel = Mathf.Max(target.foodLevel - 1, 0);
            creatureBehavior.foodLevel = Mathf.Min(creatureBehavior.foodLevel + 1, creatureBehavior.MaxFoodLevel);
            Debug.Log($"{name} (Parasitic) stole 1 food from {target.name}");
        }

        // TrapMaker: Immobilize target for 3s on first attack
        if (creatureBehavior.traitIds.Contains(64) && !target.lastAttackedBy)
        {
            target.immobilized = true;
            target.immobilizedTimer = 3f;
            Debug.Log($"{name} (TrapMaker) immobilized {target.name} for 3 seconds");
        }

        target.lastAttackedBy = creatureBehavior;

        // Burrower: Burrow if hit and not on cooldown
        if (target.traitIds.Contains(43) && !target.isBurrowing && target.burrowCooldown <= 0)
        {
            target.isBurrowing = true;
            target.burrowTimer = 10f;
            target.burrowCooldown = 60f;
            Debug.Log($"{target.name} (Burrower) is burrowing due to attack");
        }

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