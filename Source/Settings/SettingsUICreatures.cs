using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using DynamicWalkSpeeds.Modifiers;

namespace DynamicWalkSpeeds.Settings
{
    public static class SettingsUICreatures
    {
        private static Vector2 scrollPosition = Vector2.zero;
        private static List<string> populatedGroups;
        private static Dictionary<string, int> groupCounts;
        private static List<ThingDef> races;
        private static bool showOverrides;

        private static void BuildCache()
        {
            if (races != null) return;

            races = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(d => d.race != null && d.race.body != null)
                .OrderBy(d => d.label)
                .ToList();

            groupCounts = new Dictionary<string, int>();
            for (int i = 0; i < races.Count; i++)
            {
                string key = CreatureModifier.GetGroupKey(races[i]);
                if (key == null) continue;
                groupCounts.TryGetValue(key, out int n);
                groupCounts[key] = n + 1;
            }

            populatedGroups = new List<string>();
            foreach (string body in CreatureModifier.BodyTypes)
            {
                foreach (string band in CreatureModifier.SizeBands)
                {
                    string key = body + "_" + band;
                    if (groupCounts.ContainsKey(key)) populatedGroups.Add(key);
                }
            }
        }

        public static void DrawCreaturesTab(Listing_Standard listing, DynamicWalkSpeedsSettings settings, Rect inRect)
        {
            BuildCache();

            listing.CheckboxLabeled("Enable Creature Traction and Speed", ref settings.enableCreatureModifiers,
                "Traction re-weights how much manufactured flooring helps or hurts a creature. Speed applies everywhere.");
            if (!settings.enableCreatureModifiers) return;

            listing.Label("Traction: 1.00x gives the full floor bonus, 0.00x ignores floors, negative turns the bonus into a penalty.");
            listing.CheckboxLabeled("Show individual creature overrides", ref showOverrides);
            listing.Gap(6f);

            float rowH = 32f;
            float groupBlock = 26f + rowH + rowH + 10f;
            float viewH = populatedGroups.Count * groupBlock;
            if (showOverrides)
            {
                viewH += 34f;
                for (int i = 0; i < races.Count; i++)
                {
                    viewH += 28f;
                    if (settings.raceTractionOverrides.ContainsKey(races[i].defName)) viewH += rowH + rowH;
                }
            }

            Rect outRect = listing.GetRect(inRect.height - listing.CurHeight - 40f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 18f, viewH);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            Listing_Standard scroll = new Listing_Standard();
            scroll.Begin(viewRect);

            for (int i = 0; i < populatedGroups.Count; i++)
            {
                string key = populatedGroups[i];
                groupCounts.TryGetValue(key, out int count);

                Text.Font = GameFont.Small;
                scroll.Label($"<b>{key.Replace("_", " / ")}</b>  ({count} creatures)");

                if (!settings.creatureTraction.TryGetValue(key, out float traction))
                {
                    traction = CreatureModifier.GetDefaultTraction(key);
                    settings.creatureTraction[key] = traction;
                }
                settings.creatureTraction[key] = scroll.SliderLabeled($"    Traction ({traction:F2}x)", traction,
                    CreatureModifier.MinTraction, CreatureModifier.MaxTraction);

                if (!settings.creatureSpeed.TryGetValue(key, out float speed))
                {
                    speed = CreatureModifier.GetDefaultSpeed(key);
                    settings.creatureSpeed[key] = speed;
                }
                settings.creatureSpeed[key] = scroll.SliderLabeled($"    Speed ({speed:F2}x)", speed,
                    CreatureModifier.MinSpeed, CreatureModifier.MaxSpeed);

                scroll.Gap(10f);
            }

            if (showOverrides)
            {
                scroll.GapLine(12f);
                scroll.Label("Individual creatures (tick to override the group value):");

                for (int i = 0; i < races.Count; i++)
                {
                    ThingDef d = races[i];
                    string key = CreatureModifier.GetGroupKey(d);
                    bool wasOverridden = settings.raceTractionOverrides.ContainsKey(d.defName);
                    bool isOverridden = wasOverridden;

                    scroll.CheckboxLabeled($"{d.LabelCap}  ({key})", ref isOverridden);

                    if (isOverridden && !wasOverridden)
                    {
                        settings.raceTractionOverrides[d.defName] = CreatureModifier.GetTraction(d, settings);
                        settings.raceSpeedOverrides[d.defName] = CreatureModifier.GetSpeed(d, settings);
                    }
                    else if (!isOverridden && wasOverridden)
                    {
                        settings.raceTractionOverrides.Remove(d.defName);
                        settings.raceSpeedOverrides.Remove(d.defName);
                    }

                    if (isOverridden)
                    {
                        float t = settings.raceTractionOverrides[d.defName];
                        settings.raceTractionOverrides[d.defName] = scroll.SliderLabeled($"    Traction ({t:F2}x)", t,
                            CreatureModifier.MinTraction, CreatureModifier.MaxTraction);

                        float s = settings.raceSpeedOverrides[d.defName];
                        settings.raceSpeedOverrides[d.defName] = scroll.SliderLabeled($"    Speed ({s:F2}x)", s,
                            CreatureModifier.MinSpeed, CreatureModifier.MaxSpeed);
                    }
                }
            }

            scroll.End();
            Widgets.EndScrollView();
        }
    }
}
