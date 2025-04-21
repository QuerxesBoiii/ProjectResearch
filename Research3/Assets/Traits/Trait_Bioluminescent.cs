using UnityEngine;

public class Trait_Bioluminescent : Trait
{
    public override int Id => 32;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        if (creature.bioluminescentLight != null)
        {
            creature.bioluminescentLight.enabled = true;
            creature.bioluminescentLight.color = creature.typeColor;
        }
        else
        {
            Debug.LogWarning($"{creature.name}: Bioluminescent trait applied but bioluminescentLight is not assigned!");
        }
    }
}