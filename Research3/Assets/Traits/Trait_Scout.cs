using UnityEngine;

public class Trait_Scout : Trait
{
    public override int Id => 11;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.baseDetectionRadius *= 1.5f;
        Debug.Log($"{creature.name}: Applied Scout trait - Detection radius +50%.");
    }
}