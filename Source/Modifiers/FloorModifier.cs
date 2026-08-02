using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DynamicWalkSpeeds.Modifiers
{
    public static class FloorModifier
    {
        private static readonly Dictionary<TerrainDef, float> multCache = new Dictionary<TerrainDef, float>();
        private static readonly Dictionary<TerrainDef, bool> manufacturedCache = new Dictionary<TerrainDef, bool>();
        private static readonly Dictionary<TerrainDef, float> softnessCache = new Dictionary<TerrainDef, float>();

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
            // Growable ground is never a built floor, even when a mod makes it buildable.
            // VFE Architect's lawns carry a Floors designationCategory but keep soil's
            // fertility (1.0); without this gate they read as manufactured and both the
            // floor bonus and the traction penalty apply to grass. Every vanilla built
            // floor inherits FloorBase at fertility 0, so the gate only removes terrain
            // that is both buildable and fertile -- planted ground.
            if (terrain.fertility > 0f)
                return false;

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

        public static float GetSoftness(TerrainDef terrain)
        {
            if (terrain == null) return 0f;

            if (SpeedTables.TryTerrain(terrain, out SpeedTables.TerrainRow row))
                return row.softness;

            if (softnessCache.TryGetValue(terrain, out float cached))
                return cached;

            float result = ResolveSoftness(terrain);
            softnessCache[terrain] = result;
            return result;
        }

        // How soft a floor is underfoot, 0 (hard: steel tile, smoothed stone) to 1 (soft:
        // carpet). Read from what the floor was built out of via the game's own stuff
        // categories, so modded floors and the runtime-generated carpet colour variants
        // classify themselves and colour names in defNames (CarpetBlueIce) never enter in.
        // Baked into SpeedTables.TerrainRow at load; not called in the hot path.
        public static float ResolveSoftness(TerrainDef terrain)
        {
            if (terrain == null || !ResolveManufactured(terrain))
                return 0f;

            List<ThingDefCountClass> cost = terrain.costList;
            if (cost != null && cost.Count > 0)
            {
                ThingDef material = null;
                int best = int.MinValue;
                for (int i = 0; i < cost.Count; i++)
                {
                    if (cost[i]?.thingDef == null) continue;
                    if (cost[i].count > best)
                    {
                        best = cost[i].count;
                        material = cost[i].thingDef;
                    }
                }

                float matched;
                if (TrySoftnessForMaterial(material, out matched))
                    return matched;
            }

            // No costList (smoothed stone, ancient tile, burned carpet, fungal gravel):
            // nothing was consumed to make the floor. Post fertility-gate these are all
            // bare hard surfaces, so default hard.
            return 0f;
        }

        private static bool TrySoftnessForMaterial(ThingDef material, out float softness)
        {
            softness = 0f;

            List<StuffCategoryDef> cats = material?.stuffProps?.categories;
            if (cats == null || cats.Count == 0)
                return false;

            for (int i = 0; i < cats.Count; i++)
            {
                StuffCategoryDef c = cats[i];
                if (c == StuffCategoryDefOf.Fabric || c == StuffCategoryDefOf.Leathery) { softness = 1.0f; return true; }
                if (c == StuffCategoryDefOf.Woody) { softness = 0.5f; return true; }
                if (c == StuffCategoryDefOf.Stony) { softness = 0.1f; return true; }
                if (c == StuffCategoryDefOf.Metallic) { softness = 0.0f; return true; }
            }

            return false;
        }
    }
}
