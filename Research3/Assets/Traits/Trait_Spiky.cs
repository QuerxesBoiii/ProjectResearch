using UnityEngine;

public class Trait_Spiky : Trait
{
    public override int Id => 29;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.healthMultiplier *= 0.9f; // -10% health
        creature.walkingSpeedMultiplier *= 0.8f; // -20% walking speed
        creature.maxStamina *= 0.8f; // -20% max stamina
    }
}