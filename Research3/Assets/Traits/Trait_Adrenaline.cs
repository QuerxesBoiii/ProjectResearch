using UnityEngine;

public class Trait_Adrenaline : Trait
{
    public override int Id => 19;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        creature.staminaRegenRate *= 0.8f; // -20% stamina regen
    }
}