using UnityEngine;

public class CreatureCombat : MonoBehaviour
{
    private CreatureBehavior creatureBehavior;
    private float baseAttackDamage = 2f;
    private float attackDamageMultiplier = 1f;
    private float baseAttackRange = 2f;
    private float attackRangeMultiplier = 1f;
    private float baseAttackSpeed = 1f;
    private float attackSpeedMultiplier = 1f;

    public float AttackDamage => baseAttackDamage * attackDamageMultiplier;
    public float AttackRange => baseAttackRange * attackRangeMultiplier;
    public float AttackSpeed => baseAttackSpeed * attackSpeedMultiplier;

    void Start()
    {
        creatureBehavior = GetComponent<CreatureBehavior>();
        if (!creatureBehavior)
        {
            Debug.LogError($"{name}: No CreatureBehavior component found!");
            enabled = false;
            return;
        }

        // Apply trait effects for combat stats
        if (creatureBehavior.traitIds.Contains(8)) // Hunter trait
        {
            attackDamageMultiplier *= 1.5f;
            attackSpeedMultiplier *= 1.2f;
        }
        if (creatureBehavior.traitIds.Contains(14)) // Berserker trait
        {
            attackDamageMultiplier *= 1.5f;
            attackSpeedMultiplier *= 1.3f;
        }
    }

    public void Attack(CreatureBehavior target)
    {
        if (target == null || target.currentState == CreatureBehavior.State.Dead) return;

        float damage = AttackDamage;
        float defenseReduction = target.Defense * 0.1f; // Now uses target.Defense from CreatureBehavior
        float finalDamage = Mathf.Max(1, damage - defenseReduction);

        // Apply trait effects
        if (creatureBehavior.traitIds.Contains(13)) // Venomous trait
        {
            target.Poisoned = true;
            Debug.Log($"{name} applied poison to {target.name}");
        }

        target.Health -= finalDamage;
        target.lastAttackedBy = creatureBehavior;
        Debug.Log($"{name} attacked {target.name} for {finalDamage} damage. Target health: {target.Health}");

        creatureBehavior.UpdateTextDisplay();
        target.UpdateTextDisplay();

        if (target.Health <= 0)
        {
            StartCoroutine(target.DieWithRotation());
        }

        // Check for retaliation (Spiky trait)
        if (target.traitIds.Contains(29)) // Spiky trait
        {
            float spikyDamage = finalDamage * 0.3f;
            creatureBehavior.Health -= spikyDamage;
            Debug.Log($"{target.name} (Spiky) retaliated, dealing {spikyDamage} damage to {name}. Attacker health: {creatureBehavior.Health}");
            creatureBehavior.UpdateTextDisplay();

            if (creatureBehavior.Health <= 0)
            {
                StartCoroutine(creatureBehavior.DieWithRotation());
            }
        }
    }
}