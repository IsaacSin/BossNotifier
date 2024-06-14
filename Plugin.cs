﻿using BepInEx;
using Aki.Reflection.Patching;
using System.Reflection;
using UnityEngine;
using EFT.Communications;
using EFT;
using System.Collections.Generic;
using BepInEx.Configuration;
using Comfort.Common;
using BepInEx.Logging;
using Aki.Reflection.Utils;
using System.Text;

namespace BossNotifier {
    [BepInPlugin("Mattdokn.BossNotifier", "BossNotifier", "1.4.1")]
    public class BossNotifierPlugin : BaseUnityPlugin {
        // Configuration entries
        public static ConfigEntry<KeyboardShortcut> showBossesKeyCode;
        public static ConfigEntry<bool> showNotificationsOnRaidStart;
        public static ConfigEntry<int> intelCenterUnlockLevel;
        // public static ConfigEntry<bool> showBossLocation;
        public static ConfigEntry<int> intelCenterLocationUnlockLevel;
        // public static ConfigEntry<bool> showBossDetected;
        public static ConfigEntry<int> intelCenterDetectedUnlockLevel;

        private static ManualLogSource logger;

        // Logging methods
        public static void Log(LogLevel level, string msg) {
            logger.Log(level, msg);
        }

        // Dictionary mapping boss types to names
        public static readonly Dictionary<WildSpawnType, string> bossNames = new Dictionary<WildSpawnType, string>() {
            { WildSpawnType.bossBully, "Reshala" },
            { WildSpawnType.bossKnight, "Goons" },
            { WildSpawnType.sectantPriest, "Cultists" },
            { WildSpawnType.bossTagilla, "Tagilla" },
            { WildSpawnType.bossKilla, "Killa" },
            { WildSpawnType.bossZryachiy, "Zryachiy" },
            { WildSpawnType.bossGluhar, "Glukhar" },
            { WildSpawnType.bossSanitar, "Sanitar" },
            { WildSpawnType.bossKojaniy, "Shturman" },
            { WildSpawnType.bossBoar, "Kaban" },
            { WildSpawnType.gifter, "Santa Claus" },
            { WildSpawnType.arenaFighterEvent, "Bloodhounds" },
            { WildSpawnType.crazyAssaultEvent, "Crazy Scavs" },
            { WildSpawnType.exUsec, "Rogues" },
            { WildSpawnType.bossKolontay, "Kollontay" },
            { (WildSpawnType)4206927, "Punisher" },
            { (WildSpawnType)199, "Legion" },
        };
        // Set of plural boss names
        public static readonly HashSet<string> pluralBosses = new HashSet<string>() {
            "Goons",
            "Cultists",
            "Bloodhounds",
            "Crazy Scavs",
            "Rogues",
        };
        // Dictionary mapping zone IDs to names
        public static readonly Dictionary<string, string> zoneNames = new Dictionary<string, string>() {
            // Factory
            { "BotZone", "" },
            // Customs
            { "ZoneBlockPost", "Military Checkpoint" },
            { "ZoneBrige", "Bridge / Sniper Roadblock" },
            { "ZoneCrossRoad", "Bus Station" },
            { "ZoneCustoms", "Customs / Storage" }, // ZoneCustoms covers most of the area east of the river
            { "ZoneDormitory", "Dorms" },
            { "ZoneFactoryCenter", "Warehouse 4 / ZB-1012" },
            { "ZoneFactorySide", "USEC Stash / ZB-1011" },
            { "ZoneGasStation", "New Gas Station" },
            { "ZoneOldAZS", "Old Gas Station" },
            { "ZoneScavBase", "Fortress" },
            { "ZoneTankSquare", "Old Construction" },
            { "ZoneWade", "RUAF Roadblock" },
            // Woods
            { "ZoneBigRocks", "Mountain Stash" },
            { "ZoneBrokenVill", "Sunken Village" },
            { "ZoneClearVill", "Scav Town" },
            { "ZoneHouse", "Scav House" },
            { "ZoneMiniHouse", "Near USEC Checkpoint" },
            { "ZoneRedHouse", "UN Roadblock" },
            { "ZoneRoad", "RUAF Gate / EMERCOM Base" }, // Also on Interchange
            { "ZoneScavBase2", "Scav Bunker" },
            { "ZoneWoodCutter", "Sawmill" },
            // Shoreline
            { "ZoneBunker", "Bunker" },
            { "ZoneBusStation", "Bus Station" },
            { "ZoneForestGasStation", "Forest Near Gas Station" },
            { "ZoneForestSpawn", "North Forest Road" },
            { "ZoneForestTruck", "West Drone" },
            // { "ZoneGasStation", "Gas Station" }, // Already on Customs
            { "ZoneGreenHouses", "Cottages" },
            { "ZoneMeteoStation", "Weather Station" },
            { "ZonePassClose", "Road to Customs" },
            { "ZonePort", "Pier" },
            { "ZonePowerStation", "Power Station" },
            { "ZoneRailWays", "Crane" },
            { "ZoneSanatorium1", "Resort West" },
            { "ZoneSanatorium2", "Resort East" },
            { "ZoneSmuglers", "Smuggler's Path" },
            { "ZoneStartVillage", "Village" },
            { "ZoneTunnel", "Tunnel" },
            { "ZoneIsland", "Scav Island" },
            // Interchange
            { "ZoneCenter", "Mall Second Floor" },
            { "ZoneCenterBot", "Mall First/Second Floor" }, // ZoneCenterBot has spawns on first and second floor.
            { "ZoneGoshan", "Goshan" },
            { "ZoneIDEA", "IDEA" },
            { "ZoneIDEAPark", "IDEA Parking Garage" },
            { "ZoneOLI", "OLI" },
            { "ZoneOLIPark", "OLI Parking Garage" },
            // { "ZonePowerStation", "Power Station" }, // Already on Shoreline
            { "ZoneTrucks", "OLI Loading Dock" },
            // { "ZoneRoad", "Scav Camp" }, // Also on Woods
            // The Lab
            { "BotZoneBasement", "Technical Level" },
            { "BotZoneFloor1", "First Level" },
            { "BotZoneFloor2", "Second Level" },
            { "BotZoneGate1", "Hangar Gate" },
            { "BotZoneGate2", "Parking Gate" },
            // Reserve
            { "ZoneBarrack", "Black Bishop/Black Pawn Barracks" },
            { "ZonePTOR1", "Black Knight Garage" },
            { "ZonePTOR2", "White Knight Garage" },
            { "ZoneRailStrorage", "Train Station / K Buildings" },
            { "ZoneSubCommand", "Д2 Command Bunkers" },
            { "ZoneSubStorage", "Д Warehouse Bunkers" },
            // Lighthouse
            { "Zone_Blockpost", "Water Treatment South Gate" },
            { "Zone_Chalet", "Blue Resort Chalet" },
            { "Zone_Containers", "Armored Train Yard" },
            { "Zone_DestroyedHouse", "Near Landing Stage" },
            { "Zone_Helicopter", "Water Treatment Helicopter" },
            { "Zone_Island", "Lighthouse" },
            { "Zone_LongRoad", "Long Road" },
            { "Zone_Rocks", "Tennis USEC Chalet" },
            { "Zone_RoofBeach", "Water Treatment Plant 1" },
            { "Zone_RoofContainers", "Water Treatment Plant 2" },
            { "Zone_RoofRocks", "Water Treatment Plant 3" },
            { "Zone_TreatmentBeach", "Water Treatment Plant 1" },
            { "Zone_TreatmentContainers", "Water Treatment Plant 2" },
            { "Zone_TreatmentRocks", "Water Treatment Plant 3" },
            { "Zone_Village", "Hillside Village" },
            { "Zone_Bridge", "Convenience Store / Bridge" },
            { "Zone_OldHouse", "Hermit Village" },
            // Streets of Tarkov
            { "ZoneCard1", "Cardinal Apartments" },
            { "ZoneCarShowroom", "Lexos" },
            { "ZoneCinema", "Cinema" },
            { "ZoneClimova", "Klimov Mall" },
            { "ZoneColumn", "Chek/Primorsky Intersection" },
            { "ZoneConcordia_1", "Concordia" },
            { "ZoneConcordiaParking", "Concordia Underground Parking" },
            { "ZoneConstruction", "Construction" },
            { "ZoneFactory", "Abandoned Factory" },
            { "ZoneHotel_1", "Pinewood Hotel South" },
            { "ZoneHotel_2", "Pinewood Hotel North" },
            { "ZoneMvd", "MVD Academy" },
            { "ZoneStilo", "Stylobate Building / Klimov Mall" },
            { "ZoneSW00", "Zmeisky Alley West" },
            { "ZoneSW01", "Zmeisky Alley East" },
            // Ground Zero
            { "ZoneSandbox", "" }

        };

