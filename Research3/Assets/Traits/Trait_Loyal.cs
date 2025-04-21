using UnityEngine;

public class Trait_Loyal : Trait
{
    public override int Id => 33;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        // Increase reproduction chance by 100%
        creature.reproductionChanceMultiplier *= 2f;
    }
}