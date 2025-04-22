using UnityEngine;

public class Trait_Immortal : Trait
{
    public override int Id => 6;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.MaxAge *= 2f; // +100% lifespan
        creature.healthMultiplier *= 1.2f; // +20% health
        creature.reproductionCheckIntervalMultiplier *= 1.5f; // -50% reproduction frequency
        creature.hungerDecreaseIntervalMultiplier *= 0.9f; // -10% hunger interval
    }
}