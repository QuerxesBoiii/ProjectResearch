using UnityEngine;

public class Trait_Hunter : Trait
{
    public override int Id => 8;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.combatBehavior = CreatureBehavior.CombatBehavior.Hunter; // Enable hunting
        creature.walkingSpeedMultiplier *= 1.2f; // +20% speed
        creature.hungerDecreaseIntervalMultiplier *= 0.8f; // -20% hunger interval
        creature.maxFoodLevelMultiplier *= 0.9f; // -10% max food
        creature.maxStamina *= 0.75f; // -25% max stamina
        creature.staminaRegenRate *= 0.9f; // -10% stamina regen
    }
}