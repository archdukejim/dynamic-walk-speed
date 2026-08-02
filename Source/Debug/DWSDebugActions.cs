using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using LudeonTK;
using RimWorld;
using Verse;
using DynamicWalkSpeeds.Modifiers;

namespace DynamicWalkSpeeds.Debugging
{
    public static class DWSDebugActions
    {
        private const int GenerationSeed = 20260801;

        [DebugAction("Dynamic Walk Speeds", "Dump speed table (CSV)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void DumpSpeedTable()
        {
            DynamicWalkSpeedsSettings settings = DynamicWalkSpeedsMod.settings;
            if (settings == null) return;

            SpeedCaches.InvalidateSettings();
            SpeedTables.EnsureBuilt(settings);

            List<string> missing = new List<string>();
            List<TerrainDef> terrains = DWSTestSubjects.ResolveTerrains(missing);
            List<ThingDef> races = DWSTestSubjects.ResolveRaces(missing);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("race,group,terrain,manufactured,floorMult,tractionFloor,creatureSpeed,barefoot,totalMult,baseTicks,moddedTicks,ratio");

            for (int r = 0; r < races.Count; r++)
            {
                ThingDef race = races[r];
                Pawn pawn = MakeTestPawn(race);
                if (pawn == null) { missing.Add(race.defName + " (no pawnkind)"); continue; }

                string group = CreatureModifier.GetGroupKey(race) ?? "none";
                float baseTicks = pawn.TicksPerMoveCardinal;

                for (int t = 0; t < terrains.Count; t++)
                {
                    TerrainDef terrain = terrains[t];

                    float floorMult = FloorModifier.GetFloorMultiplier(terrain, settings);
                    float tractionFloor = CreatureModifier.ApplyTraction(pawn, terrain, floorMult, settings);
                    float creatureSpeed = CreatureModifier.GetSpeedMultiplier(pawn, settings);
                    float barefoot = ApparelModifier.GetBarefootMultiplier(pawn, terrain, settings);

                    float total = tractionFloor * creatureSpeed * barefoot;
                    if (total <= 0.01f) total = 0.01f;

                    float modded = baseTicks / total;

                    sb.AppendLine(string.Join(",",
                        race.defName,
                        group,
                        terrain.defName,
                        FloorModifier.IsManufactured(terrain) ? "yes" : "no",
                        floorMult.ToString("F3"),
                        tractionFloor.ToString("F3"),
                        creatureSpeed.ToString("F3"),
                        barefoot.ToString("F3"),
                        total.ToString("F3"),
                        baseTicks.ToString("F1"),
                        modded.ToString("F1"),
                        (modded / baseTicks).ToString("F3")));
                }

                pawn.Destroy();
            }

            string path = Path.Combine(GenFilePaths.ConfigFolderPath, "DWS_SpeedTable.csv");
            try
            {
                File.WriteAllText(path, sb.ToString());
                Log.Message("[DWS] Speed table written to " + path);
            }
            catch (Exception e)
            {
                Log.Error("[DWS] Could not write speed table: " + e);
            }

            if (missing.Count > 0)
                Log.Warning("[DWS] Skipped (not present in this modlist): " + string.Join(", ", missing));
        }

        [DebugAction("Dynamic Walk Speeds", "Benchmark modifier chain", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void BenchmarkModifiers()
        {
            DynamicWalkSpeedsSettings settings = DynamicWalkSpeedsMod.settings;
            Map map = Find.CurrentMap;
            if (settings == null || map == null) return;

            SpeedTables.EnsureBuilt(settings);

            Pawn pawn = null;
            foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
            {
                pawn = p;
                break;
            }
            if (pawn == null) { Log.Warning("[DWS] Benchmark needs a spawned colonist."); return; }

            IntVec3 cell = pawn.Position;
            TerrainDef terrain = cell.GetTerrain(map);

            const int Warmup = 10000;
            const int Iterations = 200000;

            float sink = 0f;
            for (int i = 0; i < Warmup; i++) sink += RunChain(pawn, map, cell, terrain, settings);

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < Iterations; i++) sink += RunChain(pawn, map, cell, terrain, settings);
            sw.Stop();

            double nsPerCall = sw.Elapsed.TotalMilliseconds * 1000000.0 / Iterations;
            Log.Message(string.Format(
                "[DWS] Modifier chain: {0:F1} ns/call over {1} iterations ({2:F1} ms total) on {3} at {4}. Sink {5:F2}",
                nsPerCall, Iterations, sw.Elapsed.TotalMilliseconds, pawn.def.defName, terrain?.defName ?? "null", sink));
        }

        private static float RunChain(Pawn pawn, Map map, IntVec3 cell, TerrainDef terrain, DynamicWalkSpeedsSettings settings)
        {
            float weather = WeatherModifier.GetWeatherMultiplier(map, settings);
            float floor = FloorModifier.GetFloorMultiplier(terrain, settings);
            floor = CreatureModifier.ApplyTraction(pawn, terrain, floor, settings);
            float filth = SurfaceModifier.GetFilthMultiplier(map, cell, settings);
            float territory = TerritoryModifier.GetTerritoryMultiplier(pawn, settings);
            float creature = CreatureModifier.GetSpeedMultiplier(pawn, settings);
            float barefoot = ApparelModifier.GetBarefootMultiplier(pawn, terrain, settings);
            return weather * floor * filth * territory * creature * barefoot
                   + SurfaceModifier.GetSnowTickAdjustment(map, cell, settings);
        }

        [DebugAction("Dynamic Walk Speeds", "Paint terrain test strips", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void PaintTerrainStrips()
        {
            Map map = Find.CurrentMap;
            if (map == null) return;

            List<TerrainDef> terrains = DWSTestSubjects.ResolveTerrains(null);
            IntVec3 origin = UI.MouseCell();

            const int StripLength = 20;

            for (int i = 0; i < terrains.Count; i++)
            {
                for (int x = 0; x < StripLength; x++)
                {
                    IntVec3 cell = origin + new IntVec3(x, 0, i * 2);
                    if (cell.InBounds(map))
                        map.terrainGrid.SetTerrain(cell, terrains[i]);
                }
            }

            Log.Message($"[DWS] Painted {terrains.Count} terrain strips of {StripLength} cells from {origin}.");
        }

        [DebugAction("Dynamic Walk Speeds", "Start 5 minute tick profile", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void StartTickProfile()
        {
            DynamicWalkSpeedsSettings settings = DynamicWalkSpeedsMod.settings;
            string tag = settings != null && SpeedCaches.AnyEnabled(settings) ? "enabled" : "disabled";
            DWSProfiler.Start(5, tag);
        }

        [DebugAction("Dynamic Walk Speeds", "Stop tick profile", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void StopTickProfile()
        {
            DWSProfiler.Stop();
        }

        [DebugAction("Dynamic Walk Speeds", "Clean map for testing", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void CleanMapForTesting()
        {
            Map map = Find.CurrentMap;
            if (map == null) return;

            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                "Dynamic Walk Speeds test cleanup.\n\nThis destroys every plant, wild and hostile pawn, item, corpse, chunk, filth pile and non-player building on this map, and clears all snow.\n\nYour own colonists, animals and buildings are kept. Natural rock is kept.\n\nThis cannot be undone. Use it on a throwaway test colony.",
                () => CleanMap(map),
                true));
        }

        private static void CleanMap(Map map)
        {
            int plants = DestroyGroup(map, ThingRequestGroup.Plant);
            int filth = DestroyGroup(map, ThingRequestGroup.Filth);
            int items = DestroyGroup(map, ThingRequestGroup.HaulableEver);

            List<Pawn> doomed = new List<Pawn>();
            foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
            {
                if (p != null && p.Faction != Faction.OfPlayer)
                    doomed.Add(p);
            }
            for (int i = 0; i < doomed.Count; i++)
            {
                if (!doomed[i].Destroyed) doomed[i].Destroy(DestroyMode.Vanish);
            }

            List<Thing> ruins = new List<Thing>();
            foreach (Thing t in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
            {
                if (t != null && t.Faction != Faction.OfPlayer) ruins.Add(t);
            }
            for (int i = 0; i < ruins.Count; i++)
            {
                if (!ruins[i].Destroyed) ruins[i].Destroy(DestroyMode.Vanish);
            }

            int snowCells = 0;
            foreach (IntVec3 cell in map.AllCells)
            {
                if (map.snowGrid.GetDepth(cell) > 0f)
                {
                    map.snowGrid.SetDepth(cell, 0f);
                    snowCells++;
                }
            }

            SpeedCaches.InvalidateSettings();

            Log.Message(string.Format(
                "[DWS] Map cleaned: {0} plants, {1} filth, {2} items and corpses, {3} non-player pawns, {4} non-player buildings, {5} snowy cells cleared. Mod caches flushed.",
                plants, filth, items, doomed.Count, ruins.Count, snowCells));
        }

        private static int DestroyGroup(Map map, ThingRequestGroup group)
        {
            List<Thing> snapshot = new List<Thing>(map.listerThings.ThingsInGroup(group));
            int count = 0;
            for (int i = 0; i < snapshot.Count; i++)
            {
                Thing t = snapshot[i];
                if (t == null || t.Destroyed) continue;
                t.Destroy(DestroyMode.Vanish);
                count++;
            }
            return count;
        }

        private static Pawn MakeTestPawn(ThingDef race)
        {
            PawnKindDef kind = DWSTestSubjects.KindFor(race);
            if (kind == null) return null;

            Pawn pawn;
            Rand.PushState(GenerationSeed);
            try
            {
                pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    kind,
                    faction: null,
                    forceGenerateNewPawn: true,
                    canGeneratePawnRelations: false,
                    allowDowned: false,
                    allowAddictions: false,
                    fixedBiologicalAge: null,
                    fixedChronologicalAge: null));
            }
            finally
            {
                Rand.PopState();
            }

            if (pawn == null) return null;

            if (pawn.health?.hediffSet?.hediffs != null)
            {
                for (int i = pawn.health.hediffSet.hediffs.Count - 1; i >= 0; i--)
                    pawn.health.RemoveHediff(pawn.health.hediffSet.hediffs[i]);
            }

            pawn.apparel?.DestroyAll();
            pawn.equipment?.DestroyAllEquipment();

            return pawn;
        }
    }
}
