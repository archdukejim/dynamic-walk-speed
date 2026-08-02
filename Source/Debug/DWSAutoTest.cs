using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using DynamicWalkSpeeds.Modifiers;
using static DynamicWalkSpeeds.Modifiers.ApparelModifier;

namespace DynamicWalkSpeeds.Debugging
{
    /// <summary>
    /// Headless test entry point. Does nothing unless RimWorld is launched with -dws-test.
    /// Emits [SYNAPSE-TEST] lines so the existing readlog.ps1 classifier parses the results,
    /// then shuts the game down so the launcher sees a clean exit instead of a timeout.
    /// </summary>
    public class DWSAutoTest : GameComponent
    {
        private const string TestArg = "dws-test";
        private const string OffArg = "dws-off";
        private const int WarmupFrames = 30;
        private const int ForceExitFrames = 240;
        private const int TimedTicks = 4000;

        private static int passed, failed;

        private bool armed;
        private bool modifiersOff;
        private bool suiteDone;
        private bool completed;
        private int framesWaited;
        private int framesSinceShutdown = -1;

        private int timingStartTick = -1;
        private System.Diagnostics.Stopwatch timingClock;

        public DWSAutoTest(Game game) { }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            if (!GenCommandLine.CommandLineArgPassed(TestArg)) return;

            armed = true;
            modifiersOff = GenCommandLine.CommandLineArgPassed(OffArg);

            if (modifiersOff)
            {
                DynamicWalkSpeedsSettings s = DynamicWalkSpeedsMod.settings;
                if (s != null)
                {
                    s.enableWeatherModifiers = false;
                    s.enableFloorModifiers = false;
                    s.enableSurfacePenalties = false;
                    s.enableTerritoryModifiers = false;
                    s.enableCreatureModifiers = false;
                    s.enableFootwearTraction = false;
                    s.enableBarefootPenalty = false;
                    SpeedCaches.InvalidateSettings();
                }
                Log.Message($"[SYNAPSE-TEST] DWS auto-test armed with ALL MODIFIERS OFF (-{OffArg}); postfix will early-out.");
            }
            else
            {
                Log.Message($"[SYNAPSE-TEST] DWS auto-test armed (-{TestArg}); warming up {WarmupFrames} frames.");
            }
        }

        public override void GameComponentUpdate()
        {
            if (!armed) return;

            if (completed)
            {
                if (framesSinceShutdown >= 0 && ++framesSinceShutdown > ForceExitFrames)
                {
                    framesSinceShutdown = -1;
                    Log.Message("[SYNAPSE-TEST] Shutdown did not complete; forcing exit.");
                    Environment.Exit(0);
                }
                return;
            }

            if (framesWaited < WarmupFrames) { framesWaited++; return; }

            if (!suiteDone)
            {
                suiteDone = true;

                if (!modifiersOff)
                {
                    try { RunAll(); }
                    catch (Exception e) { Fail("harness", e.ToString()); }
                }

                // Timed phase: let the colony run and count how often the postfix fires.
                DWSProfiler.Calls = 0;
                DWSProfiler.Active = true;
                timingStartTick = Find.TickManager.TicksGame;
                timingClock = System.Diagnostics.Stopwatch.StartNew();

                Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;
                Log.Message($"[SYNAPSE-TEST] timing phase started for {TimedTicks} ticks at Ultrafast (modifiers {(modifiersOff ? "OFF" : "ON")}).");
                return;
            }

            int elapsedTicks = Find.TickManager.TicksGame - timingStartTick;
            if (elapsedTicks < TimedTicks) return;

            timingClock.Stop();
            DWSProfiler.Active = false;
            completed = true;

            double seconds = timingClock.Elapsed.TotalSeconds;
            double tps = seconds > 0 ? elapsedTicks / seconds : 0;
            double callsPerTick = elapsedTicks > 0 ? (double)DWSProfiler.Calls / elapsedTicks : 0;
            double msPerTick = callsPerTick * 400.0 / 1000000.0;

            int pawns = 0;
            foreach (Map m in Find.Maps) pawns += m.mapPawns?.AllPawnsSpawned?.Count ?? 0;

            Log.Message(string.Format(
                "[SYNAPSE-TEST] TIMING mode={0} ticks={1} wallSec={2:F2} tps={3:F1} calls={4} callsPerTick={5:F1} pawns={6} estMsPerTick={7:F4}",
                modifiersOff ? "OFF" : "ON", elapsedTicks, seconds, tps, DWSProfiler.Calls, callsPerTick, pawns, msPerTick));

            Log.Message($"[SYNAPSE-TEST] SUMMARY passed={passed} failed={failed} skipped=0");

            framesSinceShutdown = 0;
            try { Root.Shutdown(); }
            catch (Exception e)
            {
                Log.Warning($"[SYNAPSE-TEST] Root.Shutdown() threw ({e.GetType().Name}); forcing exit.");
                Environment.Exit(0);
            }
        }

