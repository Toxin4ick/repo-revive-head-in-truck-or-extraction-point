using BepInEx.Logging;
using BepConfig = BepInEx.Configuration;

namespace RITE
{
    public static class Config
    {
        public static BepConfig.ConfigEntry<int> ReviveCooldownSeconds {get; private set;}

        public static BepConfig.ConfigEntry<bool> InstaRevive {get; private set;}

        public static BepConfig.ConfigEntry<float> ReviveCostMultiplier {get; private set;}

        public static BepConfig.ConfigEntry<float> HealMultiplier {get; private set;}

        public static void Prepare(BepConfig.ConfigFile bConfig)
        {
            ReviveCooldownSeconds = bConfig.Bind(
                "Revive",
                "ReviveCooldownSeconds",
                300,
                new BepConfig.ConfigDescription(
                    "How long should one wait after the last revive", 
                    new BepConfig.AcceptableValueRange<int>(1, 600)
                )
            );

            InstaRevive = bConfig.Bind(
                "Revive",
                "InstaRevive",
                false,
                new BepConfig.ConfigDescription(
                    "If true, players will be revived instantly without speak", 
                    new BepConfig.AcceptableValueList<bool>(true, false)
                )
            );

            ReviveCostMultiplier = bConfig.Bind(
                "Revive",
                "ReviveCostMultiplier",
                1.0f,
                new BepConfig.ConfigDescription(
                    "Multiplier for the revive cost, default is 1.0", 
                    new BepConfig.AcceptableValueRange<float>(0.0f, 10.0f)
                )
            );

            HealMultiplier = bConfig.Bind(
                "Revive",
                "HealMultiplier",
                0.5f,
                new BepConfig.ConfigDescription(
                    "Multiplier for the heal amount on revive, default is 0.5", 
                    new BepConfig.AcceptableValueRange<float>(0.1f, 1.0f)
                )
            );
        }

        // // 3. Этот метод выполнится автоматически, как только значение изменится
        // private static void OnReviveCooldownChanged(object sender, System.EventArgs e)
        // {
        //     // Здесь можно вывести лог или обновить внутренние переменные/таймеры
        //     Logger.CreateLogSource("RITE.Config").LogInfo(
        //         $"[Config] Кулдаун возрождения был изменен на лету! Новое значение: {ReviveCooldownSeconds.Value}"
        //     );
        // }
    }
}