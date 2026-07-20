using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace QuickSave
{
	[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
	[BepInProcess("AcrossTheObelisk.exe")]
	public class Plugin : BaseUnityPlugin
	{
		internal static int ModDate = int.Parse(DateTime.Today.ToString("yyyyMMdd"));

		private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

		internal static ManualLogSource Log;

		internal static bool EssentialsInstalled = false;

		public static string PluginName;

		public static string PluginVersion;

		public static string PluginGUID;

		public static string debugBase = "com.binbin.quicksave ";

		public static ConfigEntry<bool> EnableMod { get; set; }

		public static ConfigEntry<bool> EnableDebugging { get; set; }

		public static ConfigEntry<bool> EnableCustomSaveNames { get; set; }

		public static ConfigEntry<float> MidCombatHorizontalShift { get; set; }

		public static ConfigEntry<string> SaveFolderName { get; set; }

		public static ConfigEntry<bool> DisableControlClick { get; set; }

		public static ConfigEntry<bool> EnableResetSeed { get; set; }

		private void Awake()
		{
			Log = base.Logger;
			Log.LogInfo("com.binbin.quicksave " + PluginInfo.PLUGIN_VERSION + " (workspace build) has loaded!");
			EnableMod = base.Config.Bind(new ConfigDefinition("Quick Save", "EnableMod"), defaultValue: true, new ConfigDescription("Enables the Mod", null));
			EnableDebugging = base.Config.Bind(new ConfigDefinition("Quick Save", "Enable Debugging"), defaultValue: true, new ConfigDescription("Enables debugging logs.", null));
			EnableCustomSaveNames = base.Config.Bind(new ConfigDefinition("Quick Save", "Enable Custom Save Names"), defaultValue: false, new ConfigDescription("Enables you to choose the name of your saves with the Save Game button.", null));
			MidCombatHorizontalShift = base.Config.Bind(new ConfigDefinition("Quick Save", "Mid Combat UI Shift"), 100f, new ConfigDescription("Shifts the UI to the right in combat. More positive is more to the right, more negative is more to the left. A value of 100 will shift the UI one \"Icon\" to the right.", null));
			SaveFolderName = base.Config.Bind(new ConfigDefinition("Quick Save", "Save Folder Name"), "", new ConfigDescription("Sets the name of your save folder. If left blank, the current seed will be used instead.", null));
			DisableControlClick = base.Config.Bind(new ConfigDefinition("Quick Save", "Disable Control Key Click"), defaultValue: false, new ConfigDescription("If selected, disables the control keys from clicking.", null));
			EnableResetSeed = base.Config.Bind(new ConfigDefinition("Quick Save", "Enable Reset Seed Button"), defaultValue: true, new ConfigDescription("Adds a Reset Seed button in the starting town (before your first battle) that rolls a new game seed and regenerates the map, so you can reroll the path/quests until you find one you like.", null));
			PluginName = PluginInfo.PLUGIN_NAME;
			PluginVersion = PluginInfo.PLUGIN_VERSION;
			PluginGUID = PluginInfo.PLUGIN_GUID;
			if (EnableMod.Value)
			{
				if (EssentialsCompatibility.Enabled)
				{
					EssentialsCompatibility.EssentialsRegister();
				}
				else
				{
					LogDebug("Essentials is not installed. Mod will load normally, but Essentials features are unavailable.");
				}
				try
				{
					harmony.PatchAll();
					Log.LogInfo(debugBase + "Harmony patches applied successfully.");
				}
				catch (Exception ex)
				{
					Log.LogError(debugBase + "Harmony PatchAll FAILED: " + ex);
				}
			}
		}

		internal static void LogDebug(string msg)
		{
			if (EnableDebugging != null && EnableDebugging.Value)
			{
				Log.LogDebug(debugBase + msg);
			}
		}

		internal static void LogInfo(string msg)
		{
			Log.LogInfo(debugBase + msg);
		}

		internal static void LogError(string msg)
		{
			Log.LogError(debugBase + msg);
		}
	}

	public static class PluginInfo
	{
		public const string PLUGIN_GUID = "com.binbin.quicksave";

		public const string PLUGIN_NAME = "QuickSave";

		public const string PLUGIN_VERSION = "1.2.1.3";
	}
}
