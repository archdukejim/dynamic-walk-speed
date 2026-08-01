using System.Collections.Generic;
using RimWorld;
using Verse;
using DynamicWalkSpeeds.Modifiers;

namespace DynamicWalkSpeeds.Thoughts
{
    public static class BarefootUtility
    {
        public static bool IsBarefoot(Pawn p, DynamicWalkSpeedsSettings settings)
        {
            if (p == null || p.Dead || p.apparel == null)
                return false;

            return ApparelModifier.GetBestCoverage(p, settings) <= 0f;
        }

        public static bool CaresAboutBareFeet(Pawn p, DynamicWalkSpeedsSettings settings)
        {
            if (p == null || !p.RaceProps.Humanlike)
                return false;

            if (!IsBarefoot(p, settings))
                return false;

            if (settings.barefootNudistsExempt && p.story?.traits != null && p.story.traits.HasTrait(TraitDefOf.Nudist))
                return false;

            return true;
        }

        public static bool OnPainfulGround(Pawn p, DynamicWalkSpeedsSettings settings)
        {
            Map map = p?.Map;
            if (map == null || !p.Spawned)
                return false;

            IntVec3 cell = p.Position;
            if (!cell.InBounds(map))
                return false;

            return ApparelModifier.GetBarefootPenalty(cell.GetTerrain(map), settings) < 1.0f;
        }

        public static void InjureFoot(Pawn p)
        {
            List<BodyPartRecord> parts = p.RaceProps?.body?.AllParts;
            if (parts == null) return;

            List<BodyPartRecord> feet = new List<BodyPartRecord>();
            for (int i = 0; i < parts.Count; i++)
            {
                string n = parts[i].def?.defName;
                if (n != null && (n.Contains("Foot") || n.Contains("Toe")))
                    feet.Add(parts[i]);
            }

            if (feet.Count == 0) return;

            BodyPartRecord target = feet.RandomElement();
            DamageDef damage = Rand.Chance(0.5f) ? DamageDefOf.Blunt : DamageDefOf.Cut;
            p.TakeDamage(new DamageInfo(damage, Rand.Range(1f, 3f), 0f, -1f, null, target));
        }
    }
}
