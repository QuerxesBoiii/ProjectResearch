using UnityEngine;

public class Trait_Prolific : Trait
{
    public override int Id => 16;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.healthMultiplier *= 0.9f; // -10% health
    }
}