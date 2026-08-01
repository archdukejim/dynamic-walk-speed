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
                settings.snowPenaltyScale = listing.SliderLabeled($"Snow Penalty Impact ({settings.snowPenaltyScale:F2}x)", settings.snowPenaltyScale, 0.0f, 3.0f);
                settings.filthPenaltyScale = listing.SliderLabeled($"Filth Penalty Impact ({settings.filthPenaltyScale:F2}x)", settings.filthPenaltyScale, 0.0f, 3.0f);
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
