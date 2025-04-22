using UnityEngine;

public class Trait_Migratory : Trait
{
    public override int Id => 21;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        // Migratory wandering is handled in CreatureBehavior's WanderMigratory
        Debug.Log($"{creature.name}: Applied Migratory trait - Moves to new locations periodically.");
    }
}