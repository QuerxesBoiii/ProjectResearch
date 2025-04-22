using UnityEngine;

public class Trait_Wanderer : Trait
{
    public override int Id => 12;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.wanderRadius *= 2f;
        Debug.Log($"{creature.name}: Applied Wandering trait - Wander radius doubled.");
    }
}