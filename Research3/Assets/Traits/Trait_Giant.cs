using UnityEngine;

public class Trait_Giant : Trait
{
    public override int Id => 31;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.sizeMultiplier *= 2f; // 100% bigger
        creature.healthMultiplier *= 1.75f; // 75% more health
        creature.maxFoodLevelMultiplier *= 1.5f; // 50% more max food
        creature.walkingSpeedMultiplier *= 0.7f; // -30% speed
        creature.hungerDecreaseIntervalMultiplier *= 0.75f; // Faster hunger
        creature.reproductionCostMultiplier *= 2f; // 2x cost
        Debug.Log($"{creature.name}: Applied Giant trait");
    }
}