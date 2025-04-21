using UnityEngine;

public class Trait_Fortified : Trait
{
    public override int Id => 3; // Fortified ID from TraitManager.cs

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.healthMultiplier *= 1.5f; // +50% health
        creature.sizeMultiplier *= 1.1f; // +10% size
        creature.walkingSpeedMultiplier *= 0.85f; // -15% speed
        creature.reproductionIntervalMultiplier *= 1.2f; // -20% reproduction frequency (longer interval)
        Debug.Log($"{creature.name}: Applied Fortified trait");
    }
}