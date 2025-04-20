using UnityEngine;

public class Trait_Climber : Trait
{
    public override int Id => 10;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.canClimb = true; // Enable climbing
        creature.walkingSpeedMultiplier *= 0.95f; // -5% speed
    }
}