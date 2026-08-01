using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using DynamicWalkSpeeds.Modifiers;

namespace DynamicWalkSpeeds.Settings
{
    public static class SettingsUIFootwear
    {
        private static Vector2 scrollPosition = Vector2.zero;
        private static List<BodyPartGroupDef> apparelGroups;
        private static List<ThingDef> matching;
        private static string matchingKey;

        private static void BuildGroupList()
        {
            if (apparelGroups != null) return;

            HashSet<BodyPartGroupDef> seen = new HashSet<BodyPartGroupDef>();
            List<ThingDef> all = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                List<BodyPartGroupDef> groups = all[i].apparel?.bodyPartGroups;
                if (groups == null) continue;
                for (int k = 0; k < groups.Count; k++)
                {
                    if (groups[k] != null) seen.Add(groups[k]);
                }
            }

            apparelGroups = seen.OrderBy(d => d.defName).ToList();
        }

        private static void BuildMatching(DynamicWalkSpeedsSettings settings)
        {
            string key = string.Join(",", settings.footwearBodyPartGroups);
            if (matching != null && matchingKey == key) return;

            matching = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(d => ApparelModifier.CoversTractionSlot(d, settings))
                .OrderBy(d => d.label)
                .ToList();
            matchingKey = key;
        }

        public static void DrawFootwearTab(Listing_Standard listing, DynamicWalkSpeedsSettings settings, Rect inRect)
        {
            BuildGroupList();

            listing.CheckboxLabeled("Enable Footwear Traction", ref settings.enableFootwearTraction,
                "Worn apparel covering the chosen body part groups adds to a pawn's traction, so flooring pays off more for a shod pawn.");
            if (!settings.enableFootwearTraction) return;

            settings.footwearBaseTraction = listing.SliderLabeled(
                $"Default Traction Per Item ({settings.footwearBaseTraction:F2}x)",
                settings.footwearBaseTraction, ApparelModifier.MinFootwearTraction, ApparelModifier.MaxFootwearTraction);

            listing.CheckboxLabeled("Quality Matters", ref settings.footwearQualityMatters,
                "Awful 0.50x, Poor 0.75x, Normal 1.00x, Good 1.15x, Excellent 1.30x, Masterwork 1.50x, Legendary 1.75x of the item's traction.");
            listing.CheckboxLabeled("Condition Matters", ref settings.footwearWearMatters,
                "Worn out footwear grips less, scaling down to half its traction at zero hit points.");

            listing.Gap(8f);
            listing.Label("Body part groups that count as a traction slot:");

            Rect groupRow = listing.GetRect(26f);
            float gx = groupRow.x;
            for (int i = 0; i < apparelGroups.Count; i++)
            {
                BodyPartGroupDef g = apparelGroups[i];
                bool on = settings.footwearBodyPartGroups.Contains(g.defName);
                bool was = on;
                float w = Mathf.Max(70f, Text.CalcSize(g.defName).x + 34f);
                if (gx + w > groupRow.xMax)
                {
                    groupRow = listing.GetRect(26f);
                    gx = groupRow.x;
                }
                Widgets.CheckboxLabeled(new Rect(gx, groupRow.y, w, 24f), g.defName, ref on);
                if (on != was)
                {
                    if (on) settings.footwearBodyPartGroups.Add(g.defName);
                    else settings.footwearBodyPartGroups.Remove(g.defName);
                    ApparelModifier.InvalidateCache();
                    matching = null;
                }
                gx += w + 6f;
            }

            BuildMatching(settings);

            listing.Gap(8f);
            if (matching.Count == 0)
            {
                listing.Label("No apparel in your modlist covers the selected groups. Vanilla has no footwear; mods such as Vanilla Apparel Expanded add it.");
                return;
            }

            listing.Label($"Apparel occupying that slot ({matching.Count}):");

            Rect outRect = listing.GetRect(inRect.height - listing.CurHeight - 40f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 18f, matching.Count * 32f);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            Listing_Standard scroll = new Listing_Standard();
            scroll.Begin(viewRect);

            for (int i = 0; i < matching.Count; i++)
            {
                ThingDef d = matching[i];
                if (!settings.footwearTraction.TryGetValue(d.defName, out float val))
                {
                    val = settings.footwearBaseTraction;
                    settings.footwearTraction[d.defName] = val;
                }
                settings.footwearTraction[d.defName] = scroll.SliderLabeled($"{d.LabelCap} ({val:F2}x)", val,
                    ApparelModifier.MinFootwearTraction, ApparelModifier.MaxFootwearTraction);
            }

            scroll.End();
            Widgets.EndScrollView();
        }
    }
}
