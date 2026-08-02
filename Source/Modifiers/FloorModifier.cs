using System.Collections.Generic;
using Verse;

namespace DynamicWalkSpeeds.Modifiers
{
    public static class FloorModifier
    {
        private static readonly Dictionary<TerrainDef, float> multCache = new Dictionary<TerrainDef, float>();
        private static readonly Dictionary<TerrainDef, bool> manufacturedCache = new Dictionary<TerrainDef, bool>();

        public static void InvalidateSettingsCache()
        {
            multCache.Clear();
        }

        public static float GetFloorMultiplier(TerrainDef terrain, DynamicWalkSpeedsSettings settings)
        {
            if (terrain == null || !settings.enableFloorModifiers)
                return 1.0f;

            if (SpeedTables.TryTerrain(terrain, out SpeedTables.TerrainRow row))
                return row.floorMult;

            if (multCache.TryGetValue(terrain, out float cached))
                return cached;

            float result = ResolveFloorMultiplier(terrain, settings);
            multCache[terrain] = result;
            return result;
        }

        internal static float ResolveFloorMultiplier(TerrainDef terrain, DynamicWalkSpeedsSettings settings)
        {
            if (settings.floorMultipliers.TryGetValue(terrain.defName, out float mult))
            {
                return settings.linkFloors ? mult * settings.masterFloorScale : mult;
            }

            float defaultMult = GetDefaultTerrainMultiplier(terrain);
            return settings.linkFloors ? defaultMult * settings.masterFloorScale : defaultMult;
        }

        public static bool IsManufactured(TerrainDef terrain)
        {
            if (terrain == null) return false;

            if (SpeedTables.TryTerrain(terrain, out SpeedTables.TerrainRow row))
                return row.manufactured;

            if (manufacturedCache.TryGetValue(terrain, out bool cached))
                return cached;

            bool result = ResolveManufactured(terrain);
            manufacturedCache[terrain] = result;
            return result;
        }

        internal static bool ResolveManufactured(TerrainDef terrain)
        {
            // Deliberately not terrain.generated: TerrainDefGenerator_Stone sets it on every
            // runtime stone terrain, so it flags natural rough-hewn rock as a built floor.
            return terrain.designationCategory != null ||
                   (terrain.researchPrerequisites != null && terrain.researchPrerequisites.Count > 0) ||
                   (terrain.defName != null && (terrain.defName.Contains("Tile") || terrain.defName.Contains("Concrete") || terrain.defName.Contains("Carpet") || terrain.defName.Contains("Smooth")));
        }

        public static float GetDefaultTerrainMultiplier(TerrainDef terrain)
        {
            if (terrain == null) return 1.0f;

            return IsManufactured(terrain) ? 1.15f : 1.0f;
        }
    }
}
