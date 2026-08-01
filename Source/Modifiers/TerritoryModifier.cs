using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace DynamicWalkSpeeds.Modifiers
{
    public static class TerritoryModifier
    {
        private const int HostileScanTtl = 60;
        private const int TileFactionTtl = 250;

        private class MapTerritoryData
        {
            public readonly Dictionary<Faction, int> tickByFaction = new Dictionary<Faction, int>();
            public readonly Dictionary<Faction, bool> resultByFaction = new Dictionary<Faction, bool>();
            public int noFactionTick = -99999;
            public bool noFactionResult;
            public int tileTick = -99999;
            public bool tileResult;
        }

        private static readonly Dictionary<Map, MapTerritoryData> mapCache = new Dictionary<Map, MapTerritoryData>();

        public static void InvalidateSettingsCache()
        {
            mapCache.Clear();
        }

        public static void PruneCache()
        {
            if (mapCache.Count == 0) return;

            List<Map> stale = null;
            foreach (KeyValuePair<Map, MapTerritoryData> pair in mapCache)
            {
                if (pair.Key == null || !Find.Maps.Contains(pair.Key))
                {
                    if (stale == null) stale = new List<Map>();
                    stale.Add(pair.Key);
                }
            }

            if (stale == null) return;
            for (int i = 0; i < stale.Count; i++) mapCache.Remove(stale[i]);
        }

        public static bool IsFleeing(Pawn pawn)
        {
            if (pawn == null) return false;

            if (pawn.CurJobDef == JobDefOf.Flee)
                return true;

            return pawn.MentalStateDef == MentalStateDefOf.PanicFlee;
        }

        public static float GetTerritoryMultiplier(Pawn pawn, DynamicWalkSpeedsSettings settings)
        {
            if (pawn == null || pawn.Map == null || !settings.enableTerritoryModifiers)
                return 1.0f;

            if (settings.territoryFleeingExempt && IsFleeing(pawn))
                return 1.0f;

            Map map = pawn.Map;
            bool isHostileTile = IsHostileMapTileCached(map);

            if (settings.linkTerritoryTriggers)
            {
                if (isHostileTile)
                    return settings.hostileTerritoryMultiplier;

                return HasActiveHostilePawnsCached(map, pawn) ? settings.hostileTerritoryMultiplier : 1.0f;
            }

            if (settings.hostileMapTileTrigger && isHostileTile)
                return settings.hostileTerritoryMultiplier;

            if (settings.activeHostilePawnsTrigger && HasActiveHostilePawnsCached(map, pawn))
                return settings.hostileTerritoryMultiplier;

            return 1.0f;
        }

        private static bool IsHostileMapTileCached(Map map)
        {
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;

            if (!mapCache.TryGetValue(map, out MapTerritoryData data))
            {
                data = new MapTerritoryData();
                mapCache[map] = data;
            }

            if (now - data.tileTick < TileFactionTtl)
                return data.tileResult;

            data.tileResult = IsHostileMapTile(map);
            data.tileTick = now;
            return data.tileResult;
        }

        private static bool IsHostileMapTile(Map map)
        {
            Faction mapFaction = map.ParentFaction;
            return mapFaction != null && mapFaction != Faction.OfPlayer && mapFaction.HostileTo(Faction.OfPlayer);
        }

        private static bool HasActiveHostilePawnsCached(Map map, Pawn pawn)
        {
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;

            if (!mapCache.TryGetValue(map, out MapTerritoryData data))
            {
                data = new MapTerritoryData();
                mapCache[map] = data;
            }

            Faction faction = pawn.Faction;

            if (faction == null)
            {
                if (now - data.noFactionTick < HostileScanTtl)
                    return data.noFactionResult;

                data.noFactionResult = HasActiveHostilePawns(map, pawn);
                data.noFactionTick = now;
                return data.noFactionResult;
            }

            if (data.tickByFaction.TryGetValue(faction, out int stamp) && now - stamp < HostileScanTtl)
                return data.resultByFaction[faction];

            bool result = HasActiveHostilePawns(map, pawn);
            data.tickByFaction[faction] = now;
            data.resultByFaction[faction] = result;
            return result;
        }

        private static bool HasActiveHostilePawns(Map map, Pawn pawn)
        {
            IReadOnlyList<Pawn> allPawns = map.mapPawns?.AllPawnsSpawned;
            if (allPawns == null) return false;

            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn p = allPawns[i];
                if (p != null && !p.Dead && !p.Downed && p.HostileTo(pawn))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
