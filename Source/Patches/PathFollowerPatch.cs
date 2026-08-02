using HarmonyLib;
using Verse;
using Verse.AI;
using DynamicWalkSpeeds.Modifiers;

namespace DynamicWalkSpeeds.Patches
{
    [HarmonyPatch(typeof(Pawn_PathFollower), "CostToMoveIntoCell", new System.Type[] { typeof(Pawn), typeof(IntVec3) })]
    public static class PathFollowerPatch
    {
        public static void Postfix(Pawn pawn, IntVec3 c, ref float __result)
        {
            if (pawn == null || pawn.Map == null || __result <= 0f)
                return;

            DynamicWalkSpeedsSettings settings = DynamicWalkSpeedsMod.settings;
            if (settings == null)
                return;

            if (!SpeedCaches.AnyEnabled(settings))
                return;

            SpeedTables.EnsureBuilt(settings);

            Map map = pawn.Map;
            TerrainDef terrain = SpeedCaches.NeedsTerrain(settings) && c.InBounds(map) ? c.GetTerrain(map) : null;

            float weatherMult = WeatherModifier.GetWeatherMultiplier(map, settings);
            float floorMult = FloorModifier.GetFloorMultiplier(terrain, settings);
            floorMult = CreatureModifier.ApplyTraction(pawn, terrain, floorMult, settings);
            float filthMult = SurfaceModifier.GetFilthMultiplier(map, c, settings);
            float territoryMult = TerritoryModifier.GetTerritoryMultiplier(pawn, settings);
            float creatureMult = CreatureModifier.GetSpeedMultiplier(pawn, settings);
            float barefootMult = ApparelModifier.GetBarefootMultiplier(pawn, terrain, settings);

            float totalSpeedMultiplier = weatherMult * floorMult * filthMult * territoryMult * creatureMult * barefootMult;
            if (totalSpeedMultiplier <= 0.01f)
                totalSpeedMultiplier = 0.01f;

            __result += SurfaceModifier.GetSnowTickAdjustment(map, c, settings);
            __result /= totalSpeedMultiplier;

            if (__result < 1f)
                __result = 1f;
        }
    }
}
