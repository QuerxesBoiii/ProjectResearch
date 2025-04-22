using UnityEngine;

public class Trait_Small : Trait
{
    public override int Id => 1;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.sizeMultiplier *= 0.5f;
        creature.walkingSpeedMultiplier *= 1.2f;
        creature.maxFoodLevelMultiplier *= 0.5f;
        Debug.Log($"{creature.name}: Applied Small trait - Size halved, Speed +20%, Food capacity halved.");
    }
}