using UnityEngine;

public class Trait_TrapMaker : Trait
{
    public override int Id => 30;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.walkingSpeedMultiplier *= 0.85f; // -15% speed
    }
}