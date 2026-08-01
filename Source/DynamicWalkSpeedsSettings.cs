using System.Collections.Generic;
using Verse;

namespace DynamicWalkSpeeds
{
    public class DynamicWalkSpeedsSettings : ModSettings
    {
        public bool enableWeatherModifiers = true;
        public Dictionary<string, float> weatherMultipliers = new Dictionary<string, float>();

        public bool enableFloorModifiers = true;
        public float masterFloorScale = 1.0f;
        public bool linkFloors = true;
        public Dictionary<string, float> floorMultipliers = new Dictionary<string, float>();

        public bool enableSurfacePenalties = true;
        public float snowPenaltyScale = 1.0f;
        public float filthPenaltyScale = 1.0f;

        public bool enableTerritoryModifiers = true;
        public bool linkTerritoryTriggers = true;
        public bool hostileMapTileTrigger = true;
        public bool activeHostilePawnsTrigger = true;
        public float hostileTerritoryMultiplier = 0.90f;

        public bool enableCreatureModifiers = true;
        public Dictionary<string, float> creatureTraction = new Dictionary<string, float>();
        public Dictionary<string, float> creatureSpeed = new Dictionary<string, float>();
        public Dictionary<string, float> raceTractionOverrides = new Dictionary<string, float>();
        public Dictionary<string, float> raceSpeedOverrides = new Dictionary<string, float>();

        public bool enableFootwearTraction = true;
        public float footwearBaseTraction = 0.25f;
        public bool footwearQualityMatters = true;
        public bool footwearWearMatters = false;
        public List<string> footwearBodyPartGroups = new List<string> { "Feet" };
        public Dictionary<string, float> footwearTraction = new Dictionary<string, float>();

        public bool enableBarefootPenalty = true;
        public float barefootPenaltyScale = 1.0f;
        public bool barefootQualityShields = true;
        public Dictionary<string, float> barefootPenalties = new Dictionary<string, float>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref enableWeatherModifiers, "enableWeatherModifiers", true);
            Scribe_Collections.Look(ref weatherMultipliers, "weatherMultipliers", LookMode.Value, LookMode.Value);

            Scribe_Values.Look(ref enableFloorModifiers, "enableFloorModifiers", true);
            Scribe_Values.Look(ref masterFloorScale, "masterFloorScale", 1.0f);
            Scribe_Values.Look(ref linkFloors, "linkFloors", true);
            Scribe_Collections.Look(ref floorMultipliers, "floorMultipliers", LookMode.Value, LookMode.Value);

            Scribe_Values.Look(ref enableSurfacePenalties, "enableSurfacePenalties", true);
            Scribe_Values.Look(ref snowPenaltyScale, "snowPenaltyScale", 1.0f);
            Scribe_Values.Look(ref filthPenaltyScale, "filthPenaltyScale", 1.0f);

            Scribe_Values.Look(ref enableTerritoryModifiers, "enableTerritoryModifiers", true);
            Scribe_Values.Look(ref linkTerritoryTriggers, "linkTerritoryTriggers", true);
            Scribe_Values.Look(ref hostileMapTileTrigger, "hostileMapTileTrigger", true);
            Scribe_Values.Look(ref activeHostilePawnsTrigger, "activeHostilePawnsTrigger", true);
            Scribe_Values.Look(ref hostileTerritoryMultiplier, "hostileTerritoryMultiplier", 0.90f);

            Scribe_Values.Look(ref enableCreatureModifiers, "enableCreatureModifiers", true);
            Scribe_Collections.Look(ref creatureTraction, "creatureTraction", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref creatureSpeed, "creatureSpeed", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref raceTractionOverrides, "raceTractionOverrides", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref raceSpeedOverrides, "raceSpeedOverrides", LookMode.Value, LookMode.Value);

            Scribe_Values.Look(ref enableFootwearTraction, "enableFootwearTraction", true);
            Scribe_Values.Look(ref footwearBaseTraction, "footwearBaseTraction", 0.25f);
            Scribe_Values.Look(ref footwearQualityMatters, "footwearQualityMatters", true);
            Scribe_Values.Look(ref footwearWearMatters, "footwearWearMatters", false);
            Scribe_Collections.Look(ref footwearBodyPartGroups, "footwearBodyPartGroups", LookMode.Value);
            Scribe_Collections.Look(ref footwearTraction, "footwearTraction", LookMode.Value, LookMode.Value);

            Scribe_Values.Look(ref enableBarefootPenalty, "enableBarefootPenalty", true);
            Scribe_Values.Look(ref barefootPenaltyScale, "barefootPenaltyScale", 1.0f);
            Scribe_Values.Look(ref barefootQualityShields, "barefootQualityShields", true);
            Scribe_Collections.Look(ref barefootPenalties, "barefootPenalties", LookMode.Value, LookMode.Value);

            if (weatherMultipliers == null) weatherMultipliers = new Dictionary<string, float>();
            if (floorMultipliers == null) floorMultipliers = new Dictionary<string, float>();
            if (creatureTraction == null) creatureTraction = new Dictionary<string, float>();
            if (creatureSpeed == null) creatureSpeed = new Dictionary<string, float>();
            if (raceTractionOverrides == null) raceTractionOverrides = new Dictionary<string, float>();
            if (raceSpeedOverrides == null) raceSpeedOverrides = new Dictionary<string, float>();
            if (footwearTraction == null) footwearTraction = new Dictionary<string, float>();
            if (barefootPenalties == null) barefootPenalties = new Dictionary<string, float>();
            if (footwearBodyPartGroups == null || footwearBodyPartGroups.Count == 0)
                footwearBodyPartGroups = new List<string> { "Feet" };
        }
    }
}
