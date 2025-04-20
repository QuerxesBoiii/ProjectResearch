using UnityEngine;

public class Trait_Migratory : Trait
{
    public override int Id => 21;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.hungerDecreaseIntervalMultiplier *= 0.95f; // +5% hunger rate
    }
}