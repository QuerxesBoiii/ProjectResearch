using UnityEngine;

public class Trait_SwiftBreeder : Trait
{
    public override int Id => 24;
    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.reproductionCostMultiplier = 0.8f; // -20% reproduction cost
        creature.healthMultiplier *= 0.9f; // -10% health
        Debug.Log($"{creature.name}: Applied Swift Breeder trait");
    }
}