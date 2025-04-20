using UnityEngine;

public class Trait_Parasitic : Trait
{
    public override int Id => 20;

    public override void ApplyTrait(CreatureBehavior creature)
    {
        // Parasitic effect (ID 29): Steals 1 food per attack if target has food
        // Handled in CreatureCombat.cs; ensure trait ID is registered
        if (!creature.traitIds.Contains(29))
        {
            creature.traitIds.Add(29);
            Debug.Log($"{creature.name}: Applied Parasitic trait");
        }

        CreatureCombat combat = creature.GetComponent<CreatureCombat>();
        if (combat != null)
        {
            // Optional: Adjust combat properties if needed (e.g., reduce base damage by 15% as per TraitList)
            // Note: Damage reduction is descriptive in TraitList; implement if required
            // combat.damageMultiplier = combat.damageMultiplier * 0.85f;
        }
    }
}