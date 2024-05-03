﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using ServerSync;
using SkillControl.Managers;
using SkillControl.Professions;
using SkillControl.UI;
using UnityEngine;

namespace SkillControl
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class SkillControlPlugin : BaseUnityPlugin
    {
        internal const string ModName = "SkillControl";
        internal const string ModVersion = "1.0.1";
        internal const string Author = "RustyMods";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);
        public static readonly ManualLogSource SkillControlLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
        public static readonly ConfigSync ConfigSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };
        public enum Toggle { On = 1, Off = 0 }

        public static readonly AssetBundle _AssetBundle = GetAssetBundle("jobsbundle");
        public void Awake()
        {
            Localizer.Load();
            PluginPaths.CreateDirectories();
            FileWaterManager.InitFileWatcher();
            SpriteManager.GetIcons();
            JobManager.InitJobs();
            InitConfigs();
            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        private void Update()
        {
            LoadUI.UpdateUI();
        }

        private static AssetBundle GetAssetBundle(string fileName)
        {
            Assembly execAssembly = Assembly.GetExecutingAssembly();
            string resourceName = execAssembly.GetManifestResourceNames().Single(str => str.EndsWith(fileName));
            using Stream? stream = execAssembly.GetManifestResourceStream(resourceName);
            return AssetBundle.LoadFromStream(stream);
        }
        
        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                SkillControlLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                SkillControlLogger.LogError($"There was an issue loading your {ConfigFileName}");
                SkillControlLogger.LogError("Please check your config entries for spelling and format!");
            }
        }


        #region ConfigOptions

        private static ConfigEntry<Toggle> _serverConfigLocked = null!;
        public static ConfigEntry<KeyCode> _UIKey = null!;
        public static ConfigEntry<int> _JobLimit = null!;
        public static ConfigEntry<string> _Currency = null!;
        public static ConfigEntry<int> _CostToRemove = null!;
        public static ConfigEntry<Toggle> _ForceJob = null!;
        public static ConfigEntry<Vector2> _UIPosition = null!;
        public static ConfigEntry<Toggle> _RemoveSkillExperience = null!;
        public static ConfigEntry<Toggle> _OverrideDefaults = null!;

        private void InitConfigs()
        {
            _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On,
                "If on, the configuration is locked and can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

            _UIKey = config("2 - Settings", "Key", KeyCode.F7, "Set key code to open job menu", false);
            _JobLimit = config("2 - Settings", "Limit", 2, new ConfigDescription("Set the amount of jobs a player can have", new AcceptableValueRange<int>(1, 10)));
            _Currency = config("2 - Settings", "Currency", "Coins", "Set the currency used for skill controller, if invalid, defaults to coins");
            _CostToRemove = config("2 - Settings", "Cost To Remove", 999, new ConfigDescription("Set the cost to remove job", new AcceptableValueRange<int>(0, 999)));
            _ForceJob = config("2 - Settings", "Force Employment", Toggle.Off, "If on, job menu will stay open until player chooses at least one job");
            _UIPosition = config("2 - Settings", "Position", Vector2.zero, "Set the position of the UI", false);
            _UIPosition.SettingChanged += LoadUI.OnPositionChange;
            _RemoveSkillExperience = config("2 - Settings", "Lose Skills", Toggle.Off, "If on, removing jobs, also removes skill experiences");
            _OverrideDefaults = config("2 - Settings", "Override Defaults", Toggle.On,
                "If on, plugin overrides default skill gain");
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order;
            [UsedImplicitly] public bool? Browsable;
            [UsedImplicitly] public string? Category;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer;
        }

        #endregion
    }

    public static class KeyboardExtensions
    {
        public static bool IsKeyDown(this KeyboardShortcut shortcut)
        {
            return shortcut.MainKey != KeyCode.None && Input.GetKeyDown(shortcut.MainKey) &&
                   shortcut.Modifiers.All(Input.GetKey);
        }

        public static bool IsKeyHeld(this KeyboardShortcut shortcut)
        {
            return shortcut.MainKey != KeyCode.None && Input.GetKey(shortcut.MainKey) &&
                   shortcut.Modifiers.All(Input.GetKey);
        }
    }
}