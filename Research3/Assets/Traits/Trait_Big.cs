using UnityEngine;

public class Trait_Big : Trait
{
    public override int Id => 0;
    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.Size *= 1.5f; // Increase size
    }
}