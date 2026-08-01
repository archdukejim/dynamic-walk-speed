using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DynamicWalkSpeeds.Thoughts
{
    public class BarefootWatcher : GameComponent
    {
        private const int IntervalTicks = 2500;

        public BarefootWatcher(Game game)
        {
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % IntervalTicks != 0)
                return;

            Modifiers.SpeedCaches.Prune();

            DynamicWalkSpeedsSettings settings = DynamicWalkSpeedsMod.settings;
            if (settings == null)
                return;

            if (!settings.enablePainfulGroundMood && !settings.enableFootInjury)
                return;

            List<Map> maps = Find.Maps;
            if (maps == null) return;

            for (int m = 0; m < maps.Count; m++)
            {
                Map map = maps[m];
                IReadOnlyList<Pawn> pawns = map.mapPawns?.AllPawnsSpawned;
                if (pawns == null) continue;

                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn p = pawns[i];
                    if (p == null || p.Dead || p.apparel == null)
                        continue;

                    if (!BarefootUtility.CaresAboutBareFeet(p, settings))
                        continue;

                    if (!BarefootUtility.OnPainfulGround(p, settings))
                        continue;

                    if (settings.enablePainfulGroundMood && p.needs?.mood != null)
                    {
                        ThoughtDef thought = DWSThoughtDefOf.DWS_BarefootPainfulGround;
                        if (thought != null)
                            p.needs.mood.thoughts.memories.TryGainMemory(thought);
                    }

                    if (settings.enableFootInjury && Rand.Chance(settings.footInjuryChance))
                        BarefootUtility.InjureFoot(p);
                }
            }
        }
    }

    [DefOf]
    public static class DWSThoughtDefOf
    {
        public static ThoughtDef DWS_BarefootUncovered;
        public static ThoughtDef DWS_BarefootPainfulGround;

        static DWSThoughtDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DWSThoughtDefOf));
        }
    }
}
