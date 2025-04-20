using UnityEngine;

public class Trait_Fertile : Trait
{
    public override int Id => 7;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.reproductionCheckIntervalMultiplier *= 0.7f; // +30% reproduction frequency
        creature.healthMultiplier *= 0.9f; // -10% health
    }
}