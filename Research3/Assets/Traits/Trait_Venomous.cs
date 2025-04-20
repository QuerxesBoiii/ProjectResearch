using UnityEngine;

public class Trait_Venomous : Trait
{
    public override int Id => 13;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        // Venomous effect (ID 13): Attacks apply poison (0.5 damage/s for 5s)
        // Handled in CreatureCombat.cs; ensure trait ID is registered
        if (!creature.traitIds.Contains(13))
        {
            creature.traitIds.Add(13);
            Debug.Log($"{creature.name}: Applied Venomous trait");
        }

        CreatureCombat combat = creature.GetComponent<CreatureCombat>();
        if (combat != null)
        {
            // Optional: Adjust combat properties if needed (e.g., 20% longer attack cooldown)
            // Note: Cooldown is descriptive in TraitList; implement if required
            // combat.attackIntervalMultiplier = combat.attackIntervalMultiplier * 1.2f;
        }
    }
}