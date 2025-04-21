using UnityEngine;

public class Trait_Fertile : Trait
{
    public override int Id => 7;
    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.reproductionChanceMultiplier = 1.3f; // +30% reproduction chance
        creature.reproductionIntervalMultiplier = 0.7f; // -30% reproduction interval
        creature.healthMultiplier *= 0.9f; // -10% health
        Debug.Log($"{creature.name}: Applied Fertile trait");
    }
}