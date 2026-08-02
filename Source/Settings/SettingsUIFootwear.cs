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
        private static List<TerrainDef> penaltyTerrains;
        private static bool showTerrainPenalties;

        private static void BuildTerrainList()
        {
            if (penaltyTerrains != null) return;

            penaltyTerrains = DefDatabase<TerrainDef>.AllDefsListForReading
                .OrderByDescending(t => 1f - ApparelModifier.GetDefaultBarefootPenalty(t))
                .ThenBy(t => t.label)
                .ToList();
        }

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

            Text.Font = GameFont.Small;
            GUI.color = new Color(1f, 0.82f, 0.4f);
            listing.Label("EXPERIMENTAL - off by default, being finished for 1.0");
            GUI.color = Color.white;
            listing.Label("Everything in this section is untested in real play. The mood effects and the injury roll are the only parts of this mod that add content and damage pawns, so they stay opt in until they have been played rather than only measured. Enable at your own risk on a save you do not mind losing.");
            listing.Gap(4f);

            listing.CheckboxLabeled("Enable Barefoot Terrain Penalties", ref settings.enableBarefootPenalty,
                "EXPERIMENTAL. A pawn that could wear footwear but is not slows down on ground that hurts to walk on. Animals and mechanoids are never penalised.");
            if (settings.enableBarefootPenalty)
            {
                settings.barefootPenaltyScale = listing.SliderLabeled(
                    $"Barefoot Penalty Impact ({settings.barefootPenaltyScale:F2}x)", settings.barefootPenaltyScale, 0f, 3f);
                listing.CheckboxLabeled("Footwear Quality Shields", ref settings.barefootQualityShields,
                    "Awful footwear only shields half the penalty. Off means any footwear shields it completely.");
                listing.CheckboxLabeled("Edit penalties per terrain", ref showTerrainPenalties);
            }

            listing.GapLine(8f);
            listing.Label("EXPERIMENTAL consequences of going barefoot (humanlike pawns only):");

            listing.CheckboxLabeled("Nudists Are Exempt", ref settings.barefootNudistsExempt,
                "Nudists take no mood hit and no foot injuries. They opted in.");

            listing.CheckboxLabeled("Mood: Bare Feet", ref settings.enableBarefootMoodPenalty,
                "A standing mood penalty while the pawn has nothing on its feet, anywhere on the map.");
            if (settings.enableBarefootMoodPenalty)
            {
                settings.barefootMoodOffset = listing.SliderLabeled(
                    $"    Bare Feet Mood ({settings.barefootMoodOffset:F0})", settings.barefootMoodOffset, -20f, 0f);
            }

            listing.CheckboxLabeled("Mood: Sore Feet On Painful Ground", ref settings.enablePainfulGroundMood,
                "A stacking memory gained each in game hour spent barefoot on ground that carries a barefoot penalty.");
            if (settings.enablePainfulGroundMood)
            {
                settings.painfulGroundMoodOffset = listing.SliderLabeled(
                    $"    Sore Feet Mood ({settings.painfulGroundMoodOffset:F0})", settings.painfulGroundMoodOffset, -20f, 0f);
            }

            listing.CheckboxLabeled("Foot Injuries", ref settings.enableFootInjury,
                "Each in game hour spent barefoot on painful ground carries a chance of a bruise or a cut to a foot.");
            if (settings.enableFootInjury)
            {
                settings.footInjuryChance = listing.SliderLabeled(
                    $"    Injury Chance Per Hour ({(settings.footInjuryChance * 100f):F2}%)", settings.footInjuryChance, 0f, 0.05f);
            }

            listing.GapLine(8f);
            listing.Label("Footwear traction is not experimental and is on by default:");

            listing.CheckboxLabeled("Enable Footwear Traction", ref settings.enableFootwearTraction,
                "Worn apparel covering the chosen body part groups adds to a pawn's traction, so flooring pays off more for a shod pawn.");

            if (showTerrainPenalties && settings.enableBarefootPenalty)
            {
                DrawTerrainPenalties(listing, settings, inRect);
                return;
            }

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

            Rect outRect = listing.GetRect(Mathf.Max(120f, inRect.height - listing.CurHeight - 40f));
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

        private static void DrawTerrainPenalties(Listing_Standard listing, DynamicWalkSpeedsSettings settings, Rect inRect)
        {
            BuildTerrainList();

            listing.Gap(6f);
            listing.Label("Barefoot speed on each terrain. 1.00x is no penalty. Rough stone and gravel lead the list.");

            Rect outRect = listing.GetRect(Mathf.Max(120f, inRect.height - listing.CurHeight - 40f));
            Rect viewRect = new Rect(0f, 0f, outRect.width - 18f, penaltyTerrains.Count * 32f);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            Listing_Standard scroll = new Listing_Standard();
            scroll.Begin(viewRect);

            for (int i = 0; i < penaltyTerrains.Count; i++)
            {
                TerrainDef t = penaltyTerrains[i];
                if (!settings.barefootPenalties.TryGetValue(t.defName, out float val))
                {
                    val = ApparelModifier.GetDefaultBarefootPenalty(t);
                    settings.barefootPenalties[t.defName] = val;
                }
                settings.barefootPenalties[t.defName] = scroll.SliderLabeled($"{t.LabelCap} ({val:F2}x)", val, 0.50f, 1.00f);
            }

            scroll.End();
            Widgets.EndScrollView();
        }
    }
}
