using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TraitManager : MonoBehaviour
{
    public class TraitInfo
    {
        public string Name;
        public int Cost;
        public System.Type Type;
        public string HexColor; // Hex code for the trait's color
        public bool Required; // Indicates if the trait must be picked
    }

    private static readonly Dictionary<int, TraitInfo> traits = new Dictionary<int, TraitInfo>
    {
        { 1, new TraitInfo { Name = "Small", Cost = 1, Type = typeof(Trait_Small), HexColor = "#e2ffc9", Required = false } },
        { 2, new TraitInfo { Name = "Big", Cost = 2, Type = typeof(Trait_Big), HexColor = "#783506", Required = false } },
        { 3, new TraitInfo { Name = "Fortified", Cost = 3, Type = typeof(Trait_Fortified), HexColor = "#4795d6", Required = false } },
        { 4, new TraitInfo { Name = "Fast", Cost = 2, Type = typeof(Trait_Fast), HexColor = "#fff700", Required = false } },
        { 5, new TraitInfo { Name = "Efficient Eater", Cost = 2, Type = typeof(Trait_EfficientEater), HexColor = "#228B22", Required = false } },
        { 6, new TraitInfo { Name = "Immortal", Cost = 4, Type = typeof(Trait_Immortal), HexColor = "#ffd700", Required = false } },
        { 7, new TraitInfo { Name = "Fertile", Cost = 2, Type = typeof(Trait_Fertile), HexColor = "#FF69B4", Required = false } },
        { 8, new TraitInfo { Name = "Hunting", Cost = 3, Type = typeof(Trait_Hunter), HexColor = "#FF0000", Required = false } },
        { 9, new TraitInfo { Name = "Friendly", Cost = 2, Type = typeof(Trait_Friendly), HexColor = "#2fff00", Required = false } },
        { 10, new TraitInfo { Name = "Climbing", Cost = 1, Type = typeof(Trait_Climber), HexColor = "#dbd9d9", Required = false } },
        { 11, new TraitInfo { Name = "Scout", Cost = 2, Type = typeof(Trait_Scout), HexColor = "#abd100", Required = false } },
        { 12, new TraitInfo { Name = "Wandering", Cost = 2, Type = typeof(Trait_Wanderer), HexColor = "#87CEEB", Required = false } },
        { 13, new TraitInfo { Name = "Venomous", Cost = 4, Type = typeof(Trait_Venomous), HexColor = "#55ff00", Required = false } },
        { 14, new TraitInfo { Name = "Berserk", Cost = 3, Type = typeof(Trait_Berserker), HexColor = "#DC143C", Required = false } },
        { 15, new TraitInfo { Name = "Gluttonous", Cost = 3, Type = typeof(Trait_Glutton), HexColor = "#FFA500", Required = false } },
        { 16, new TraitInfo { Name = "Prolific", Cost = 2, Type = typeof(Trait_Prolific), HexColor = "#ffa1e4", Required = false } },
        { 17, new TraitInfo { Name = "Altruistic", Cost = 3, Type = typeof(Trait_Altruist), HexColor = "#98FB98", Required = false } },
        { 18, new TraitInfo { Name = "Enduring", Cost = 2, Type = typeof(Trait_Enduring), HexColor = "#4e6e0c", Required = false } },
        { 19, new TraitInfo { Name = "Adrenaline", Cost = 3, Type = typeof(Trait_Adrenaline), HexColor = "#FF4500", Required = false } },
        { 20, new TraitInfo { Name = "Parasitic", Cost = 4, Type = typeof(Trait_Parasitic), HexColor = "#eb024c", Required = false } },
        { 21, new TraitInfo { Name = "Migratory", Cost = 3, Type = typeof(Trait_Migratory), HexColor = "#1E90FF", Required = false } },
        { 22, new TraitInfo { Name = "Tactical", Cost = 3, Type = typeof(Trait_Tactician), HexColor = "#6a85a1", Required = false } },
        { 23, new TraitInfo { Name = "Ambush", Cost = 3, Type = typeof(Trait_Ambusher), HexColor = "#212121", Required = false } },
        { 24, new TraitInfo { Name = "Swift Breeding", Cost = 2, Type = typeof(Trait_SwiftBreeder), HexColor = "#ff7dfd", Required = false } },
        { 25, new TraitInfo { Name = "Burrowing", Cost = 2, Type = typeof(Trait_Burrower), HexColor = "#8B4513", Required = false } },
        { 27, new TraitInfo { Name = "Pheromonal", Cost = 2, Type = typeof(Trait_Pheromonal), HexColor = "#FF00FF", Required = false } },
        { 28, new TraitInfo { Name = "Keen", Cost = 2, Type = typeof(Trait_Keen), HexColor = "#20B2AA", Required = false } },
        { 29, new TraitInfo { Name = "Spiky", Cost = 5, Type = typeof(Trait_Spiky), HexColor = "#FF4500", Required = false } },
        { 30, new TraitInfo { Name = "TrapMaking", Cost = 3, Type = typeof(Trait_TrapMaker), HexColor = "#4f7d00", Required = false } },
        { 31, new TraitInfo { Name = "Giant", Cost = 5, Type = typeof(Trait_Giant), HexColor = "#5A2D0C", Required = false } },
        { 32, new TraitInfo { Name = "Bioluminescent", Cost = 2, Type = typeof(Trait_Bioluminescent), HexColor = "#00FFFF", Required = false } },
        { 33, new TraitInfo { Name = "Loyal", Cost = 2, Type = typeof(Trait_Loyal), HexColor = "#FF1493", Required = false } },
        { 100, new TraitInfo { Name = "Nomadic", Cost = 0, Type = typeof(Trait_NomadLeader), HexColor = "#FFD700", Required = true } }
    };

    public static string GetTraitName(int traitId)
    {
        return traits.ContainsKey(traitId) ? traits[traitId].Name : null;
    }

    public static string GetTraitPrefix(int traitId)
    {
        return GetTraitName(traitId) ?? ""; // Use the trait's Name as the prefix
    }

    public static System.Type GetTraitType(int traitId)
    {
        return traits.ContainsKey(traitId) ? traits[traitId].Type : null;
    }

    public static int GetTraitCost(int traitId)
    {
        return traits.ContainsKey(traitId) ? traits[traitId].Cost : 0;
    }

    public static string GetTraitHexColor(int traitId)
    {
        return traits.ContainsKey(traitId) ? traits[traitId].HexColor : "#FFFFFF"; // Default to white if not found
    }

    public static bool IsTraitRequired(int traitId)
    {
        return traits.ContainsKey(traitId) && traits[traitId].Required;
    }

    public static int[] GetAllTraitIds()
    {
        return traits.Keys.ToArray();
    }
}