using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DynamicWalkSpeeds.Modifiers
{
    public static class SurfaceModifier
    {
        public const float FilthPenaltyPerUnit = 0.01f;

        // FilthDef.maxThickness defaults to 100 and only a handful of defs override it, so a
        // single well trodden cell can accumulate far more thickness than the 1% per unit rate
        // was designed around. Uncapped, a busy corridor would eventually saturate at the 0.10x
        // floor: a 90% slowdown from dirt. Cap what is charged at a sane number of units.
        public const int MaxCountedFilth = 10;

        public static float GetSnowTickAdjustment(Map map, IntVec3 cell, DynamicWalkSpeedsSettings settings)
        {
            if (map == null || !settings.enableSurfacePenalties || !cell.InBounds(map))
                return 0f;

            if (settings.snowPenaltyScale == 1f)
                return 0f;

            var grid = map.snowGrid;
            if (grid == null)
                return 0f;

            int addon = WeatherBuildupUtility.MovementTicksAddOn(grid.GetCategory(cell));
            if (addon == 0)
                return 0f;

            return addon * (settings.snowPenaltyScale - 1f);
        }

        public static float GetFilthMultiplier(Map map, IntVec3 cell, DynamicWalkSpeedsSettings settings)
        {
            if (map == null || !settings.enableSurfacePenalties || settings.filthPenaltyScale <= 0f || !cell.InBounds(map))
                return 1.0f;

            List<Thing> thingList = cell.GetThingList(map);
            if (thingList == null)
                return 1.0f;

            int amount = 0;
            for (int i = 0; i < thingList.Count; i++)
            {
                if (thingList[i] is Filth filth)
                    amount += filth.thickness;
            }

            if (amount == 0)
                return 1.0f;

            if (amount > MaxCountedFilth)
                amount = MaxCountedFilth;

            float penalty = FilthPenaltyPerUnit * amount * settings.filthPenaltyScale;
            return UnityEngine.Mathf.Clamp(1.0f - penalty, 0.1f, 1.0f);
        }
    }
}