        private void Awake() {
            logger = Logger;

            // Initialize configuration entries
            showBossesKeyCode = Config.Bind("General", "Keyboard Shortcut", new KeyboardShortcut(KeyCode.O), "Key to show boss notifications.");
            showNotificationsOnRaidStart = Config.Bind("General", "Show Bosses on Raid Start", true, "Show boss notifications on raid start.");
            // showBossLocation = Config.Bind("Balance", "Show Boss Spawn Location", true, "Show boss locations in notification.");
            // showBossDetected = Config.Bind("In-Raid Updates", "Show Boss Detected Notification", true, "Show detected notification when bosses spawn during the raid.");
            // intelCenterUnlockLevel = Config.Bind("Balance", "Intel Center Level Requirement", 0, "Level to unlock at.");
            intelCenterUnlockLevel = Config.Bind("Intel Center Unlocks (4 means Disabled)", "1. Intel Center Level Requirement", 0, 
                new ConfigDescription("Level to unlock plain notifications at.",
                new AcceptableValueRange<int>(0, 4)));
            // intelCenterLocationUnlockLevel = Config.Bind("Balance", "Intel Center Location Level Requirement", 0, "Unlocks showing boss spawn location.");
            intelCenterLocationUnlockLevel = Config.Bind("Intel Center Unlocks (4 means Disabled)", "2. Intel Center Location Level Requirement", 0,
                new ConfigDescription("Unlocks showing boss spawn location in notification.",
                new AcceptableValueRange<int>(0, 4)));
            // intelCenterDetectedUnlockLevel = Config.Bind("Intel Center Unlocks", "Intel Center Detection Requirement", 0, "Unlocks showing boss detected notification.");
            intelCenterDetectedUnlockLevel = Config.Bind("Intel Center Unlocks (4 means Disabled)", "3. Intel Center Detection Requirement", 0, 
                new ConfigDescription("Unlocks showing boss detected notification. (When you get near a boss)", 
                new AcceptableValueRange<int>(0, 4)));


            // Config.Bind("Section", "Key", 1, new ConfigDescription("Description", new AcceptableValueRange<int>(0, 100)));

            // Enable patches
            new BossLocationSpawnPatch().Enable();
            new NewGamePatch().Enable();
            new BotBossPatch().Enable();

            // Subscribe to config changes
            Config.SettingChanged += Config_SettingChanged;

            Logger.LogInfo($"Plugin BossNotifier is loaded!");
        }