        private static void Pass(string name, string detail)
        {
            passed++;
            Log.Message($"[SYNAPSE-TEST] PASS {name} | {detail}");
        }

        private static void Fail(string name, string detail)
        {
            failed++;
            Log.Error($"[SYNAPSE-TEST] FAIL {name} | {detail}");
        }

        private static void Check(string name, bool ok, string detail)
        {
            if (ok) Pass(name, detail); else Fail(name, detail);
        }

        private static bool Near(float a, float b) => Math.Abs(a - b) < 0.0005f;

        private static void RunAll()
        {
            DynamicWalkSpeedsSettings s = DynamicWalkSpeedsMod.settings;
            if (s == null) { Fail("settings", "DynamicWalkSpeedsMod.settings was null"); return; }

            // Barefoot ships disabled as an experimental option. Force it on for the suite so
            // the assertions still exercise the code rather than trivially passing through the
            // disabled early-out, then restore the shipped default.
            bool shippedBarefoot = s.enableBarefootPenalty;
            s.enableBarefootPenalty = true;

            SpeedCaches.InvalidateSettings();
            SpeedTables.EnsureBuilt(s);

            Check("defaults.barefootDisabled", !shippedBarefoot,
                $"barefoot should ship disabled, shipped value was {shippedBarefoot}");
            Check("defaults.moodDisabled",
                !s.enableBarefootMoodPenalty && !s.enablePainfulGroundMood && !s.enableFootInjury,
                $"mood={s.enableBarefootMoodPenalty} soreFeet={s.enablePainfulGroundMood} injury={s.enableFootInjury}");

            RaceClassification();
            TerrainClassification(s);
            PredictedCrossValues(s);
            BarefootAppliesToHumanlikeOnly(s);
            TableMatchesFallback(s);
            GroupCensus();

            try { DWSDebugActions.DumpSpeedTable(); Pass("csv.dump", "speed table written"); }
            catch (Exception e) { Fail("csv.dump", e.Message); }

            try { DWSDebugActions.DumpReferenceTables(); Pass("csv.reference", "weather, terrain, snow and filth tables written"); }
            catch (Exception e) { Fail("csv.reference", e.Message); }

            try { DWSDebugActions.BenchmarkModifiers(); Pass("benchmark", "benchmark completed, see ns/call above"); }
            catch (Exception e) { Fail("benchmark", e.Message); }

            s.enableBarefootPenalty = shippedBarefoot;
            SpeedCaches.InvalidateSettings();
        }

