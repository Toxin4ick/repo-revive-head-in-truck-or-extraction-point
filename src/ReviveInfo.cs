using System;
using System.Collections.Generic;
using UnityEngine;
using RITE.Utils;
using Patch = RITE.Patches.PlayerDeathHeadPath;

namespace RITE.Models
{
    internal class ReviveInfo
    {
        public PlayerAvatar Player { get; }
        public int Count { get; private set; } = 0;
        public float LastReviveTime { get; private set; } = Time.realtimeSinceStartup;
        public float OutsideLevelTimer { get; private set; } = 0f;
        public bool IsPreparingToRevive { get; private set; } = false;
        public readonly Queue<string> Messages = new Queue<string>();
        public float LastMessageTime { get; private set; } = Time.realtimeSinceStartup;
        public bool IsWaitingForSpeech { get; private set; } = false;
        private const float MessageCooldownSeconds = 30f;
        public float UpdateThrottle { get; set; } = 0f;

        public ReviveInfo(PlayerAvatar player)
        {
            Player = player ?? throw new ArgumentNullException(nameof(player));
        }

        public ReviveInfo SetCount(int c) { Count = c; return this; }
        public ReviveInfo IncCount() { Count++; return this; }
        public ReviveInfo SetLastReviveTime(float t) { LastReviveTime = t; return this; }
        public ReviveInfo SetOutsideLevelTimer(float t) { OutsideLevelTimer = t; return this; }
        public ReviveInfo SetReviving(bool r) { IsPreparingToRevive = r; return this; }

        public ReviveInfo AddMessage(string message)
        {
            Messages.Enqueue(message);
            return this;
        }

        public string GetMessage() => Messages.Dequeue();
        public ReviveInfo ClearMessages() { Messages.Clear(); return this; }
        public ReviveInfo DropReviving() => ClearMessages().SetReviving(false);
        public bool AnyMessages() => Messages.Count > 0;
        public ReviveInfo SetWaitingForSpeech(bool value)
        {
            IsWaitingForSpeech = value;
            return this;
        }

        public ReviveInfo TryAddMessageWithCooldown(string message)
        {
            float now = Time.realtimeSinceStartup;
            if (now - LastMessageTime >= MessageCooldownSeconds)
            {
                AddMessage(message);
                LastMessageTime = now;
            }
            return this;
        }

        public bool CanRevive(int currentLevel, int currentCurrency, uint reviveCooldownSeconds, float customMultiplier)
        {
            PlayerDeathHead head = Player.playerDeathHead;

            if (head.physGrabObject?.impactDetector?.currentCart != null)
            {
                ReviveHelper.LogInfo($"Player \"{Player.playerName}\" inCart");
                DropReviving();
                return false;
            }

            bool inTruck = head.roomVolumeCheck?.inTruck ?? false;
            bool inExtractionPoint = head.roomVolumeCheck?.inExtractionPoint ?? false;

            if (!inTruck && !inExtractionPoint)
            {
                ReviveHelper.LogInfo($"Player \"{Player.playerName}\" is not in the truck or at the extraction point");
                DropReviving();
                return false;
            }

            if (!CooldownPassed(reviveCooldownSeconds))
            {
                return false;
            }

            if (!IsCostPayable(currentLevel, currentCurrency, customMultiplier))
            {
                return false;
            }

            return true;
        }

        private bool IsCostPayable(int currentLevel, int currentCurrency, float customMultiplier)
        {
            int reviveCost = ReviveHelper.CalculateReviveCost(currentLevel, Count, customMultiplier);

            if (currentCurrency >= reviveCost)
            {
                return true;
            }

            DropReviving();
            TryAddMessageWithCooldown($"I can't be revived yet — not enough money. Cost: ${reviveCost}k, available: ${currentCurrency}");
            ReviveHelper.LogInfo($"Player \"{Player.playerName}\" cannot be revived, not enough money. Revive cost: ${reviveCost}k. Current currency: ${currentCurrency}k");
            return false;
        }

        private bool CooldownPassed(uint reviveCooldownSeconds)
        {
            if (Count == 0) return true;

            float elapsed = Time.realtimeSinceStartup - LastReviveTime;
            if (elapsed >= reviveCooldownSeconds)
            {
                return true;
            }

            float remaining = reviveCooldownSeconds - elapsed;
            DropReviving();
            TryAddMessageWithCooldown($"I can't be revived yet, {Mathf.CeilToInt(remaining)} seconds left");
            ReviveHelper.LogInfo($"Player \"{Player.playerName}\" cannot be revived, cooldown: {Mathf.CeilToInt(remaining)} seconds left");
            return false;
        }
    }
}