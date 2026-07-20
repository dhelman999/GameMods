using BepInEx.Bootstrap;

// Global namespace (matches the original assembly layout).
//
// NOTE: this workspace build intentionally has NO compile-time dependency on Obeliskial
// Essentials, because the current game build (2026-07-09) makes Essentials crash on load.
// `Enabled` only inspects the BepInEx chainloader (no Essentials types), and
// `EssentialsRegister` is a no-op log — it is only ever called when Essentials IS present,
// which is not our supported configuration.
public static class EssentialsCompatibility
{
	private static bool? _enabled;

	public static bool Enabled
	{
		get
		{
			if (!_enabled.HasValue)
			{
				_enabled = Chainloader.PluginInfos.ContainsKey("com.stiffmeds.obeliskialessentials");
			}
			return _enabled.Value;
		}
	}

	public static void EssentialsRegister()
	{
		QuickSave.Plugin.LogInfo(QuickSave.Plugin.PluginGUID + " " + QuickSave.Plugin.PluginVersion + " detected Essentials (running in framework-less mode).");
	}
}
