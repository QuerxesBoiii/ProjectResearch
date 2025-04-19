using UnityEngine;

public class Trait_Fortified : Trait
{
    public override int Id => 3;
    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.Health *= 1.25f;
        creature.WalkingSpeed *= 0.8f;
    }
}