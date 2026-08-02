using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DynamicWalkSpeeds.Modifiers
{
    public static class ApparelModifier
    {
        public const float MinFootwearTraction = -1.00f;
        public const float MaxFootwearTraction = 1.50f;

        private const int PawnScanTtl = 60;

        private static readonly Dictionary<ThingDef, bool> coversCache = new Dictionary<ThingDef, bool>();
        private static string coversCacheKey;

        private static readonly Dictionary<TerrainDef, float> barefootCache = new Dictionary<TerrainDef, float>();

        private class PawnFootwear
        {
            public int tick = -99999;
            public float traction;
            public float coverage;
        }

        private static readonly Dictionary<Pawn, PawnFootwear> pawnCache = new Dictionary<Pawn, PawnFootwear>();

        public static void InvalidateCache()
        {
            coversCache.Clear();
            coversCacheKey = null;
            barefootCache.Clear();
            pawnCache.Clear();
        }

        public static void PruneCache()
        {
            if (pawnCache.Count == 0) return;

            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            List<Pawn> stale = null;

            foreach (KeyValuePair<Pawn, PawnFootwear> pair in pawnCache)
            {
                Pawn p = pair.Key;
                if (p == null || p.Destroyed || !p.Spawned || now - pair.Value.tick > 2500)
                {
                    if (stale == null) stale = new List<Pawn>();
                    stale.Add(p);
                }
            }

            if (stale == null) return;
            for (int i = 0; i < stale.Count; i++) pawnCache.Remove(stale[i]);
        }

        private static PawnFootwear GetPawnFootwear(Pawn pawn, DynamicWalkSpeedsSettings settings)
        {
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;

            if (!pawnCache.TryGetValue(pawn, out PawnFootwear entry))
            {
                entry = new PawnFootwear();
                pawnCache[pawn] = entry;
            }
            else if (now - entry.tick < PawnScanTtl)
            {
                return entry;
            }

            ScanWorn(pawn, settings, entry);
            entry.tick = now;
            return entry;
        }

        private static void ScanWorn(Pawn pawn, DynamicWalkSpeedsSettings settings, PawnFootwear entry)
        {
            entry.traction = 0f;
            entry.coverage = 0f;

            List<Apparel> worn = pawn.apparel?.WornApparel;
            if (worn == null) return;

            for (int i = 0; i < worn.Count; i++)
            {
                Apparel a = worn[i];
                if (a == null || !CoversTractionSlot(a.def, settings))
                    continue;

                float condition = GetConditionFactor(a, settings);
                entry.traction += GetBaseTraction(a.def, settings) * condition;

                float cover = settings.barefootQualityShields ? condition : 1f;
                if (cover > entry.coverage) entry.coverage = cover;
            }
        }

        public static bool CoversTractionSlot(ThingDef def, DynamicWalkSpeedsSettings settings)
        {
            if (def == null || def.apparel == null)
                return false;

            string key = string.Join(",", settings.footwearBodyPartGroups);
            if (coversCacheKey != key)
            {
                coversCache.Clear();
                coversCacheKey = key;
            }

            if (coversCache.TryGetValue(def, out bool cached))
                return cached;

            bool covers = false;
            List<BodyPartGroupDef> groups = def.apparel.bodyPartGroups;
            if (groups != null)
            {
                for (int i = 0; i < groups.Count && !covers; i++)
                {
                    if (groups[i] != null && settings.footwearBodyPartGroups.Contains(groups[i].defName))
                        covers = true;
                }
            }

            coversCache[def] = covers;
            return covers;
        }

        public static float GetQualityFactor(QualityCategory quality)
        {
            switch (quality)
            {
                case QualityCategory.Awful: return 0.50f;
                case QualityCategory.Poor: return 0.75f;
                case QualityCategory.Normal: return 1.00f;
                case QualityCategory.Good: return 1.15f;
                case QualityCategory.Excellent: return 1.30f;
                case QualityCategory.Masterwork: return 1.50f;
                case QualityCategory.Legendary: return 1.75f;
                default: return 1.00f;
            }
        }

        public static float GetBaseTraction(ThingDef def, DynamicWalkSpeedsSettings settings)
        {
            if (def == null) return 0f;

            if (settings.footwearTraction.TryGetValue(def.defName, out float over))
                return over;

            return settings.footwearBaseTraction;
        }

        public static float GetFootwearTraction(Pawn pawn, DynamicWalkSpeedsSettings settings)
        {
            if (pawn == null || pawn.apparel == null || !settings.enableFootwearTraction)
                return 0f;

            return GetPawnFootwear(pawn, settings).traction;
        }

        private static float GetConditionFactor(Apparel a, DynamicWalkSpeedsSettings settings)
        {
            float factor = 1f;

            if (settings.footwearQualityMatters && a.TryGetQuality(out QualityCategory quality))
                factor *= GetQualityFactor(quality);

            if (settings.footwearWearMatters && a.MaxHitPoints > 0)
            {
                float condition = (float)a.HitPoints / a.MaxHitPoints;
                factor *= UnityEngine.Mathf.Lerp(0.5f, 1.0f, UnityEngine.Mathf.Clamp01(condition));
            }

            return factor;
        }

        public static float GetBestCoverage(Pawn pawn, DynamicWalkSpeedsSettings settings)
        {
            if (pawn?.apparel == null)
                return -1f;

            return GetPawnFootwear(pawn, settings).coverage;
        }

        public static float GetDefaultBarefootPenalty(TerrainDef terrain)
        {
            if (terrain == null) return 1.00f;

            string n = terrain.defName;
            if (n == null) return 1.00f;

            if (n.Contains("Rough")) return 0.85f;
            if (n.Contains("Gravel") || n.Contains("Scree") || n.Contains("Rubble")) return 0.88f;
            if (n.Contains("Ice")) return 0.90f;
            if (n.Contains("Asphalt")) return 0.92f;

            return 1.00f;
        }

        public static float GetBarefootPenalty(TerrainDef terrain, DynamicWalkSpeedsSettings settings)
        {
            if (terrain == null) return 1.00f;

            if (SpeedTables.TryTerrain(terrain, out SpeedTables.TerrainRow row))
                return row.barefootPenalty;

            if (barefootCache.TryGetValue(terrain, out float cached))
                return cached;

            float penalty = ResolveBarefootPenalty(terrain, settings);
            barefootCache[terrain] = penalty;
            return penalty;
        }

        internal static float ResolveBarefootPenalty(TerrainDef terrain, DynamicWalkSpeedsSettings settings)
        {
            if (!settings.barefootPenalties.TryGetValue(terrain.defName, out float penalty))
                penalty = GetDefaultBarefootPenalty(terrain);

            return penalty;
        }

        public static float GetBarefootMultiplier(Pawn pawn, TerrainDef terrain, DynamicWalkSpeedsSettings settings)
        {
            if (pawn == null || terrain == null || !settings.enableBarefootPenalty)
                return 1.0f;

            // Humanlike only, matching the mood and injury paths. A mechanoid has an apparel
            // tracker but no feet to hurt, and would otherwise read as permanently barefoot.
            if (pawn.RaceProps == null || !pawn.RaceProps.Humanlike)
                return 1.0f;

            float coverage = GetBestCoverage(pawn, settings);
            if (coverage < 0f)
                return 1.0f;

            float penalty = GetBarefootPenalty(terrain, settings);
            if (penalty >= 1.0f)
                return 1.0f;

            float shortfall = UnityEngine.Mathf.Clamp01(1f - coverage);
            float scaled = (1f - penalty) * shortfall * settings.barefootPenaltyScale;

            return UnityEngine.Mathf.Clamp(1f - scaled, 0.1f, 1.0f);
        }
    }
}
