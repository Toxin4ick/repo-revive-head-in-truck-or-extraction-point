using HarmonyLib;
using SingularityGroup.HotReload;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hypn.Patches
{
    [HarmonyPatch(typeof(PlayerDeathHead), "Update")]
    internal class HypnReviveInTruckAndExtractionPatch
    {
        private class ReviveInfo
        {
            private PlayerAvatar Player;
            private int Count = 0;
            private DateTime LastReviveTime = DateTime.UtcNow;
            private float OutsideLevelTimer = 0f;
            private bool Reviving = false;
            private Queue<string> Messages = new Queue<string>();

            private DateTime lastMessageTime = DateTime.MinValue;

            public PlayerAvatar GetPlayer()
            {
                return Player;
            }

            public int GetCount()
            {
                return Count;
            }

            public ReviveInfo SetCount(int value)
            {
                Count = value;
                return this;
            }

            public ReviveInfo IncCount()
            {
                Count++;
                return this;
            }

            public DateTime GetLastReviveTime()
            {
                return LastReviveTime;
            }

            public ReviveInfo SetLastReviveTime(DateTime time)
            {
                LastReviveTime = time;
                return this;
            }

            public float GetOutsideLevelTimer()
            {
                return OutsideLevelTimer;
            }

            public ReviveInfo SetOutsideLevelTimer(float value)
            {
                OutsideLevelTimer = value;
                return this;
            }

            public bool IsReviving()
            {
                return Reviving;
            }

            public ReviveInfo SetReviving(bool value)
            {
                Reviving = value;
                return this;
            }

            public ReviveInfo AddMessage(string message)
            {
                Messages.Enqueue(message);
                return this;
            }

            public string GetMessage()
            {
                return Messages.Dequeue();
            }

            public ReviveInfo ClearMessages()
            {
                Messages.Clear();
                return this;
            }

            public ReviveInfo DropReviving()
            {
                return ClearMessages().SetReviving(false);
            }

            public bool AnyMessages()
            {
                return Messages.Count > 0;
            }

            public ReviveInfo SetPlayer(PlayerAvatar player)
            {
                Player = player;
                return this;
            }

            public ReviveInfo TryAddMessageWithCooldown(string message)
            {
                DateTime now = DateTime.UtcNow;
                if ((now - lastMessageTime).TotalSeconds >= MessageCooldownSeconds)
                {
                    AddMessage(message);
                    lastMessageTime = now;
                }
                return this;
            }
        }

        private static readonly Dictionary<string, ReviveInfo> _reviveTracker = new Dictionary<string, ReviveInfo>();
        private static readonly object _reviveLock = new object();
        private static readonly int ReviveCooldownSeconds = 300;

        private static readonly float MessageCooldownSeconds = 30f;

        private static int Level = 0;

        private static int Currency = 0;

        private static float updateThrottle = 0f;
        private static readonly string[] Countdown = new string[] { "Five", "Four", "Three", "Two", "One", "Zero" };
        private static void TryTeleportPlayer(ReviveInfo info)
        {
            PlayerAvatar player = info.GetPlayer();
            PlayerDeathHead head = player.playerDeathHead;

            int roomCount = head.roomVolumeCheck?.CurrentRooms?.Count ?? 1;

            if (roomCount > 0)
            {
                info.SetOutsideLevelTimer(0f);
                return;
            }

            info.SetOutsideLevelTimer(info.GetOutsideLevelTimer() + Time.deltaTime);

            if (head.physGrabObject == null)
            {
                LogInfo($"Player \"{player.playerName}\" could not be teleported — no physGrabObject.");
                return;
            }

            if (info.GetOutsideLevelTimer() < 5f)
                return;

            if (player.playerDeathHead?.roomVolumeCheck?.inTruck ?? false)
            {
                LogInfo($"Teleporting player \"{player.playerName}\" to truck");
                head.physGrabObject.Teleport(TruckSafetySpawnPoint.instance.transform.position, TruckSafetySpawnPoint.instance.transform.rotation);
                return;
            }

            LogInfo($"Teleporting player \"{player.playerName}\" to extraction point");
            head.physGrabObject.Teleport(RoundDirector.instance.extractionPointCurrent.safetySpawn.position, RoundDirector.instance.extractionPointCurrent.safetySpawn.rotation);
        }

        private static void Revive(ReviveInfo info)
        {
            if (info.AnyMessages() || PlayerIsTalking(info))
            {
                return;
            }

            PlayerAvatar player = info.GetPlayer();
            int reviveCost = CalculateReviveCost(info);
            Currency = Math.Max(Currency - reviveCost, 0);
            SemiFunc.StatSetRunCurrency(Currency);
            int healAmount = (int)(player.playerHealth.maxHealth * 0.2f);

            info.IncCount().SetLastReviveTime(DateTime.UtcNow).SetReviving(false);

            LogInfo($"Reviving player \"{player.playerName}\" with {healAmount}/{player.playerHealth.maxHealth} health (revives: {info.GetCount()}). Revive cost: {reviveCost}. Currency left: {Currency}.");

            player.Revive(player.playerDeathHead?.roomVolumeCheck?.inTruck ?? false);
            player.playerHealth.HealOther(healAmount - 1, true);

            // Teleport logic if player is outside the map
            TryTeleportPlayer(info);
        }

        private static void Speak(ReviveInfo info)
        {

            if (!info.AnyMessages() || PlayerIsTalking(info))
            {
                return;
            }

            string message = info.GetMessage();
            LogInfo($"Player \"{info.GetPlayer().playerName}\" talk {message}");
            info.GetPlayer().ChatMessageSend(message);
        }

        private static void LogInfo(string message)
        {
            Hypn.Plugin.Logger.LogInfo($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        }

        private static bool PlayerIsTalking(ReviveInfo info)
        {
            if (info.GetPlayer().voiceChat.ttsVoice.isSpeaking)
            {
                LogInfo($"Player \"{info.GetPlayer().playerName}\" is talking");
                return true;
            }

            return false;
        }


        private static void CheckCurrentLevel(int currentlevel)
        {
            if (Level == currentlevel)
            {
                return;
            }

            LogInfo($"New level. Reset count revive");
            Level = currentlevel;
            foreach (ReviveInfo value in _reviveTracker.Values)
            {
                LogInfo($"{value.GetPlayer().name} revive reset");
                value.SetCount(0);
            }
        }

        private static bool CooldownPassed(ReviveInfo info)
        {
            int cooldown = info.GetCount() == 0 ? ReviveCooldownSeconds : (int)(DateTime.UtcNow - info.GetLastReviveTime()).TotalSeconds;

            if (cooldown >= ReviveCooldownSeconds)
            {
                return true;
            }

            LogInfo($"Player \"{info.GetPlayer().playerName}\" cannot be revived, cooldown: {cooldown} .");
            return false;
        }


        private static int CalculateReviveCost(ReviveInfo info)
        {
            return info.GetCount() > 0 ? Math.Min(Level, 20) + info.GetCount() - 1 : 0;
        }

        private static bool IsCostPayable(ReviveInfo info)
        {
            int reviveCost = CalculateReviveCost(info);

            if (Currency >= reviveCost)
            {
                return true;
            }

            LogInfo($"Player \"{info.GetPlayer().playerName}\" cannot be revived, not enough money. Revive cost: ${reviveCost}k. Current currency: ${Currency}k");
            return false;
        }

        private static bool IsCanRevive(ReviveInfo info)
        {

            PlayerDeathHead head = info.GetPlayer().playerDeathHead;

            if (head.physGrabObject?.impactDetector?.currentCart != null ? true : false)
            {
                LogInfo($"Player \"{info.GetPlayer().playerName}\" inCart");
                info.DropReviving();
                return false;
            }

            bool inTruck = head.roomVolumeCheck?.inTruck ?? false;
            bool inExtractionPoint = head.roomVolumeCheck?.inExtractionPoint ?? false;

            if (!inTruck && !inExtractionPoint)
            {
                LogInfo($"Player \"{info.GetPlayer().playerName}\" is not in the truck or at the extraction point");
                info.DropReviving();
                return false;
            }

            if (!CooldownPassed(info))
            {
                info.DropReviving();
                info.TryAddMessageWithCooldown("I can't revive yet, still on cooldown");
                return false;
            }

            if (!IsCostPayable(info))
            {
                info.DropReviving();
                info.TryAddMessageWithCooldown("We are don't have enough currency to revive");
                return false;
            }

            return true;
        }

        private static void StartReviving(ReviveInfo info)
        {
            info.AddMessage($"My revival will cost ${CalculateReviveCost(info)}k");
            info.AddMessage("Reviving");
            foreach (string num in Countdown)
            {
                info.AddMessage(num);
            }
            info.SetReviving(true);
        }

        private static void Postfix(PlayerDeathHead __instance)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer())
                return;

            updateThrottle += Time.deltaTime;
            if (updateThrottle < 1f)
                return;
            updateThrottle = 0f;

            PlayerAvatar? player = __instance?.playerAvatar;
            if (player == null) return;

            string steamID = player.steamID;
            if (string.IsNullOrEmpty(steamID) || __instance == null || !__instance.triggered)
                return;

            lock (_reviveLock)
            {
                _reviveTracker.TryGetValue(steamID, out ReviveInfo info);

                if (info == null)
                {
                    info = new ReviveInfo();
                    _reviveTracker[steamID] = info;
                }

                info.SetPlayer(player);
                CheckCurrentLevel(StatsManager.instance.GetRunStatLevel() + 1);
                Currency = SemiFunc.StatGetRunCurrency();

                Speak(info);

                if (!IsCanRevive(info))
                {
                    return;
                }

                if (info.IsReviving())
                {
                    Revive(info);
                    return;
                }

                StartReviving(info);
            }
        }
    }
}
