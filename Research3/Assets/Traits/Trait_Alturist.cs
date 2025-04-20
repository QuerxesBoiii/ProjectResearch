using UnityEngine;

public class Trait_Altruist : Trait
{
    public override int Id => 17;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.maxFoodLevelMultiplier *= 0.8f; // -20% max food
    }
}