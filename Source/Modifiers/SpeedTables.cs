using System.Collections.Generic;
using Verse;

namespace DynamicWalkSpeeds.Modifiers
{
    public static class SpeedTables
    {
        public struct TerrainRow
        {
            public float floorMult;
            public float barefootPenalty;
            public bool manufactured;
        }

        public struct RaceRow
        {
            public float traction;
            public float speed;
        }

        private static TerrainRow[] terrainRows;
        private static RaceRow[] raceRows;
        private static float[] weatherMults;
        private static bool built;

        public static void Invalidate()
        {
            built = false;
        }

        public static void EnsureBuilt(DynamicWalkSpeedsSettings settings)
        {
            if (built) return;
            Build(settings);
        }

        private static void Build(DynamicWalkSpeedsSettings settings)
        {
            built = true;

            List<TerrainDef> terrains = DefDatabase<TerrainDef>.AllDefsListForReading;
            terrainRows = new TerrainRow[MaxIndex(terrains) + 1];
            for (int i = 0; i < terrains.Count; i++)
            {
                TerrainDef t = terrains[i];
                int k = t.index;
                if (k < 0 || k >= terrainRows.Length) continue;

                terrainRows[k].manufactured = FloorModifier.ResolveManufactured(t);
                terrainRows[k].floorMult = FloorModifier.ResolveFloorMultiplier(t, settings);
                terrainRows[k].barefootPenalty = ApparelModifier.ResolveBarefootPenalty(t, settings);
            }

            List<WeatherDef> weathers = DefDatabase<WeatherDef>.AllDefsListForReading;
            weatherMults = new float[MaxIndex(weathers) + 1];
            for (int i = 0; i < weathers.Count; i++)
            {
                WeatherDef w = weathers[i];
                int k = w.index;
                if (k < 0 || k >= weatherMults.Length) continue;

                weatherMults[k] = WeatherModifier.ResolveWeatherMultiplier(w, settings);
            }

            List<ThingDef> things = DefDatabase<ThingDef>.AllDefsListForReading;
            raceRows = new RaceRow[MaxIndex(things) + 1];
            for (int i = 0; i < things.Count; i++)
            {
                ThingDef d = things[i];
                int k = d.index;
                if (k < 0 || k >= raceRows.Length) continue;

                raceRows[k].traction = CreatureModifier.GetTraction(d, settings);
                raceRows[k].speed = CreatureModifier.GetSpeed(d, settings);
            }
        }

        private static int MaxIndex<T>(List<T> defs) where T : Def
        {
            int max = 0;
            for (int i = 0; i < defs.Count; i++)
            {
                int k = defs[i].index;
                if (k > max) max = k;
            }
            return max;
        }

        public static bool TryTerrain(TerrainDef t, out TerrainRow row)
        {
            TerrainRow[] rows = terrainRows;
            if (rows != null && t != null)
            {
                int k = t.index;
                if (k >= 0 && k < rows.Length)
                {
                    row = rows[k];
                    return true;
                }
            }

            row = default(TerrainRow);
            return false;
        }

        public static bool TryWeather(WeatherDef w, out float mult)
        {
            float[] rows = weatherMults;
            if (rows != null && w != null)
            {
                int k = w.index;
                if (k >= 0 && k < rows.Length)
                {
                    mult = rows[k];
                    return true;
                }
            }

            mult = 1f;
            return false;
        }

        public static bool TryRace(ThingDef d, out RaceRow row)
        {
            RaceRow[] rows = raceRows;
            if (rows != null && d != null)
            {
                int k = d.index;
                if (k >= 0 && k < rows.Length)
                {
                    row = rows[k];
                    return true;
                }
            }

            row = default(RaceRow);
            return false;
        }
    }
}
