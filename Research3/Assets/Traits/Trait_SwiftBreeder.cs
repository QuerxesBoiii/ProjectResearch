using UnityEngine;

public class Trait_SwiftBreeder : Trait
{
    public override int Id => 24;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.reproductionCostMultiplier *= 0.5f;
        Debug.Log($"{creature.name}: Applied Swift Breeder trait - Reproduction cost halved.");
    }
}