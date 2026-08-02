namespace DynamicWalkSpeeds.Modifiers
{
    public static class SpeedCaches
    {
        private static bool anyEnabled;
        private static bool anyEnabledValid;
        private static bool needsTerrain;

        public static bool AnyEnabled(DynamicWalkSpeedsSettings settings)
        {
            if (!anyEnabledValid) Recompute(settings);
            return anyEnabled;
        }

        public static bool NeedsTerrain(DynamicWalkSpeedsSettings settings)
        {
            if (!anyEnabledValid) Recompute(settings);
            return needsTerrain;
        }

        private static void Recompute(DynamicWalkSpeedsSettings settings)
        {
            needsTerrain = settings.enableFloorModifiers ||
                           settings.enableCreatureModifiers ||
                           settings.enableBarefootPenalty;

            anyEnabled = needsTerrain ||
                         settings.enableWeatherModifiers ||
                         settings.enableSurfacePenalties ||
                         settings.enableTerritoryModifiers;

            anyEnabledValid = true;
        }

        public static void InvalidateSettings()
        {
            anyEnabledValid = false;
            SpeedTables.Invalidate();

            FloorModifier.InvalidateSettingsCache();
            WeatherModifier.InvalidateSettingsCache();
            CreatureModifier.InvalidateSettingsCache();
            TerritoryModifier.InvalidateSettingsCache();
            ApparelModifier.InvalidateCache();
        }

        public static void Prune()
        {
            ApparelModifier.PruneCache();
            TerritoryModifier.PruneCache();
        }
    }
}
