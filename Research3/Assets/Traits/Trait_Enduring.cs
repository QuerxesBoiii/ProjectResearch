using UnityEngine;

public class Trait_Enduring : Trait
{
    public override int Id => 18;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.maxStamina *= 1.5f; // +50% max stamina
        creature.staminaRegenRate *= 1.1f; // +10% stamina regen
        creature.walkingSpeedMultiplier *= 0.8f; // -20% walking speed
    }
}