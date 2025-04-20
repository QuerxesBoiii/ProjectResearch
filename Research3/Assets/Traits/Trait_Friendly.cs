using UnityEngine;

public class Trait_Friendly : Trait
{
    public override int Id => 9;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.combatBehavior = CreatureBehavior.CombatBehavior.Friendly; // Enable fleeing
        creature.reproductionCheckIntervalMultiplier *= 0.8f; // +20% reproduction frequency
        creature.healthMultiplier *= 0.9f; // -10% health
        creature.maxStamina *= 1.25f; // +25% max stamina
    }
}