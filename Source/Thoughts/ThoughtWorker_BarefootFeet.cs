using RimWorld;
using Verse;
using DynamicWalkSpeeds.Modifiers;

namespace DynamicWalkSpeeds.Thoughts
{
    public class ThoughtWorker_BarefootFeet : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            DynamicWalkSpeedsSettings settings = DynamicWalkSpeedsMod.settings;
            if (settings == null || !settings.enableBarefootMoodPenalty)
                return ThoughtState.Inactive;

            if (!BarefootUtility.CaresAboutBareFeet(p, settings))
                return ThoughtState.Inactive;

            return ThoughtState.ActiveDefault;
        }
    }
}
