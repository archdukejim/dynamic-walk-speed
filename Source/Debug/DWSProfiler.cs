using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Verse;

namespace DynamicWalkSpeeds.Debugging
{
    public static class DWSProfiler
    {
        public static bool Active;
        public static long Calls;

        private static Stopwatch clock;
        private static long lastSampleMs;
        private static int lastSampleTick;
        private static long lastSampleCalls;
        private static int stopAtTick;
        private static StringBuilder rows;
        private static string label;

        public static void Start(int minutes, string tag)
        {
            Calls = 0;
            lastSampleCalls = 0;
            label = tag;

            clock = Stopwatch.StartNew();
            lastSampleMs = 0;
            lastSampleTick = Find.TickManager.TicksGame;
            stopAtTick = int.MaxValue;

            rows = new StringBuilder();
            rows.AppendLine("wallSeconds,gameTicks,ticksPerSecond,postfixCalls,callsPerTick,spawnedPawns");

            Active = true;
            Log.Message($"[DWS] Tick profile started ({tag}). Sampling every second for {minutes} minutes of wall time. Unpause and set a speed the CPU cannot keep up with, or the tick rate is capped and the comparison shows nothing.");

            stopAtTick = minutes;
        }

        public static void Sample()
        {
            if (!Active || clock == null) return;

            long nowMs = clock.ElapsedMilliseconds;
            if (nowMs - lastSampleMs < 1000) return;

            int nowTick = Find.TickManager.TicksGame;
            long nowCalls = Calls;

            int ticks = nowTick - lastSampleTick;
            long calls = nowCalls - lastSampleCalls;
            double seconds = (nowMs - lastSampleMs) / 1000.0;
            double tps = seconds > 0 ? ticks / seconds : 0;

            int pawns = 0;
            List<Map> maps = Find.Maps;
            if (maps != null)
            {
                for (int i = 0; i < maps.Count; i++)
                {
                    var spawned = maps[i].mapPawns?.AllPawnsSpawned;
                    if (spawned != null) pawns += spawned.Count;
                }
            }

            rows.AppendLine(string.Join(",",
                (nowMs / 1000.0).ToString("F1"),
                ticks.ToString(),
                tps.ToString("F1"),
                calls.ToString(),
                (ticks > 0 ? (double)calls / ticks : 0).ToString("F1"),
                pawns.ToString()));

            lastSampleMs = nowMs;
            lastSampleTick = nowTick;
            lastSampleCalls = nowCalls;

            if (nowMs >= stopAtTick * 60000L)
                Stop();
        }

        public static void Stop()
        {
            if (!Active) return;
            Active = false;

            string name = $"DWS_TickProfile_{label}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string path = Path.Combine(GenFilePaths.ConfigFolderPath, name);

            try
            {
                File.WriteAllText(path, rows.ToString());
                Log.Message($"[DWS] Tick profile written to {path}. Total postfix calls: {Calls}.");
            }
            catch (Exception e)
            {
                Log.Error("[DWS] Could not write tick profile: " + e);
            }

            clock = null;
            rows = null;
        }
    }

    public class DWSProfilerComponent : GameComponent
    {
        public DWSProfilerComponent(Game game)
        {
        }

        public override void GameComponentTick()
        {
            DWSProfiler.Sample();
        }
    }
}
