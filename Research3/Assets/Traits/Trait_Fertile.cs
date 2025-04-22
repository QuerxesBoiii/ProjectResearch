using UnityEngine;

public class Trait_Fertile : Trait
{
    public override int Id => 7;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.reproductionChanceMultiplier *= 2f;
        creature.reproductionIntervalMultiplier *= 0.5f;
        Debug.Log($"{creature.name}: Applied Fertile trait - Reproduction chance doubled, interval halved.");
    }
}