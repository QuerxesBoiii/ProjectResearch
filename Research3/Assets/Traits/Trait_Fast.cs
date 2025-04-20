using UnityEngine;

public class Trait_Fast : Trait
{
    public override int Id => 4;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.walkingSpeedMultiplier *= 1.25f; // +25% speed
        creature.hungerDecreaseIntervalMultiplier *= 0.9f; // -10% hunger interval (eat more often)
        creature.healthMultiplier *= 0.9f; // -10% health
    }
}