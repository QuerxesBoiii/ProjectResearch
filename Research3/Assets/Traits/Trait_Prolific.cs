using UnityEngine;

public class Trait_Prolific : Trait
{
    public override int Id => 16;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.TwinChance += 0.1f;
        creature.TripletChance += 0.01f;
        Debug.Log($"{creature.name}: Applied Prolific trait - Twin chance +10%, Triplet chance +1%.");
    }
}