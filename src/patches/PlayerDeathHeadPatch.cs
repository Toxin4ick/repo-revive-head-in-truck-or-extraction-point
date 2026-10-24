using HarmonyLib;
using SingularityGroup.HotReload;
using System;
using System.Collections.Generic;
using UnityEngine;
using RITE.Models;
using RITE.Utils;

namespace RITE.Patches
{
    [HarmonyPatch(typeof(PlayerDeathHead), "Update")]
    internal class PlayerDeathHeadPath
    {
        private static readonly Dictionary<string, ReviveInfo> reviveInfos = new Dictionary<string, ReviveInfo>();
        private static readonly object _reviveLock = new object();
        private static uint ReviveCooldownSeconds => (uint)RITE.Config.ReviveCooldownSeconds.Value;

        private static bool InstaRevive => RITE.Config.InstaRevive.Value;

        private static float ReviveCostMultiplier => RITE.Config.ReviveCostMultiplier.Value;

        private static float HealMultiplier => RITE.Config.HealMultiplier.Value;

        private static int CurrentLevel = 0;

        private static int CurrentCurrency => SemiFunc.StatGetRunCurrency();
        private static readonly string[] Countdown = new string[] { "Five", "Four", "Three", "Two", "One" };


        private static void TeleportPlayer(ReviveInfo info)
        {
            PlayerAvatar player = info.Player;
            PlayerDeathHead head = player.playerDeathHead;

            int roomCount = head.roomVolumeCheck?.CurrentRooms?.Count ?? 1;

            if (roomCount > 0)
            {
                info.SetOutsideLevelTimer(0f);
                return;
            }

            info.SetOutsideLevelTimer(info.OutsideLevelTimer + Time.deltaTime);

            if (head.physGrabObject == null)
            {
                ReviveHelper.LogInfo($"Player \"{player.playerName}\" could not be teleported — no physGrabObject.");
                return;
            }

            if (info.OutsideLevelTimer < 5f)
                return;

            if (player.playerDeathHead?.roomVolumeCheck?.inTruck ?? false)
            {
                ReviveHelper.LogInfo($"Teleporting player \"{player.playerName}\" to truck");
                head.physGrabObject.Teleport(TruckSafetySpawnPoint.instance.transform.position, TruckSafetySpawnPoint.instance.transform.rotation);
                return;
            }

            ReviveHelper.LogInfo($"Teleporting player \"{player.playerName}\" to extraction point");
            head.physGrabObject.Teleport(RoundDirector.instance.extractionPointCurrent.safetySpawn.position, RoundDirector.instance.extractionPointCurrent.safetySpawn.rotation);
        }

        private static void Revive(ReviveInfo info)
        {
            if (info.AnyMessages() || PlayerIsTalking(info))
            {
                return;
            }

            int reviveCost = ReviveHelper.CalculateReviveCost(CurrentLevel, info.Count, ReviveCostMultiplier);
            SemiFunc.StatSetRunCurrency(Math.Max(CurrentCurrency - reviveCost, 0));
            int healAmount = (int)(info.Player.playerHealth.maxHealth * HealMultiplier);

            info.IncCount().SetLastReviveTime(Time.realtimeSinceStartup).SetReviving(false);

            ReviveHelper.LogInfo($"Reviving player \"{info.Player.playerName}\" with {healAmount}/{info.Player.playerHealth.maxHealth} health (revives: {info.Count}). Revive cost: {reviveCost}. Currency left: {CurrentCurrency}.");

            info.Player.Revive(info.Player.playerDeathHead?.roomVolumeCheck?.inTruck ?? false);
            info.Player.playerHealth.HealOther(healAmount - 1, true);

            TeleportPlayer(info);
        }

        private static void Speak(ReviveInfo info)
        {
            if (!info.AnyMessages() || PlayerIsTalking(info))
            {
                return;
            }

            string message = info.GetMessage();
            ReviveHelper.LogInfo($"Player \"{info.Player.playerName}\" talk {message}");

            info.Player.ChatMessageSend(message);
        }

        private static bool PlayerIsTalking(ReviveInfo info)
        {
            bool isTalking = info.Player?.voiceChat?.isTalking ?? false;
            bool isSpeaking = info.Player?.voiceChat?.ttsVoice?.isSpeaking ?? false;

            if (isTalking || isSpeaking)
            {
                ReviveHelper.LogInfo($"Player \"{info.Player.playerName}\" is talking");
                return true;
            }

            return false;
        }


        private static void CheckCurrentLevel(int lvl)
        {
            if (CurrentLevel == lvl) { return; }

            ReviveHelper.LogInfo($"New level. Reset count revive");

            CurrentLevel = lvl;

            reviveInfos.Clear();
        }

        private static void StartReviving(ReviveInfo info)
        {
            if (!InstaRevive)
            {
                info.AddMessage($"My revive will cost ${ReviveHelper.CalculateReviveCost(CurrentLevel, info.Count, ReviveCostMultiplier)}k");
                info.AddMessage("Reviving");
                foreach (string num in Countdown)
                {
                    info.AddMessage(num);
                }
            }

            info.SetReviving(true);
        }

        private static ReviveInfo GetReviveInfo(string steamId, PlayerAvatar player)
        {
            reviveInfos.TryGetValue(steamId, out ReviveInfo info);

            if (info != null)
            {
                return info;
            }

            info = new ReviveInfo(player);
            reviveInfos[steamId] = info;
            return info;
        }

        private static void Postfix(PlayerDeathHead __instance)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer())
                return;


            PlayerAvatar? player = __instance?.playerAvatar;
            if (player == null) return;

            string steamId = player.steamID;
            if (string.IsNullOrEmpty(steamId) || __instance == null || !__instance.triggered)
                return;

            lock (_reviveLock)
            {
                CheckCurrentLevel(StatsManager.instance.GetRunStatLevel() + 1);

                ReviveInfo info = GetReviveInfo(steamId, player);

                Speak(info);

                info.UpdateThrottle += Time.deltaTime;
                if (info.UpdateThrottle < 1f)
                    return;
                info.UpdateThrottle = 0f;

                if (!info.CanRevive(CurrentLevel, CurrentCurrency, ReviveCooldownSeconds, ReviveCostMultiplier))
                {
                    return;
                }

                if (info.IsPreparingToRevive)
                {
                    Revive(info);
                    return;
                }

                StartReviving(info);
            }
        }
    }
}
