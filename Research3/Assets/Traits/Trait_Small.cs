using UnityEngine;

public class Trait_Small : Trait
{
    public override int Id => 1;
    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.Size *= 0.75f; // Decrease size
    }
}