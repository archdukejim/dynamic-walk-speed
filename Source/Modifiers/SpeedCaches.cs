namespace DynamicWalkSpeeds.Modifiers
{
    public static class SpeedCaches
    {
        public static void InvalidateSettings()
        {
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
