using UnityEngine;

public class Trait_Fast : Trait
{
    public override int Id => 4;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.walkingSpeedMultiplier *= 1.5f;
        Debug.Log($"{creature.name}: Applied Fast trait - Speed +50%.");
    }
}