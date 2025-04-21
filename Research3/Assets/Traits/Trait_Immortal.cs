using UnityEngine;

public class Trait_Immortal : Trait
{
    public override int Id => 6;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        // Set maxAge to 9 * adultAge
        creature.maxAge = 9 * creature.adultAge;

        // Increase reproduction cost by 50%
        creature.reproductionCostMultiplier *= 1.5f;
    }
}