using UnityEngine;

public class Trait_Big : Trait
{
    public override int Id => 2;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.sizeMultiplier *= 1.25f; // +25% larger size
        creature.healthMultiplier *= 1.25f; // +25% health
        creature.maxFoodLevelMultiplier *= 1.2f; // +20% max food
        creature.walkingSpeedMultiplier *= 0.9f; // -10% speed
        creature.hungerDecreaseIntervalMultiplier *= 0.9f; // -10% hunger interval (eat more often)
    }
}