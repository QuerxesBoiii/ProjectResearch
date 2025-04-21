using UnityEngine;

public class Trait_Big : Trait
{
    public override int Id => 2;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.sizeMultiplier *= 1.5f;
        creature.healthMultiplier *= 1.25f;
        creature.walkingSpeedMultiplier *= 0.9f;
        creature.maxFoodLevelMultiplier *= 1.25f;
        creature.adultAge *= 1.2f; // 12f
        creature.reproductionCostMultiplier *= 1.2f; // 1.2x cost
        Debug.Log($"{creature.name}: Applied Big trait - Adult Age: {creature.adultAge}, Reproduction Cost Multiplier: {creature.reproductionCostMultiplier}");
    }
}