using UnityEngine;

public class Trait_Wanderer : Trait
{
    public override int Id => 12;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.wanderRadius *= 1.5f; // +50% wander radius
        creature.reproductionCheckIntervalMultiplier *= 1.2f; // -20% reproduction frequency
    }
}