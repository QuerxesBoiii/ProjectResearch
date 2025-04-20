using UnityEngine;

public class Trait_Glutton : Trait
{
    public override int Id => 15;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.maxFoodLevelMultiplier *= 1.25f; // +25% max food
        creature.healthMultiplier *= 1.1f; // +10% health
        creature.hungerDecreaseIntervalMultiplier *= 0.8f; // -20% hunger interval
        creature.walkingSpeedMultiplier *= 0.95f; // -5% speed
        creature.sizeMultiplier *= 1.05f; // +5% size
    }
}