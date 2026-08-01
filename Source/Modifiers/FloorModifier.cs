using Verse;

namespace DynamicWalkSpeeds.Modifiers
{
    public static class FloorModifier
    {
        public static float GetFloorMultiplier(TerrainDef terrain, DynamicWalkSpeedsSettings settings)
        {
            if (terrain == null || !settings.enableFloorModifiers)
                return 1.0f;

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

            return terrain.generated ||
                   terrain.designationCategory != null ||
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
