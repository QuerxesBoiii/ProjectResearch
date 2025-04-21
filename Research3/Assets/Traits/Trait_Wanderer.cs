using UnityEngine;

public class Trait_Wanderer : Trait
{
    public override int Id => 12; // Wandering ID from TraitManager.cs

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.wanderRadius *= 1.5f; // +50% wander radius
        creature.reproductionIntervalMultiplier *= 1.2f; // -20% reproduction frequency (longer interval)
        Debug.Log($"{creature.name}: Applied Wanderer trait");
    }
}