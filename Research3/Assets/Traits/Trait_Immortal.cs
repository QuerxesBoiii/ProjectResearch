using UnityEngine;

public class Trait_Immortal : Trait
{
    public override int Id => 5;
    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.MaxAge *= 3f; // Longer lifespan
        creature.ReproductionCheckInterval *= 3f; // Slower reproduction checks
    }
}