using UnityEngine;

public class Trait_Pheromonal : Trait
{
    public override int Id => 27;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.maxFoodLevelMultiplier *= 0.9f; // -10% max food
    }
}