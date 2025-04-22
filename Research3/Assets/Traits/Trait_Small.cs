using UnityEngine;

public class Trait_Small : Trait
{
    public override int Id => 1;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.sizeMultiplier *= 0.75f; // +25% smaller size
        creature.walkingSpeedMultiplier *= 1.15f; // +15% speed
        creature.healthMultiplier *= 0.9f; // -10% health
        creature.maxFoodLevelMultiplier *= 0.8f; // -20% max food
    }
}