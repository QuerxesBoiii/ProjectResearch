using UnityEngine;

public class Trait_Big : Trait
{
    public override int Id => 2;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.sizeMultiplier *= 1.5f;
        creature.walkingSpeedMultiplier *= 0.75f;
        creature.maxFoodLevelMultiplier *= 1.5f;
        Debug.Log($"{creature.name}: Applied Big trait - Size +50%, Speed -20%, Food capacity +50%.");
    }
}