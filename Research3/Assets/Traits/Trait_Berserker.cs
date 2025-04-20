using UnityEngine;

public class Trait_Berserker : Trait
{
    public override int Id => 14;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.maxFoodLevelMultiplier *= 0.9f; // -10% max food
    }
}