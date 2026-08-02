using System.Collections.Generic;
using Verse;

namespace DynamicWalkSpeeds.Debugging
{
    public static class DWSTestSubjects
    {
        public static readonly string[] TerrainNames =
        {
            "Soil",
            "SoilRich",
            "MossyTerrain",
            "Gravel",
            "Sand",
            "SoftSand",
            "Mud",
            "MarshyTerrain",
            "Ice",
            "Riverbank",

            "PackedDirt",
            "BrokenAsphalt",

            "Sandstone_Rough",
            "Sandstone_RoughHewn",
            "Sandstone_Smooth",

            "StrawMatting",
            "WoodPlankFloor",
            // A carpet (cloth) is the soft built floor 0.3.0 turns on. The colour is a
            // deliberate callback: "ice" in the defName must not make it read hard.
            "CarpetBlueIce",
            "Concrete",
            "PavedTile",
            "TileSandstone",
            "FlagstoneSandstone",
            "MetalTile",
            "SilverTile",
            "SterileTile",
            "Bridge"
        };

        public static readonly string[] RaceNames =
        {
            "Human",

            "GuineaPig",
            "Rat",
            "Cat",
            "Squirrel",

            "LabradorRetriever",
            "Lynx",

            "Megasloth",

            "Goat",
            "Caribou",

            "Muffalo",
            "Cow",
            "Bison",
            "Elephant",

            "Chicken",
            "Duck",
            "Goose",

            "Megascarab",
            "Megaspider",
            "Spelopede",

            "Toughspike",
            "StoneCrab",

            "Tortoise",
            "Seal",
            "Walrus",

            "Cobra",

            "Mech_Agrihand",
            "Mech_Centurion"
        };

        public static List<TerrainDef> ResolveTerrains(List<string> missing)
        {
            List<TerrainDef> result = new List<TerrainDef>();
            for (int i = 0; i < TerrainNames.Length; i++)
            {
                TerrainDef def = DefDatabase<TerrainDef>.GetNamedSilentFail(TerrainNames[i]);
                if (def != null) result.Add(def);
                else missing?.Add(TerrainNames[i]);
            }
            return result;
        }

        public static List<ThingDef> ResolveRaces(List<string> missing)
        {
            List<ThingDef> result = new List<ThingDef>();
            for (int i = 0; i < RaceNames.Length; i++)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(RaceNames[i]);
                if (def != null && def.race != null) result.Add(def);
                else missing?.Add(RaceNames[i]);
            }
            return result;
        }

        public static PawnKindDef KindFor(ThingDef race)
        {
            List<PawnKindDef> kinds = DefDatabase<PawnKindDef>.AllDefsListForReading;
            for (int i = 0; i < kinds.Count; i++)
            {
                if (kinds[i].race == race) return kinds[i];
            }
            return null;
        }
    }
}
