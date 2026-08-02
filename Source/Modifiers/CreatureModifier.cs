using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DynamicWalkSpeeds.Modifiers
{
    public static class CreatureModifier
    {
        public const float MinTraction = -1.50f;
        public const float MaxTraction = 1.50f;
        public const float MinSpeed = 0.25f;
        public const float MaxSpeed = 2.00f;

        public static readonly string[] BodyTypes =
        {
            "Hoofed", "Padded", "Taloned", "Insectoid", "Shelled", "Serpentine", "Mechanoid", "Vehicle", "Humanlike", "Other"
        };

        public static readonly string[] SizeBands = { "Small", "Medium", "Large" };

        private static readonly Dictionary<ThingDef, string> groupCache = new Dictionary<ThingDef, string>();
        private static readonly Dictionary<ThingDef, float> tractionCache = new Dictionary<ThingDef, float>();
        private static readonly Dictionary<ThingDef, float> speedCache = new Dictionary<ThingDef, float>();

        public static void InvalidateSettingsCache()
        {
            tractionCache.Clear();
            speedCache.Clear();
        }

        private static float GetTractionCached(ThingDef def, DynamicWalkSpeedsSettings settings)
        {
            if (def == null) return 1.00f;

            if (SpeedTables.TryRace(def, out SpeedTables.RaceRow row))
                return row.traction;

            if (tractionCache.TryGetValue(def, out float cached))
                return cached;

            float result = GetTraction(def, settings);
            tractionCache[def] = result;
            return result;
        }

        private static float GetSpeedCached(ThingDef def, DynamicWalkSpeedsSettings settings)
        {
            if (def == null) return 1.00f;

            if (SpeedTables.TryRace(def, out SpeedTables.RaceRow row))
                return row.speed;

            if (speedCache.TryGetValue(def, out float cached))
                return cached;

            float result = GetSpeed(def, settings);
            speedCache[def] = result;
            return result;
        }

        public static string GetGroupKey(ThingDef def)
        {
            if (def == null)
                return null;

            if (groupCache.TryGetValue(def, out string cached))
                return cached;

            string key;
            if (IsVehicleDef(def))
            {
                key = "Vehicle_" + ClassifyFootprint(def);
            }
            else if (def.race == null)
            {
                key = null;
            }
            else
            {
                key = ClassifyBody(def.race) + "_" + ClassifySize(def.race.baseBodySize);
            }

            groupCache[def] = key;
            return key;
        }

        public static bool IsVehicleDef(ThingDef def)
        {
            System.Type t = def.GetType();
            while (t != null)
            {
                if (t.Name == "VehicleDef") return true;
                t = t.BaseType;
            }
            return false;
        }

        public static string ClassifyFootprint(ThingDef def)
        {
            int area = def.size.x * def.size.z;
            if (area <= 2) return "Small";
            if (area <= 6) return "Medium";
            return "Large";
        }

        public static string ClassifyBody(RaceProperties race)
        {
            if (race == null) return "Other";

            FleshTypeDef flesh = race.FleshType;
            if (flesh == FleshTypeDefOf.Mechanoid) return "Mechanoid";
            if (flesh == FleshTypeDefOf.Insectoid) return "Insectoid";
            if (race.Humanlike) return "Humanlike";

            string body = race.body?.defName;
            if (body == null) return "Other";

            if (body.Contains("Hooves")) return "Hoofed";
            if (body.Contains("Paws") || body.Contains("Claws") || body.Contains("Monkey")) return "Padded";
            if (body.Contains("Bird")) return "Taloned";
            if (body.Contains("Snake")) return "Serpentine";
            if (body.Contains("Turtle") || body.Contains("Shell") || body.Contains("Pinniped")) return "Shelled";

            return ClassifyByParts(race.body) ?? "Other";
        }

        private static string ClassifyByParts(BodyDef body)
        {
            List<BodyPartRecord> parts = body?.AllParts;
            if (parts == null) return null;

            bool hoof = false, shell = false, padded = false, talon = false, foot = false;
            for (int i = 0; i < parts.Count; i++)
            {
                string n = parts[i].def?.defName;
                if (n == null) continue;

                if (n.Contains("Hoof")) hoof = true;
                else if (n.Contains("Shell") || n.Contains("Plastron")) shell = true;
                else if (n.Contains("Paw") || n.Contains("Claw")) padded = true;
                else if (n.Contains("Talon") || n.Contains("Beak")) talon = true;
                else if (n.Contains("Foot") || n.Contains("Toe")) foot = true;
            }

            if (hoof) return "Hoofed";
            if (shell) return "Shelled";
            if (padded) return "Padded";
            if (talon) return "Taloned";
            if (foot) return "Padded";
            return null;
        }

        public static string ClassifySize(float bodySize)
        {
            if (bodySize < 0.5f) return "Small";
            if (bodySize < 1.5f) return "Medium";
            return "Large";
        }

        public static float GetDefaultTraction(string groupKey)
        {
            switch (groupKey)
            {
                case "Hoofed_Small": return 0.00f;
                case "Hoofed_Medium": return -0.40f;
                case "Hoofed_Large": return -0.60f;

                case "Padded_Small": return -0.75f;
                case "Padded_Medium": return -0.25f;
                case "Padded_Large": return 0.25f;

                case "Taloned_Small": return -0.60f;
                case "Taloned_Medium": return -0.40f;
                case "Taloned_Large": return -0.30f;

                case "Serpentine_Small":
                case "Serpentine_Medium":
                case "Serpentine_Large": return -0.50f;

                case "Shelled_Small":
                case "Shelled_Medium":
                case "Shelled_Large": return 0.25f;

                case "Mechanoid_Small":
                case "Mechanoid_Medium":
                case "Mechanoid_Large": return 1.25f;

                case "Vehicle_Small":
                case "Vehicle_Medium":
                case "Vehicle_Large": return 1.50f;

                default: return 1.00f;
            }
        }

        public static float GetDefaultSpeed(string groupKey)
        {
            return 1.00f;
        }

        public static float GetTraction(ThingDef def, DynamicWalkSpeedsSettings settings)
        {
            if (def == null) return 1.00f;

            if (settings.raceTractionOverrides.TryGetValue(def.defName, out float over))
                return over;

            string key = GetGroupKey(def);
            if (key == null) return 1.00f;

            if (settings.creatureTraction.TryGetValue(key, out float group))
                return group;

            return GetDefaultTraction(key);
        }

        public static float GetSpeed(ThingDef def, DynamicWalkSpeedsSettings settings)
        {
            if (def == null) return 1.00f;

            if (settings.raceSpeedOverrides.TryGetValue(def.defName, out float over))
                return over;

            string key = GetGroupKey(def);
            if (key == null) return 1.00f;

            if (settings.creatureSpeed.TryGetValue(key, out float group))
                return group;

            return GetDefaultSpeed(key);
        }

        public static float ApplyTraction(Pawn pawn, TerrainDef terrain, float floorMult, DynamicWalkSpeedsSettings settings)
        {
            if (pawn == null || !settings.enableCreatureModifiers || floorMult == 1.0f)
                return floorMult;

            if (!FloorModifier.IsManufactured(terrain))
                return floorMult;

            float traction = GetTractionCached(pawn.def, settings) + ApparelModifier.GetFootwearTraction(pawn, settings);
            traction = UnityEngine.Mathf.Clamp(traction, MinTraction, MaxTraction);
            return 1.0f + (floorMult - 1.0f) * traction;
        }

        public static float GetSpeedMultiplier(Pawn pawn, DynamicWalkSpeedsSettings settings)
        {
            if (pawn == null || !settings.enableCreatureModifiers)
                return 1.0f;

            return GetSpeedCached(pawn.def, settings);
        }
    }
}
