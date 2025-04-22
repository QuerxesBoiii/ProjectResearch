using UnityEngine;

public class Trait_Giant : Trait
{
    public override int Id => 31;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.sizeMultiplier *= 3f;
        creature.walkingSpeedMultiplier *= 0.6f;
        creature.maxFoodLevelMultiplier *= 3f;
        creature.healthMultiplier *= 2f;
        Debug.Log($"{creature.name}: Applied Giant trait - Size x3, Speed -50%, Food capacity x3, Health doubled.");
    }
}