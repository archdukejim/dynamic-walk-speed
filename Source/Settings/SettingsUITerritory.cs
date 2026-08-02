using UnityEngine;
using Verse;

namespace DynamicWalkSpeeds.Settings
{
    public static class SettingsUITerritory
    {
        public static void DrawTerritoryAndSurfaceTab(Listing_Standard listing, DynamicWalkSpeedsSettings settings)
        {
            listing.Label("Surface Penalties (Snow and Filth):");
            listing.CheckboxLabeled("Enable Surface Penalties", ref settings.enableSurfacePenalties);
            if (settings.enableSurfacePenalties)
            {
                settings.snowPenaltyScale = listing.SliderLabeled(
                    $"Snow Effectiveness ({(settings.snowPenaltyScale * 100f):F0}% of vanilla)",
                    settings.snowPenaltyScale, 0.0f, 10.0f,
                    tooltip: "Scales the game's own snow movement penalty. 0% removes it entirely, 100% leaves vanilla alone, 1000% makes it ten times as punishing.");
                settings.filthPenaltyScale = listing.SliderLabeled(
                    $"Filth Effectiveness ({(settings.filthPenaltyScale * 100f):F0}%)",
                    settings.filthPenaltyScale, 0.0f, 10.0f,
                    tooltip: "1% slower per unit of filth in the cell at 100%. A cell holding five units costs 5% at 100%, or 50% at 1000%.");
            }

            listing.Gap(15f);
            listing.Label("Hostile Territory Modifiers:");
            listing.CheckboxLabeled("Enable Hostile Territory Modifiers", ref settings.enableTerritoryModifiers);
            if (settings.enableTerritoryModifiers)
            {
                listing.CheckboxLabeled("Link Hostile Map Tile and Active Enemy Triggers", ref settings.linkTerritoryTriggers);
                if (!settings.linkTerritoryTriggers)
                {
                    listing.CheckboxLabeled("Trigger on Hostile Map Tile", ref settings.hostileMapTileTrigger);
                    listing.CheckboxLabeled("Trigger on Active Hostile Pawns Present", ref settings.activeHostilePawnsTrigger);
                }
                settings.hostileTerritoryMultiplier = listing.SliderLabeled($"Hostile Speed Multiplier ({settings.hostileTerritoryMultiplier:F2}x)", settings.hostileTerritoryMultiplier, 0.50f, 1.00f);
                listing.CheckboxLabeled("Fleeing Pawns Are Exempt", ref settings.territoryFleeingExempt,
                    "A pawn that is fleeing or in a panic flee mental state drops the hostile territory penalty entirely. Running for your life is the one time the ground stops mattering.");
            }
        }
    }
}
