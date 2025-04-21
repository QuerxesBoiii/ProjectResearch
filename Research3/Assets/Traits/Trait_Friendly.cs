using UnityEngine;

public class Trait_Friendly : Trait
{
    public override int Id => 9; // Friendly ID from TraitManager.cs

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.combatBehavior = CreatureBehavior.CombatBehavior.Friendly; // Set to Friendly
        creature.reproductionIntervalMultiplier *= 0.8f; // +20% reproduction frequency (shorter interval)
        creature.healthMultiplier *= 0.9f; // -10% health
        creature.maxStamina *= 1.25f; // +25% max stamina
        Debug.Log($"{creature.name}: Applied Friendly trait");
    }
}