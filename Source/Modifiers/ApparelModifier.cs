using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DynamicWalkSpeeds.Modifiers
{
    public static class ApparelModifier
    {
        public const float MinFootwearTraction = -1.00f;
        public const float MaxFootwearTraction = 1.50f;

        private static readonly Dictionary<ThingDef, bool> coversCache = new Dictionary<ThingDef, bool>();
        private static string coversCacheKey;

        public static void InvalidateCache()
        {
            coversCache.Clear();
            coversCacheKey = null;
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
            if (pawn == null || !settings.enableFootwearTraction)
                return 0f;

            Pawn_ApparelTracker tracker = pawn.apparel;
            if (tracker == null)
                return 0f;

            List<Apparel> worn = tracker.WornApparel;
            if (worn == null || worn.Count == 0)
                return 0f;

            float total = 0f;
            for (int i = 0; i < worn.Count; i++)
            {
                Apparel a = worn[i];
                if (a == null || !CoversTractionSlot(a.def, settings))
                    continue;

                total += GetBaseTraction(a.def, settings) * GetConditionFactor(a, settings);
            }

            return total;
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
            Pawn_ApparelTracker tracker = pawn?.apparel;
            if (tracker == null)
                return -1f;

            List<Apparel> worn = tracker.WornApparel;
            if (worn == null)
                return 0f;

            float best = 0f;
            for (int i = 0; i < worn.Count; i++)
            {
                Apparel a = worn[i];
                if (a == null || !CoversTractionSlot(a.def, settings))
                    continue;

                float cover = settings.barefootQualityShields ? GetConditionFactor(a, settings) : 1f;
                if (cover > best) best = cover;
            }

            return best;
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

            if (!settings.barefootPenalties.TryGetValue(terrain.defName, out float penalty))
                penalty = GetDefaultBarefootPenalty(terrain);

            return penalty;
        }

        public static float GetBarefootMultiplier(Pawn pawn, TerrainDef terrain, DynamicWalkSpeedsSettings settings)
        {
            if (pawn == null || terrain == null || !settings.enableBarefootPenalty)
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
