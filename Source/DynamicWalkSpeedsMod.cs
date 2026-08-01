using HarmonyLib;
using UnityEngine;
using Verse;

namespace DynamicWalkSpeeds
{
    public class DynamicWalkSpeedsMod : Mod
    {
        public static DynamicWalkSpeedsSettings settings;
        private int currentTab = 0;

        public DynamicWalkSpeedsMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<DynamicWalkSpeedsSettings>();
            Harmony harmony = new Harmony("archdukejim.DynamicWalkSpeeds");
            harmony.PatchAll();
        }

        public override string SettingsCategory() => "Dynamic Walk Speeds";

        public override void WriteSettings()
        {
            base.WriteSettings();
            Thoughts.ThoughtTuning.Apply(settings);
            Modifiers.SpeedCaches.InvalidateSettings();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            Rect tabRect = listing.GetRect(30f);
            if (Widgets.ButtonText(new Rect(tabRect.x, tabRect.y, 140f, 28f), "Weather")) currentTab = 0;
            if (Widgets.ButtonText(new Rect(tabRect.x + 145f, tabRect.y, 140f, 28f), "Floors")) currentTab = 1;
            if (Widgets.ButtonText(new Rect(tabRect.x + 290f, tabRect.y, 180f, 28f), "Territory and Surface")) currentTab = 2;
            if (Widgets.ButtonText(new Rect(tabRect.x + 475f, tabRect.y, 140f, 28f), "Creatures")) currentTab = 3;
            if (Widgets.ButtonText(new Rect(tabRect.x + 620f, tabRect.y, 140f, 28f), "Footwear")) currentTab = 4;

            listing.Gap(10f);

            switch (currentTab)
            {
                case 0:
                    Settings.SettingsUIWeather.DrawWeatherTab(listing, settings, inRect);
                    break;
                case 1:
                    Settings.SettingsUIFloors.DrawFloorsTab(listing, settings, inRect);
                    break;
                case 2:
                    Settings.SettingsUITerritory.DrawTerritoryAndSurfaceTab(listing, settings);
                    break;
                case 3:
                    Settings.SettingsUICreatures.DrawCreaturesTab(listing, settings, inRect);
                    break;
                case 4:
                    Settings.SettingsUIFootwear.DrawFootwearTab(listing, settings, inRect);
                    break;
            }

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }
    }
}
