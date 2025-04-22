using UnityEngine;

public class Trait_Pheromonal : Trait
{
    public override int Id => 27;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.mateDetectionRadiusMultiplier *= 2f;
        Debug.Log($"{creature.name}: Applied Pheromonal trait - Mate detection radius doubled.");
    }
}