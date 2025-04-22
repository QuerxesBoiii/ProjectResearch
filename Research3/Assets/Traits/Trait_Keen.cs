using UnityEngine;

public class Trait_Keen : Trait
{
    public override int Id => 28;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        // Food detection radius increase is handled in CreatureBehavior's GetFoodDetectionRadius
        Debug.Log($"{creature.name}: Applied Keen trait - Food detection radius +25%.");
    }
}