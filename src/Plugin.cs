using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace RITE
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGUID = "HT.ReviveHeadInTruckOrExtractionPoint";
        public const string ModName = "ReviveHeadInTruckOrExtractionPoint";
        public const string ModAuthors = "Hypn, Toxin";
        public const string ModVersion = "1.4.0";

        private readonly Harmony harmony = new Harmony(ModGUID);

        internal static Plugin Instance { get; private set; }
        public static ManualLogSource Logger { get; private set; }

        private void Awake()
        {
            RITE.Config.Prepare(Config);
            Instance = this;
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {ModName} v{ModVersion} is loaded! Authors: {ModAuthors}");
            harmony.PatchAll();
        }

        internal void Unpatch()
        {
            harmony.UnpatchSelf();
        }
    }
}
