using UnityEngine;

public class Trait_Fast : Trait
{
    public override int Id => 2;
    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.WalkingSpeed *= 1.25f; // Increase speed
    }
}