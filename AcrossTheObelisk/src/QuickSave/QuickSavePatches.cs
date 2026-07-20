using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace QuickSave
{
	[HarmonyPatch]
	public class QuickSavePatches
	{
		public static List<GameObject> myButtonsMap;

		public static List<GameObject> myButtonsCombat;

		public static List<GameObject> test;

		public static Transform iconReloadTurn;

		public static Transform iconSkipCorruptor;

		public static Transform iconResetSeed;

		public static Transform iconSaveGame;

		public static Transform iconSaveTurn;

		public static Transform iconLoadGame;

		public static Transform iconLoadGame2;

		public static Transform iconQuickLoad;

		public static Transform iconQuickLoad2;

		public static Transform iconShowMap;

		public static TMP_Dropdown loadgameDropdown;

		public static bool skipCinematic = false;

		public static bool skipCorruption = false;

		public static bool changeSaveGameText = false;

		public static List<string> loadFiles;

		public static string customName = "";

		// Names of the buttons THIS mod creates. Used so we never touch vanilla button flow.
		private static readonly HashSet<string> ourButtons = new HashSet<string>
		{
			"reloadturn", "resetseed", "savegame", "saveturn", "loadgame", "quickload", "skipcorruptor"
		};

		[HarmonyPostfix]
		[HarmonyPatch(typeof(OptionsManager), "Awake")]
		public static void AwakePostfix(OptionsManager __instance, List<GameObject> ___buttonOrder)
		{
			Plugin.LogDebug("AwakePostfix");
			myButtonsMap = new List<GameObject>();
			myButtonsCombat = new List<GameObject>();
			iconQuickLoad = QuickSaveFunctions.CreateIcon(__instance.iconSettings, "quickload", myButtonsMap);
			iconLoadGame = QuickSaveFunctions.CreateIcon(__instance.iconStats, "loadgame", myButtonsMap);
			iconSaveGame = QuickSaveFunctions.CreateIcon(__instance.iconTome, "savegame", myButtonsMap);
			iconResetSeed = QuickSaveFunctions.CreateIcon(__instance.iconRetry, "resetseed", myButtonsMap);
			iconReloadTurn = QuickSaveFunctions.CreateIcon(__instance.iconRetry, "reloadturn", myButtonsCombat);
			iconSaveTurn = QuickSaveFunctions.CreateIcon(__instance.iconTome, "saveturn", myButtonsCombat);
			iconLoadGame2 = QuickSaveFunctions.CreateIcon(__instance.iconStats, "loadgame", myButtonsCombat);
			iconQuickLoad2 = QuickSaveFunctions.CreateIcon(__instance.iconSettings, "quickload", myButtonsCombat);
			iconSkipCorruptor = QuickSaveFunctions.CreateIcon(__instance.iconResign, "skipcorruptor", myButtonsCombat);
			myButtonsCombat.Reverse();
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SettingsManager), "Awake")]
		public static void SettingsManagerAwakePostfix(SettingsManager __instance)
		{
			TMP_Dropdown languageDropdown = SettingsManager.Instance.languageDropdown;
			GameObject gameObject = UnityEngine.Object.Instantiate(languageDropdown.gameObject, Vector3.zero, Quaternion.identity, __instance.canvas.transform);
			loadgameDropdown = gameObject.GetComponent<TMP_Dropdown>();
			loadgameDropdown.gameObject.SetActive(value: false);
			UnityEngine.Object.DontDestroyOnLoad(loadgameDropdown.transform);
			loadgameDropdown.name = "loadgamedropdown";
			loadgameDropdown.onValueChanged.SetPersistentListenerState(0, UnityEventCallState.Off);
			RectTransform component = loadgameDropdown.GetComponent<RectTransform>();
			component.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 650f);
			loadgameDropdown.ClearOptions();
			loadgameDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
			loadgameDropdown.transform.position = __instance.transform.position;
			loadgameDropdown.transform.localPosition = new Vector3(__instance.transform.localPosition.x - 200f, __instance.transform.localPosition.y - 200f, __instance.transform.localPosition.z);
			Plugin.LogDebug("Created DropDownTest");
			Plugin.LogDebug(string.Format("Created DropDownTest: {0} with index options {1}", loadgameDropdown.gameObject.activeSelf, string.Join(", ", loadgameDropdown.options)));
		}

		public static void OnDropdownValueChanged(int index)
		{
			Plugin.LogDebug($"OnSubmit Test option selected {index}: {loadgameDropdown.options[index].text}");
			if (AtOManager.Instance == null)
			{
				Plugin.LogDebug("OnDropdownValueChanged - Null AtOManger");
				return;
			}
			string directoryName = Path.GetDirectoryName(SaveManager.Instance.PathSaveGameTurn(SaveManager.Instance.GetSaveSlot()));
			string gameId = AtOManager.Instance.GetGameId();
			string path = ((Plugin.SaveFolderName.Value == "") ? gameId : Plugin.SaveFolderName.Value);
			string text = Path.Combine(directoryName, path, loadgameDropdown.options[index].text);
			long length = new FileInfo(text).Length;
			Plugin.LogDebug($"Current filesize in bytes: {length}");
			Plugin.LogDebug("Current filename: " + text);
			QuickSaveFunctions.LoadType loadType = ((length > 10000) ? QuickSaveFunctions.LoadType.Game : QuickSaveFunctions.LoadType.Turn);
			string filePathToGameIfTurn = "";
			if (loadType == QuickSaveFunctions.LoadType.Turn)
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text);
				string extension = Path.GetExtension(text);
				filePathToGameIfTurn = fileNameWithoutExtension + "_gamedata" + extension;
			}
			QuickSaveFunctions.LoadGameFromFilename(loadType, text, filePathToGameIfTurn);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(OptionsManager), "Show")]
		public static void ShowPostfix(OptionsManager __instance, List<GameObject> ___buttonOrder)
		{
			QuickSaveFunctions.isMP = GameManager.Instance.IsMultiplayer();
			QuickSaveFunctions.isHost = GameManager.Instance.IsMultiplayer() && NetworkManager.Instance.IsMaster();
			Plugin.LogDebug($"ShowPostfix - IsHost {QuickSaveFunctions.isHost}, IsMP {QuickSaveFunctions.isMP}");
			QuickSaveFunctions.SetButtons(myButtonsCombat, active: false);
			QuickSaveFunctions.SetButtons(myButtonsMap, active: false);
			if (((bool)MatchManager.Instance || (bool)RewardsManager.Instance) && (QuickSaveFunctions.isHost || !QuickSaveFunctions.isMP))
			{
				QuickSaveFunctions.SetButtons(myButtonsCombat, active: true);
				if (!AtOManager.Instance.corruptionAccepted)
				{
					iconSkipCorruptor.gameObject.SetActive(value: false);
				}
			}
			else if (((bool)TownManager.Instance || AtOManager.Instance.CharInTown()) && (QuickSaveFunctions.isHost || !QuickSaveFunctions.isMP))
			{
				QuickSaveFunctions.SetButtons(myButtonsMap, active: true);
			}
			else if ((bool)MapManager.Instance && (QuickSaveFunctions.isHost || !QuickSaveFunctions.isMP))
			{
				QuickSaveFunctions.SetButtons(myButtonsMap, active: true);
			}
			// Reset Seed is only meaningful before the run really begins (starting town,
			// no combat resolved yet). Reseeding after the first battle would require
			// restarting the whole game, so we hide the button once progress exists.
			if (iconResetSeed != null)
			{
				if (!QuickSaveFunctions.ResetSeedAllowed())
				{
					iconResetSeed.gameObject.SetActive(value: false);
				}
			}
			float num = 0.95f;
			float num2 = 0.65f;
			for (int i = 0; i < myButtonsMap.Count; i++)
			{
				if (myButtonsMap[i].activeSelf)
				{
					myButtonsMap[i].transform.position = new Vector3(__instance.iconTome.transform.position.x - num, __instance.iconTome.transform.position.y, __instance.iconTome.transform.position.z);
					myButtonsMap[i].transform.localPosition = new Vector3(num + num2 * 2f, myButtonsMap[i].transform.localPosition.y, myButtonsMap[i].transform.localPosition.z);
					num -= num2;
				}
			}
			num = 0.95f;
			for (int j = 0; j < myButtonsCombat.Count; j++)
			{
				if (myButtonsCombat[j].activeSelf)
				{
					myButtonsCombat[j].transform.position = new Vector3(__instance.iconTome.transform.position.x - num, __instance.iconTome.transform.position.y, __instance.iconTome.transform.position.z);
					myButtonsCombat[j].transform.localPosition = new Vector3(num - num2 * 5.7f + Plugin.MidCombatHorizontalShift.Value * 0.01f, myButtonsCombat[j].transform.localPosition.y, myButtonsCombat[j].transform.localPosition.z);
					num -= num2;
				}
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(BotonRollover), "ShowText")]
		public static void BotonRolloverShowText(BotonRollover __instance)
		{
			if (__instance.gameObject.name == "reloadturn")
			{
				__instance.rollOverText.GetComponent<TMP_Text>().text = "Reload Turn";
			}
			if (__instance.gameObject.name == "savegame")
			{
				__instance.rollOverText.GetComponent<TMP_Text>().text = (changeSaveGameText ? "Game Saved!" : "Save Game");
			}
			if (__instance.gameObject.name == "loadgame")
			{
				__instance.rollOverText.GetComponent<TMP_Text>().text = "Load Game";
			}
			if (__instance.gameObject.name == "resetseed")
			{
				__instance.rollOverText.GetComponent<TMP_Text>().text = "Reset Seed";
			}
			if (__instance.gameObject.name == "saveturn")
			{
				__instance.rollOverText.GetComponent<TMP_Text>().text = (changeSaveGameText ? "Game Saved!" : "Save Turn");
			}
			if (__instance.gameObject.name == "skipcorruptor")
			{
				__instance.rollOverText.GetComponent<TMP_Text>().text = "Restart Combat without Corruptor";
			}
			if (__instance.gameObject.name == "quickload")
			{
				string text = "Quick Load \n(" + QuickSaveFunctions.StripGamedataPrefix(Path.GetFileNameWithoutExtension(QuickSaveFunctions.quickLoadFilename)) + ")";
				string value = "\n";
				int num = text.IndexOf('_');
				int num2 = -1;
				if (num != -1)
				{
					num2 = text.IndexOf('_', num + 1);
				}
				if (num2 != -1)
				{
					StringBuilder stringBuilder = new StringBuilder(text);
					stringBuilder.Remove(num2, 1);
					stringBuilder.Insert(num2, value);
					text = stringBuilder.ToString();
				}
				__instance.rollOverText.GetComponent<TMP_Text>().text = text;
			}
			if (__instance.gameObject.name == "showmap")
			{
				__instance.rollOverText.GetComponent<TMP_Text>().text = "Show Map (Coming Soon)";
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(BotonRollover), "OnMouseUp")]
		public static void BotonRolloverOnMouseUp(BotonRollover __instance)
		{
			// HARDENED + INSTRUMENTED (workspace build):
			//  * Entry log BEFORE any dereference -> tells us if this handler is even reached
			//    (distinguishes an input/raycast blocker from a swallowed exception).
			//  * Whole body wrapped in try/catch so a mod error can NEVER prevent the game's own
			//    OnMouseUp from running (this patches the SHARED base class used by every button).
			//  * Every game singleton is null-guarded (the stock mod dereferenced several
			//    unguarded, which throws on the current build and kills all button input).
			//  * We only act on OUR buttons and return immediately for vanilla ones.
			string btnName = null;
			try
			{
				btnName = __instance != null && __instance.gameObject != null ? __instance.gameObject.name : null;
			}
			catch
			{
			}
			Plugin.LogDebug("OnMouseUp ENTER: " + (btnName ?? "null"));
			try
			{
				QuickSaveFunctions.isMP = GameManager.Instance != null && GameManager.Instance.IsMultiplayer();
				QuickSaveFunctions.isHost = QuickSaveFunctions.isMP && NetworkManager.Instance != null && NetworkManager.Instance.IsMaster();

				// Only proceed for the buttons this mod adds; leave vanilla buttons untouched.
				if (btnName == null || !ourButtons.Contains(btnName))
				{
					return;
				}

				bool clickedThis = Functions.ClickedThisTransform(__instance.transform);
				bool combatLoading = (bool)MatchManager.Instance && MatchManager.Instance.CombatLoading;
				bool alertActive = AlertManager.Instance != null && AlertManager.Instance.IsActive();
				bool tutorialActive = GameManager.Instance != null && GameManager.Instance.IsTutorialActive();
				bool settingsActive = SettingsManager.Instance != null && SettingsManager.Instance.IsActive();
				bool damageMeterActive = DamageMeterManager.Instance != null && DamageMeterManager.Instance.IsActive();
				bool charUnlock = (bool)MapManager.Instance && MapManager.Instance.IsCharacterUnlock();
				bool consoleActive = (bool)MatchManager.Instance && MatchManager.Instance.console != null && MatchManager.Instance.console.IsActive();

				if (!clickedThis || combatLoading || alertActive || tutorialActive || settingsActive || damageMeterActive || charUnlock || consoleActive)
				{
					return;
				}

				string name = __instance.gameObject.name;
				CloseWindows(__instance, name);
				int saveSlot = SaveManager.Instance.GetSaveSlot();
				Plugin.LogDebug("BotonRolloverOnMouseUp " + (name ?? "null object"));
				switch (name)
				{
				case "reloadturn":
					QuickSaveFunctions.isHost = GameManager.Instance.IsMultiplayer() && NetworkManager.Instance.IsMaster();
					if (QuickSaveFunctions.isHost)
					{
						AtOManager.Instance.DoLoadGameFromMP();
					}
					else
					{
						SaveManager.Instance.LoadGame(saveSlot, false);
					}
					break;
				case "savegame":
					if (Plugin.EnableCustomSaveNames.Value)
					{
						QuickSaveFunctions.SetCustomName();
					}
					else
					{
						Globals.Instance.StartCoroutine(QuickSaveFunctions.SaveGame());
					}
					break;
				case "saveturn":
					if (Plugin.EnableCustomSaveNames.Value)
					{
						QuickSaveFunctions.SetCustomName(isTurn: true);
					}
					else
					{
						Globals.Instance.StartCoroutine(QuickSaveFunctions.SaveGameTurn());
					}
					break;
				case "loadgame":
				{
					string directoryName = Path.GetDirectoryName(SaveManager.Instance.PathSaveGameTurn(saveSlot));
					string gameId = AtOManager.Instance.GetGameId();
					string path = ((Plugin.SaveFolderName.Value == "") ? gameId : Plugin.SaveFolderName.Value);
					string directory = Path.Combine(directoryName, path);
					List<string> allFilePaths = QuickSaveFunctions.GetAllFilePaths(directory);
					Plugin.LogDebug("initialized dropdown");
					loadgameDropdown.ClearOptions();
					loadgameDropdown.AddOptions(allFilePaths);
					loadgameDropdown.gameObject.SetActive(value: true);
					SettingsManager.Instance.ShowSettings(_state: true);
					break;
				}
				case "quickload":
					QuickSaveFunctions.QuickLoad();
					break;
				case "resetseed":
					QuickSaveFunctions.HandleResetSeed();
					break;
				case "skipcorruptor":
					skipCorruption = true;
					AtOManager.Instance.corruptionAccepted = false;
					MatchManager.Instance.ALL_BreakByDesync();
					break;
				}
				fRollOut(__instance);
			}
			catch (Exception ex)
			{
				Plugin.LogError("Exception in BotonRolloverOnMouseUp for '" + (btnName ?? "null") + "': " + ex);
			}
		}

		[HarmonyReversePatch(HarmonyReversePatchType.Original)]
		[HarmonyPatch(typeof(BotonRollover), "fRollOut")]
		public static void fRollOut(BotonRollover __instance)
		{
		}

		[HarmonyReversePatch(HarmonyReversePatchType.Original)]
		[HarmonyPatch(typeof(BotonRollover), "CloseWindows")]
		public static void CloseWindows(BotonRollover __instance, string botName)
		{
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(CinematicManager), "Awake")]
		public static void CinematicManagerAwakePostfix(CinematicManager __instance)
		{
			if (skipCinematic)
			{
				Plugin.LogDebug("CinematicManagerAwakePostfix attempting to skip");
				skipCinematic = false;
				__instance?.SkipCinematic();
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(AtOManager), "DoLoadGame")]
		public static void DoLoadGamePrefix(AtOManager __instance, bool comingFromReloadCombat = false)
		{
			Plugin.LogDebug("DoLoadGame");
			if (skipCorruption)
			{
				Plugin.LogDebug("DoLoadGame attempting to corruption");
				AtOManager.Instance.corruptionAccepted = false;
				skipCorruption = false;
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(AtOManager), "DoLoadGameFromMP")]
		public static void DoLoadGameFromMPPrefix(AtOManager __instance, bool comingFromReloadCombat = false)
		{
			Plugin.LogDebug("DoLoadGameFromMPPrefix");
			if (skipCorruption)
			{
				Plugin.LogDebug("DoLoadGameFromMPPrefix attempting to corruption");
				AtOManager.Instance.corruptionAccepted = false;
				skipCorruption = false;
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(SaveManager), "LoadGameTurn")]
		public static void LoadGameTurnPrefix()
		{
			Plugin.LogDebug("LoadGameTurnPrefix");
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(SaveManager), "LoadGame")]
		public static void LoadGamePrefix()
		{
			Plugin.LogDebug("LoadGamePrefix");
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(InputController), "DoKeyBinding")]
		public static bool DoKeyBindingPrefix(InputAction.CallbackContext _context)
		{
			if (Keyboard.current != null && Plugin.DisableControlClick.Value && (_context.control == Keyboard.current[Key.LeftCtrl] || _context.control == Keyboard.current[Key.RightCtrl]))
			{
				Plugin.LogDebug("Disabling control click");
				return false;
			}
			return true;
		}
	}
}
