using UnityEngine;

public class Trait_EfficientEater : Trait
{
    public override int Id => 5; // Efficient Eater ID from TraitManager.cs

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.hungerDecreaseIntervalMultiplier *= 1.3f; // +30% hunger interval
        creature.maxFoodLevelMultiplier *= 1.2f; // +20% max food
        creature.walkingSpeedMultiplier *= 0.9f; // -10% speed
        creature.reproductionIntervalMultiplier *= 1.15f; // -15% reproduction frequency (longer interval)
        Debug.Log($"{creature.name}: Applied Efficient Eater trait");
    }
}