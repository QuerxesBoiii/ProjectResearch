using UnityEngine;

public class Trait_Fertile : Trait
{
    public override int Id => 7;
    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.ReproductionCheckInterval *= 0.666f; // Faster reproduction
    }
}