using UnityEngine;

public class Trait_Prolific : Trait
{
    public override int Id => 16;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.TwinChance = 0.1f; // 10% chance for twins
        creature.TripletChance = 0.015f; // 1.5% chance for triplets
        Debug.Log($"{creature.name}: Applied Prolific trait - Twin Chance: {creature.TwinChance}, Triplet Chance: {creature.TripletChance}");
    }
}