using UnityEngine;

public class Trait_Small : Trait
{
    public override int Id => 1;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.sizeMultiplier *= 0.75f;
        creature.healthMultiplier *= 0.75f;
        creature.walkingSpeedMultiplier *= 1.15f;
        creature.maxFoodLevelMultiplier *= 0.75f;
        creature.adultAge *= 0.8f; // 8f
        creature.reproductionCostMultiplier *= 0.8f; // 0.8x cost
        Debug.Log($"{creature.name}: Applied Small trait - Adult Age: {creature.adultAge}, Reproduction Cost Multiplier: {creature.reproductionCostMultiplier}");
    }
}