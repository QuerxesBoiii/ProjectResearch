using UnityEngine;

public class Trait_Scout : Trait
{
    public override int Id => 11;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.baseDetectionRadius *= 1.25f; // +25% detection radius
        creature.healthMultiplier *= 0.9f; // -10% health
    }
}