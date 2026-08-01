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

                float bonus = GetBaseTraction(a.def, settings);

                if (settings.footwearQualityMatters && a.TryGetQuality(out QualityCategory quality))
                    bonus *= GetQualityFactor(quality);

                if (settings.footwearWearMatters && a.MaxHitPoints > 0)
                {
                    float condition = (float)a.HitPoints / a.MaxHitPoints;
                    bonus *= UnityEngine.Mathf.Lerp(0.5f, 1.0f, UnityEngine.Mathf.Clamp01(condition));
                }

                total += bonus;
            }

            return total;
        }
    }
}
