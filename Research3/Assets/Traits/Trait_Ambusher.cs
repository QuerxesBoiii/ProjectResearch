using UnityEngine;

public class Trait_Ambusher : Trait
{
    public override int Id => 23;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        // Attack speed handled in CreatureBehavior.attackInterval
    }
}