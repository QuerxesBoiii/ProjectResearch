using UnityEngine;

public class Trait_Cannibal : Trait
{
    public override int Id => 26;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.reproductionCheckIntervalMultiplier *= 1.1f; // -10% reproduction frequency
    }
}