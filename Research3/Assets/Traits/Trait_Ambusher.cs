using UnityEngine;

public class Trait_Ambusher : Trait
{
    public override int Id => 23;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        // Faster attack interval is handled in CreatureBehavior's attackInterval
        Debug.Log($"{creature.name}: Applied Ambush trait - Attack interval reduced by 20%.");
    }
}