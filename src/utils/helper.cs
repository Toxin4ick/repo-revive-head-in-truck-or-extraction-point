using System;

namespace RITE.Utils
{
    internal static class ReviveHelper
    {

        public static int CalculateReviveCost(int lvl, int count, float customMultiplier)
        {
            if (count == 0) return 0;

            int cappedLvl = Math.Min(lvl, 20);

            
            float baseCost = cappedLvl * 2.5f;
            float multiplier = 1.0f + (count - 1) * 0.2f;

            float finalCost = baseCost * multiplier * customMultiplier;

            return Math.Max(0, (int)finalCost);
        }

        public static void LogInfo(string message)
        {
            RITE.Plugin.Logger.LogInfo($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        }

    }
}