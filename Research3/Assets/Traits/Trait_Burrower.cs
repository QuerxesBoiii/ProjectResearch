using UnityEngine;

public class Trait_Burrower : Trait
{
    public override int Id => 25;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.walkingSpeedMultiplier *= 0.9f; // -10% speed
    }
}