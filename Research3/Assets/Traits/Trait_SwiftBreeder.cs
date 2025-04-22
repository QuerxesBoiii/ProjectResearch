using UnityEngine;

public class Trait_SwiftBreeder : Trait
{
    public override int Id => 24;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.healthMultiplier *= 0.9f; // -10% health
    }
}