        // Event handler for configuration changes
        private void Config_SettingChanged(object sender, SettingChangedEventArgs e) {
            ConfigEntryBase changedSetting = e.ChangedSetting;

            // If player is in a raid, reset their notifications to reflect changes
            if (BossNotifierMono.Instance) BossNotifierMono.Instance.GenerateBossNotifications();
        }

        // Get boss name by type
        public static string GetBossName(WildSpawnType type) {
            // Return boss name if found, otherwise null
            return bossNames.ContainsKey(type) ? bossNames[type] : null;
        }

        // Get zone name by ID
        public static string GetZoneName(string zoneId) {
            // Special case for ZoneGasStation because it's used in Customs and Shoreline
            if (zoneId == "ZoneGasStation") return (Singleton<GameWorld>.Instance.LocationId == "bigmap") ? "New Gas Station" : "Gas Station";
            // Return zone name if found, otherwise clean up the zoneId
            if (zoneNames.ContainsKey(zoneId)) return zoneNames[zoneId];

            string location = zoneId.Replace("Bot", "").Replace("Zone", "");
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < location.Length; i++) {
                char c = location[i];
                if (char.IsUpper(c) && i != 0 && i < location.Length - 1 && !char.IsUpper(location[i + 1]) && !char.IsDigit(location[i + 1])) {
                    sb.Append(" ");
                }
                sb.Append(c);
            }
            return sb.ToString().Replace("_", " ").Trim();
        }
    }

    // Patch for tracking boss location spawns
    internal class BossLocationSpawnPatch : ModulePatch {
        protected override MethodBase GetTargetMethod() => typeof(BossLocationSpawn).GetMethod("Init");

        // Bosses in raid along with their locations ex Key: Reshala Value: Dorms, Gas Station
        public static Dictionary<string, string> bossesInRaid = new Dictionary<string, string>();

        // Add boss spawn if not already present
        private static void TryAddBoss(string boss, string location) {
            if (location == null) {
                Logger.LogError("Tried to add boss with null location.");
                return;
            }
            // If boss is already added
            if (bossesInRaid.ContainsKey(boss)) {
                // If location isn't already present, and location isnt empty, add it.
                if (!bossesInRaid[boss].Contains(location) && !location.Equals("")) {
                    // If the boss has an empty location, set new location
                    if (bossesInRaid[boss].Equals("")) {
                        bossesInRaid[boss] = location;
                    } else {
                        // Otherwise if boss has a location, append our new location
                        bossesInRaid[boss] += ", " + location;
                    }
                }
            } else {
                // Add the boss entry
                bossesInRaid.Add(boss, location);
            }
        }

        // Handle boss location spawns
        [PatchPostfix]
        private static void PatchPostfix(BossLocationSpawn __instance) {
            // If the boss will spawn
            if (__instance.ShallSpawn) {
                // Get it's name, if no name found then return.
                string name = BossNotifierPlugin.GetBossName(__instance.BossType);
                if (name == null) return;

                // Get the spawn location
                string location = BossNotifierPlugin.GetZoneName(__instance.BornZone);

                BossNotifierPlugin.Log(LogLevel.Debug, $"Boss {name} @ zone {__instance.BornZone} translated to {(location == null ? __instance.BornZone.Replace("Bot", "").Replace("Zone", ""): location)}");

                if (location == null) {
                    // If it's null then use cleaned up BornZone
                    TryAddBoss(name, __instance.BornZone.Replace("Bot", "").Replace("Zone", ""));
                } else if (location.Equals("")) {
                    // If it's empty location (Factory Spawn)
                    TryAddBoss(name, "");
                } else {
                    // Location is valid
                    TryAddBoss(name, location);
                }
            }
        }
    }

    // Patch for tracking live boss spawns
    internal class BotBossPatch : ModulePatch {
        protected override MethodBase GetTargetMethod() => typeof(BotBoss).GetConstructors()[0];

        // Bosses spawned in raid
        public static HashSet<string> spawnedBosses = new HashSet<string>();

        [PatchPostfix]
        private static void PatchPostfix(BotBoss __instance) {
            WildSpawnType role = __instance.Owner.Profile.Info.Settings.Role;
            // Get it's name, if no name found then return.
            string name = BossNotifierPlugin.GetBossName(role);
            if (name == null) return;

            // Get the spawn location
            Vector3 positionVector = __instance.Player().Position;
            string position = $"{(int)positionVector.x}, {(int)positionVector.y}, {(int)positionVector.z}";
            // {name} has spawned at (x, y, z) on {map}
            BossNotifierPlugin.Log(LogLevel.Debug, $"{name} has spawned at {position} on {Singleton<GameWorld>.Instance.LocationId}");

            // Add boss to spawnedBosses
            spawnedBosses.Add(name);

            if (BossNotifierMono.Instance.intelCenterLevel >= BossNotifierPlugin.intelCenterDetectedUnlockLevel.Value) {
                NotificationManagerClass.DisplayMessageNotification($"{name} {(BossNotifierPlugin.pluralBosses.Contains(name) ? "have" : "has")} been detected in your vicinity.", ENotificationDurationType.Long);
                BossNotifierMono.Instance.GenerateBossNotifications();
            }
        }
    }

    // Patch for hooking when a raid is started
    internal class NewGamePatch : ModulePatch {
        protected override MethodBase GetTargetMethod() => typeof(GameWorld).GetMethod("OnGameStarted");

        [PatchPrefix]
        public static void PatchPrefix() {
            // Start BossNotifierMono
            BossNotifierMono.Init();
        }
    }

    // Monobehavior for boss notifier
    class BossNotifierMono : MonoBehaviour {
        // Required to invalidate notification cache on settings changed event.
        public static BossNotifierMono Instance;
        // Caching the notification messages
        private List<string> bossNotificationMessages;
        // Intel Center level, only updated when raid is entered.
        public int intelCenterLevel;

        private void SendBossNotifications() {
            if (intelCenterLevel < BossNotifierPlugin.intelCenterUnlockLevel.Value) return;

            // If we have no notifications to display, send one saying there's no bosses located.
            if (bossNotificationMessages.Count == 0) {
                NotificationManagerClass.DisplayMessageNotification("No Bosses Located", ENotificationDurationType.Long);
                return;
            }

            foreach (var bossMessage in bossNotificationMessages) {
                NotificationManagerClass.DisplayMessageNotification(bossMessage, ENotificationDurationType.Long);
            }
        }

        // Initializes boss notifier mono and attaches it to the game world object
        public static void Init() {
            if (Singleton<GameWorld>.Instantiated) {
                Instance = Singleton<GameWorld>.Instance.GetOrAddComponent<BossNotifierMono>();
                BossNotifierPlugin.Log(LogLevel.Debug, $"Game started on map {Singleton<GameWorld>.Instance.LocationId}");
                if (ClientAppUtils.GetMainApp().GetClientBackEndSession() == null) {
                    Instance.intelCenterLevel = 0;
                } else {
                    Instance.intelCenterLevel = ClientAppUtils.GetMainApp().GetClientBackEndSession().Profile.Hideout.Areas[11].level;
                }
            }
        }

        public void Start() {
            GenerateBossNotifications();

            if (!BossNotifierPlugin.showNotificationsOnRaidStart.Value) return;
            Invoke("SendBossNotifications", 2f);
        }

        public void Update() {
            if (IsKeyPressed(BossNotifierPlugin.showBossesKeyCode.Value)) {
                SendBossNotifications();
            }
        }

        public void OnDestroy() {
            // Clear out boss locations for this raid
            BossLocationSpawnPatch.bossesInRaid.Clear();
            // Clear out spawned bosses for this raid
            BotBossPatch.spawnedBosses.Clear();
        }

        public void GenerateBossNotifications() {
            // Clear out boss notification cache
            bossNotificationMessages = new List<string>();

            // Check if it's daytime to prevent showing Cultist notif.
            // This is the same method that DayTimeCultists patches so if that mod is installed then this always returns false
            bool isDayTime = Singleton<IBotGame>.Instance.BotsController.ZonesLeaveController.IsDay();

            // Get whether location is unlocked or not.
            bool isLocationUnlocked = intelCenterLevel >= BossNotifierPlugin.intelCenterLocationUnlockLevel.Value;

            // Get whether detection is unlocked or not.
            bool isDetectionUnlocked = intelCenterLevel >= BossNotifierPlugin.intelCenterDetectedUnlockLevel.Value;

            foreach (var bossSpawn in BossLocationSpawnPatch.bossesInRaid) {
                // If it's daytime then cultists don't spawn
                if (isDayTime && bossSpawn.Key.Equals("Cultists")) continue;

                // If boss has been spawned/detected
                bool isDetected = BotBossPatch.spawnedBosses.Contains(bossSpawn.Key);

                string notificationMessage;
                // If we don't have locations or value is null/whitespace
                if (!isLocationUnlocked || bossSpawn.Value == null || bossSpawn.Value.Equals("")) {
                    // Then just show that they spawned and nothing else
                    notificationMessage = $"{bossSpawn.Key} {(BossNotifierPlugin.pluralBosses.Contains(bossSpawn.Key) ? "have" : "has")} been located.{(isDetectionUnlocked && isDetected ? $" ✓" : "")}";
                } else {
                    // Location is unlocked and location isnt null
                    notificationMessage = $"{bossSpawn.Key} {(BossNotifierPlugin.pluralBosses.Contains(bossSpawn.Key) ? "have" : "has")} been located near {bossSpawn.Value}{(isDetectionUnlocked && isDetected ? $" ✓" : "")}";
                }
                BossNotifierPlugin.Log(LogLevel.Debug, notificationMessage);
                // Add notification to cache list
                bossNotificationMessages.Add(notificationMessage);
            }
        }

        // Credit to DrakiaXYZ, thank you!
        bool IsKeyPressed(KeyboardShortcut key) {
            if (!UnityInput.Current.GetKeyDown(key.MainKey)) {
                return false;
            }
            foreach (var modifier in key.Modifiers) {
                if (!UnityInput.Current.GetKey(modifier)) {
                    return false;
                }
            }
            return true;
        }
    }
}
