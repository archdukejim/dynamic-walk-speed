using RimWorld;
using Verse;

namespace DynamicWalkSpeeds.Thoughts
{
    [StaticConstructorOnStartup]
    public static class ThoughtTuning
    {
        static ThoughtTuning()
        {
            Apply(DynamicWalkSpeedsMod.settings);
        }

        public static void Apply(DynamicWalkSpeedsSettings settings)
        {
            if (settings == null) return;

            SetMood("DWS_BarefootUncovered", settings.barefootMoodOffset);
            SetMood("DWS_BarefootPainfulGround", settings.painfulGroundMoodOffset);
        }

        private static void SetMood(string defName, float mood)
        {
            ThoughtDef def = DefDatabase<ThoughtDef>.GetNamedSilentFail(defName);
            if (def?.stages == null || def.stages.Count == 0) return;

            def.stages[0].baseMoodEffect = mood;
        }
    }
}
