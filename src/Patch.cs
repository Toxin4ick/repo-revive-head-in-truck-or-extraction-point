using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Hypn
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGUID = "HT.ReviveHeadInTruckOrExtractionPoint";
        public const string ModName = "ReviveHeadInTruckOrExtractionPoint (Toxin fork, original: Hypn)";
        public const string ModAuthors = "Hypn, Toxin";
        public const string ModVersion = "1.2.1";

        private readonly Harmony harmony = new Harmony(ModGUID);

        internal static Plugin Instance { get; private set; }
        public static ManualLogSource Logger { get; private set; }

        private void Awake()
        {
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
