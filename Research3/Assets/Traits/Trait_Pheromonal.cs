using UnityEngine;

public class Trait_Pheromonal : Trait
{
    public override int Id => 27;
    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.mateDetectionRadiusMultiplier = 2.0f; // +33% mate detection range
        creature.maxFoodLevelMultiplier *= 0.9f; // -10% max food
        Debug.Log($"{creature.name}: Applied Pheromonal trait");
    }
}