using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DynamicWalkSpeeds.Modifiers
{
    public static class WeatherModifier
    {
        private static readonly Dictionary<WeatherDef, float> weatherCache = new Dictionary<WeatherDef, float>();

        public static void InvalidateSettingsCache()
        {
            weatherCache.Clear();
        }

        public static float GetWeatherMultiplier(Map map, DynamicWalkSpeedsSettings settings)
        {
            if (map == null || !settings.enableWeatherModifiers)
                return 1.0f;

            WeatherDef curWeather = map.weatherManager?.curWeather;
            if (curWeather == null)
                return 1.0f;

            if (SpeedTables.TryWeather(curWeather, out float tabled))
                return tabled;

            if (weatherCache.TryGetValue(curWeather, out float cached))
                return cached;

            float result = ResolveWeatherMultiplier(curWeather, settings);
            weatherCache[curWeather] = result;
            return result;
        }

        internal static float ResolveWeatherMultiplier(WeatherDef weather, DynamicWalkSpeedsSettings settings)
        {
            return settings.weatherMultipliers.TryGetValue(weather.defName, out float mult)
                ? mult
                : GetDefaultWeatherMultiplier(weather);
        }

        public static float GetDefaultWeatherMultiplier(WeatherDef weather)
        {
            if (weather == null) return 1.0f;
            
            float penalty = 0f;
            if (weather.rainRate > 0) penalty += weather.rainRate * 0.10f;
            if (weather.snowRate > 0) penalty += weather.snowRate * 0.15f;
            
            return UnityEngine.Mathf.Clamp(1.0f - penalty, 0.5f, 1.5f);
        }
    }
}
