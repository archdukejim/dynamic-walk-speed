using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DynamicWalkSpeeds.Modifiers
{
    public static class SurfaceModifier
    {
        public const float FilthPenaltyPerUnit = 0.01f;

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

            float penalty = FilthPenaltyPerUnit * amount * settings.filthPenaltyScale;
            return UnityEngine.Mathf.Clamp(1.0f - penalty, 0.1f, 1.0f);
        }
    }
}
