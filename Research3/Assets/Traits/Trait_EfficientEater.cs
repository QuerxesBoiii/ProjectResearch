using UnityEngine;

public class Trait_EfficientEater : Trait
{
    public override int Id => 5;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.hungerDecreaseIntervalMultiplier *= 1.3f; // +30% hunger interval (eat less often)
        creature.maxFoodLevelMultiplier *= 1.2f; // +20% max food
        creature.walkingSpeedMultiplier *= 0.9f; // -10% speed
        creature.reproductionCheckIntervalMultiplier *= 1.15f; // -15% reproduction frequency
    }
}