        private static void RaceClassification()
        {
            var expected = new Dictionary<string, string>
            {
                { "Human", "Humanlike_Medium" },
                { "GuineaPig", "Padded_Small" },
                { "Cat", "Padded_Small" },
                { "LabradorRetriever", "Padded_Medium" },
                { "Megasloth", "Padded_Large" },
                { "Muffalo", "Hoofed_Large" },
                { "Goat", "Hoofed_Medium" },
                { "Chicken", "Taloned_Small" },
                { "Megaspider", "Insectoid_Medium" },
                { "Tortoise", "Shelled_Medium" },
                { "Cobra", "Serpentine_Small" },
                { "Mech_Centurion", "Mechanoid_Large" }
            };

            foreach (KeyValuePair<string, string> kv in expected)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(kv.Key);
                if (def == null) { Fail("race." + kv.Key, "def not present"); continue; }

                string actual = CreatureModifier.GetGroupKey(def);
                Check("race." + kv.Key, actual == kv.Value, $"expected {kv.Value}, got {actual}");
            }
        }

        private static void TerrainClassification(DynamicWalkSpeedsSettings s)
        {
            var manufactured = new Dictionary<string, bool>
            {
                { "Soil", false }, { "Gravel", false }, { "Sand", false }, { "Mud", false },
                { "Concrete", true }, { "PavedTile", true }, { "TileSandstone", true },
                { "WoodPlankFloor", true }, { "SterileTile", true }, { "Bridge", true },
                { "Sandstone_Rough", false }, { "Sandstone_Smooth", true }
            };

            foreach (KeyValuePair<string, bool> kv in manufactured)
            {
                TerrainDef t = DefDatabase<TerrainDef>.GetNamedSilentFail(kv.Key);
                if (t == null) { Fail("terrain." + kv.Key, "def not present"); continue; }

                bool man = FloorModifier.IsManufactured(t);
                float mult = FloorModifier.GetFloorMultiplier(t, s);
                float expectMult = kv.Value ? 1.15f : 1.00f;

                Check("terrain." + kv.Key,
                    man == kv.Value && Near(mult, expectMult),
                    $"manufactured expected {kv.Value} got {man}; floorMult expected {expectMult:F3} got {mult:F3}");
            }

            var barefoot = new Dictionary<string, float>
            {
                { "Soil", 1.00f }, { "Gravel", 0.88f }, { "Ice", 0.90f },
                { "BrokenAsphalt", 0.92f }, { "Sandstone_Rough", 0.85f }, { "Concrete", 1.00f },
                // Colour names collide with the terrain patterns; a carpet must never hurt.
                { "CarpetBlueIce", 1.00f }, { "CarpetFineBlueIce", 1.00f }
            };

            foreach (KeyValuePair<string, float> kv in barefoot)
            {
                TerrainDef t = DefDatabase<TerrainDef>.GetNamedSilentFail(kv.Key);
                if (t == null) { Fail("barefoot." + kv.Key, "def not present"); continue; }

                float p = ApparelModifier.GetBarefootPenalty(t, s);
                Check("barefoot." + kv.Key, Near(p, kv.Value), $"expected {kv.Value:F2}, got {p:F2}");
            }
        }

        private static void PredictedCrossValues(DynamicWalkSpeedsSettings s)
        {
            // The headline prediction: a guinea pig is slower on concrete than on soil.
            var cases = new[]
            {
                new { Race = "GuineaPig", Terrain = "Concrete", Expected = 0.8875f },
                new { Race = "Human", Terrain = "Concrete", Expected = 1.1500f },
                new { Race = "Muffalo", Terrain = "Concrete", Expected = 0.9100f },
                new { Race = "Mech_Centurion", Terrain = "Concrete", Expected = 1.1875f },
                new { Race = "Megasloth", Terrain = "Concrete", Expected = 1.0375f },
                new { Race = "GuineaPig", Terrain = "Soil", Expected = 1.0000f }
            };

            foreach (var c in cases)
            {
                ThingDef race = DefDatabase<ThingDef>.GetNamedSilentFail(c.Race);
                TerrainDef terrain = DefDatabase<TerrainDef>.GetNamedSilentFail(c.Terrain);
                if (race == null || terrain == null) { Fail($"cross.{c.Race}.{c.Terrain}", "def not present"); continue; }

                float floor = FloorModifier.GetFloorMultiplier(terrain, s);
                float traction = CreatureModifier.GetTraction(race, s);
                float eff = FloorModifier.IsManufactured(terrain) && floor != 1f
                    ? 1f + (floor - 1f) * traction
                    : floor;

                Check($"cross.{c.Race}.{c.Terrain}", Near(eff, c.Expected),
                    $"effective floor expected {c.Expected:F4}, got {eff:F4} (traction {traction:F2})");
            }
        }

        /// <summary>
        /// Mechanoids carry an apparel tracker but have no feet to hurt. Only humanlikes
        /// should ever read as barefoot.
        /// </summary>
        private static void BarefootAppliesToHumanlikeOnly(DynamicWalkSpeedsSettings s)
        {
            TerrainDef rough = DefDatabase<TerrainDef>.GetNamedSilentFail("Sandstone_Rough");
            if (rough == null) { Fail("barefoot.nonhumanlike", "Sandstone_Rough not present"); return; }

            foreach (string name in new[] { "Mech_Centurion", "Mech_Agrihand", "Muffalo", "GuineaPig" })
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(name);
                if (def == null) { Fail("barefoot.nonhumanlike." + name, "def not present"); continue; }

                PawnKindDef kind = DWSTestSubjects.KindFor(def);
                if (kind == null) { Fail("barefoot.nonhumanlike." + name, "no pawnkind"); continue; }

                Pawn p = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, faction: null, forceGenerateNewPawn: true));
                float m = GetBarefootMultiplier(p, rough, s);
                p.Destroy();

                Check("barefoot.nonhumanlike." + name, Near(m, 1.0f),
                    $"expected no barefoot penalty on rough stone, got {m:F3}");
            }
        }

        /// <summary>
        /// The flat Def.index tables are the riskiest optimisation in the mod: a wrong row is a
        /// silent gameplay bug, not a crash. Verify every def agrees with the resolve path.
        /// </summary>
        private static void TableMatchesFallback(DynamicWalkSpeedsSettings s)
        {
            int checkedTerrains = 0, badTerrains = 0;
            foreach (TerrainDef t in DefDatabase<TerrainDef>.AllDefsListForReading)
            {
                if (!SpeedTables.TryTerrain(t, out SpeedTables.TerrainRow row)) continue;
                checkedTerrains++;

                if (!Near(row.floorMult, FloorModifier.ResolveFloorMultiplier(t, s)) ||
                    row.manufactured != FloorModifier.ResolveManufactured(t) ||
                    !Near(row.barefootPenalty, ApparelModifier.ResolveBarefootPenalty(t, s)))
                {
                    badTerrains++;
                    if (badTerrains <= 5) Log.Warning($"[SYNAPSE-TEST] terrain table mismatch on {t.defName}");
                }
            }
            Check("tables.terrain", badTerrains == 0, $"{checkedTerrains} terrains checked, {badTerrains} mismatched");

            int checkedWeather = 0, badWeather = 0;
            foreach (WeatherDef w in DefDatabase<WeatherDef>.AllDefsListForReading)
            {
                if (!SpeedTables.TryWeather(w, out float mult)) continue;
                checkedWeather++;
                if (!Near(mult, WeatherModifier.ResolveWeatherMultiplier(w, s))) badWeather++;
            }
            Check("tables.weather", badWeather == 0, $"{checkedWeather} weathers checked, {badWeather} mismatched");

            int checkedRaces = 0, badRaces = 0;
            foreach (ThingDef d in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (d.race == null) continue;
                if (!SpeedTables.TryRace(d, out SpeedTables.RaceRow row)) continue;
                checkedRaces++;

                if (!Near(row.traction, CreatureModifier.GetTraction(d, s)) ||
                    !Near(row.speed, CreatureModifier.GetSpeed(d, s)))
                {
                    badRaces++;
                    if (badRaces <= 5) Log.Warning($"[SYNAPSE-TEST] race table mismatch on {d.defName}");
                }
            }
            Check("tables.race", badRaces == 0, $"{checkedRaces} races checked, {badRaces} mismatched");
        }

        private static void GroupCensus()
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            int total = 0;

            foreach (ThingDef d in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (d.race == null || d.race.body == null) continue;
                string key = CreatureModifier.GetGroupKey(d);
                if (key == null) continue;

                total++;
                counts.TryGetValue(key, out int n);
                counts[key] = n + 1;
            }

            List<string> parts = new List<string>();
            foreach (KeyValuePair<string, int> kv in counts) parts.Add($"{kv.Key}={kv.Value}");
            parts.Sort();

            Log.Message($"[SYNAPSE-TEST] census | {total} races across {counts.Count} groups: {string.Join(" ", parts)}");
            Check("census.groups", counts.Count > 0, $"{counts.Count} populated groups over {total} races");
        }
    }
}
