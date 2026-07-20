using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using UnityEngine;

namespace QuickSave
{
	[HarmonyPatch]
	public class QuickSaveFunctions
	{
		public enum LoadType
		{
			Turn,
			Game
		}

		public static string quickLoadFilename;

		public static string quickLoadFilenameGameForTurn;

		public static LoadType quickLoadType;

		// NOTE: original initialized these in static field initializers by dereferencing
		// GameManager.Instance / NetworkManager.Instance. That risks a TypeInitializationException
		// if the class is touched before those singletons exist. They are always re-computed at
		// the point of use (ShowPostfix / OnMouseUp), so we default them safely here.
		public static bool isHost = false;

		public static bool isMP = false;

		public static Transform CreateIcon(Transform original, string name = "", List<GameObject> buttonList = null)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(original.gameObject, Vector3.zero, Quaternion.identity, null);
			if (gameObject == null)
			{
				Plugin.LogDebug("Failed to instantiate game object clone");
				return null;
			}
			Transform transform = gameObject.transform;
			if (transform == null)
			{
				Plugin.LogDebug("Cloned object has no transform component");
				return null;
			}
			UnityEngine.Object.DontDestroyOnLoad(transform);
			transform.gameObject.SetActive(value: false);
			transform.gameObject.name = name;
			buttonList?.Add(transform.gameObject);
			return transform;
		}

		public static List<string> GetAllSaves(string rootDirectory)
		{
			string[] files = Directory.GetFiles(rootDirectory, "*", SearchOption.AllDirectories);
			List<string> list = new List<string>();
			foreach (string text in files)
			{
				string item = text.Substring(rootDirectory.Length + 1);
				list.Add(item);
			}
			return list;
		}

		public static IEnumerator SaveGameTurn(bool doCustomName = false)
		{
			int saveSlot = SaveManager.Instance.GetSaveSlot();
			Plugin.LogDebug($"Saving turn to slot {saveSlot}");
			string savePath = SaveManager.Instance.PathSaveGameTurn(saveSlot);
			string directory = Path.GetDirectoryName(savePath);
			string seed = AtOManager.Instance?.GetGameId() ?? "nullseed";
			string node = AtOManager.Instance?.currentMapNode ?? "sen_0";
			string folderName = ((Plugin.SaveFolderName.Value == "") ? seed : Plugin.SaveFolderName.Value);
			string outputDirectory = Path.Combine(directory, folderName, node);
			string originalPath = savePath;
			string originalGameData = Path.Combine(directory, $"gamedata_{saveSlot}.ato");
			string charActive = MatchManager.Instance?.GetCharacterActive()?.SourceName ?? "nullchar";
			int turnNum = MatchManager.Instance?.GetCurrentRound() ?? 0;
			Plugin.LogDebug("Original Path: " + originalPath);
			string newFileName = $"gamedata_{saveSlot}_{node}_turn{turnNum}_{charActive}.ato";
			Plugin.LogDebug("Output Dir: " + outputDirectory);
			if (!Directory.Exists(outputDirectory))
			{
				Directory.CreateDirectory(outputDirectory);
			}
			string newPath = Path.Combine(outputDirectory, newFileName);
			string nextFilePath = ((!doCustomName) ? GetUniqueFilename(newPath) : Path.Combine(outputDirectory, QuickSavePatches.customName));
			string nameWithoutExtension = Path.GetFileNameWithoutExtension(nextFilePath);
			string extension = Path.GetExtension(nextFilePath);
			string newGamePathName = nameWithoutExtension + "_gamedata" + extension;
			Plugin.LogDebug($"nextFilePath with doCustomName = {doCustomName}: {nextFilePath}");
			Plugin.LogDebug($"newGamePathName with doCustomName = {doCustomName}: {newGamePathName}");
			File.Copy(originalPath, nextFilePath, overwrite: true);
			File.Copy(originalGameData, newGamePathName, overwrite: true);
			quickLoadFilename = nextFilePath;
			quickLoadFilenameGameForTurn = newGamePathName;
			quickLoadType = LoadType.Turn;
			QuickSavePatches.changeSaveGameText = true;
			yield return Globals.Instance.WaitForSeconds(1f);
			QuickSavePatches.changeSaveGameText = false;
			yield return Globals.Instance.WaitForSeconds(1f);
		}

		public static IEnumerator SaveGame(bool doCustomName = false)
		{
			int saveSlot = SaveManager.Instance.GetSaveSlot();
			string savePath = SaveManager.Instance.PathSaveGameTurn(saveSlot);
			string directory = Path.GetDirectoryName(savePath);
			string seed = AtOManager.Instance?.GetGameId() ?? "nullseed";
			string node = AtOManager.Instance?.currentMapNode ?? "sen_0";
			string folderName = ((Plugin.SaveFolderName.Value == "") ? seed : Plugin.SaveFolderName.Value);
			string outputDirectory = Path.Combine(directory, folderName, node);
			string originalPath = Path.Combine(directory, $"gamedata_{saveSlot}.ato");
			Plugin.LogDebug("Original Path: " + originalPath);
			string newFileName = $"gamedata_{saveSlot}_{node}.ato";
			Plugin.LogDebug("Output Dir: " + outputDirectory);
			if (!Directory.Exists(outputDirectory))
			{
				Directory.CreateDirectory(outputDirectory);
			}
			string newPath = Path.Combine(outputDirectory, newFileName);
			string nextFilePath = ((!doCustomName) ? GetUniqueFilename(newPath) : Path.Combine(outputDirectory, QuickSavePatches.customName));
			File.Copy(originalPath, nextFilePath, overwrite: true);
			quickLoadFilename = nextFilePath;
			quickLoadType = LoadType.Game;
			QuickSavePatches.changeSaveGameText = true;
			yield return Globals.Instance.WaitForSeconds(5f);
			QuickSavePatches.changeSaveGameText = false;
			yield return Globals.Instance.WaitForSeconds(1f);
		}

		public static void QuickLoad()
		{
			if (string.IsNullOrEmpty(quickLoadFilename))
			{
				Plugin.LogDebug("QuickLoad - no quick-save yet this session (save once first).");
				return;
			}
			if (!File.Exists(quickLoadFilename))
			{
				Plugin.LogDebug("QuickLoad - file missing: " + quickLoadFilename);
				return;
			}
			Plugin.LogDebug("attempting to load" + quickLoadFilename);
			LoadGameFromFilename(quickLoadType, quickLoadFilename, quickLoadFilenameGameForTurn);
		}

		public static void LoadGameFromFilename(LoadType loadType, string filePath, string filePathToGameIfTurn = "")
		{
			if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
			{
				Plugin.LogDebug("LoadGameFromFilename - missing file: " + (filePath ?? "(null)"));
				return;
			}
			int saveSlot = SaveManager.Instance.GetSaveSlot();
			string directoryName = Path.GetDirectoryName(SaveManager.Instance.PathSaveGameTurn(saveSlot));
			switch (loadType)
			{
			case LoadType.Turn:
			{
				string destFileName = Path.Combine(directoryName, $"gamedata_{saveSlot}.ato");
				string path = Path.Combine(directoryName, $"gamedata_{saveSlot}_turn.ato");
				if (!string.IsNullOrEmpty(filePathToGameIfTurn) && File.Exists(filePathToGameIfTurn))
				{
					File.Copy(filePathToGameIfTurn, destFileName, overwrite: true);
				}
				File.Copy(filePath, path, overwrite: true);
				BinbinLoadGame(saveSlot);
				break;
			}
			case LoadType.Game:
			{
				string destFileName = Path.Combine(directoryName, $"gamedata_{saveSlot}.ato");
				string path = Path.Combine(directoryName, $"gamedata_{saveSlot}_turn.ato");
				if (File.Exists(path))
				{
					File.Delete(path);
				}
				File.Copy(filePath, destFileName, overwrite: true);
				BinbinLoadGame(saveSlot);
				break;
			}
			default:
				BinbinLoadGame(saveSlot);
				break;
			}
		}

		public static void SetButtons(List<GameObject> gameObjects, bool active)
		{
			foreach (GameObject gameObject in gameObjects)
			{
				gameObject.SetActive(active);
			}
		}

		public static string GetUniqueFilename(string originalFilename)
		{
			string directoryName = Path.GetDirectoryName(originalFilename);
			string text = Path.GetFileName(originalFilename);
			int num = 2;
			while (File.Exists(Path.Combine(directoryName, text)))
			{
				Match match = Regex.Match(text, "^(gamedata_\\d+_[^_]+_)(\\d+)(\\.ato)$");
				if (match.Success)
				{
					string value = match.Groups[1].Value;
					int num2 = int.Parse(match.Groups[2].Value);
					string value2 = match.Groups[3].Value;
					text = $"{value}{num2}_save{num}{value2}";
				}
				else
				{
					Match match2 = Regex.Match(text, "(.+?)(\\d+)(\\.ato)$");
					if (match2.Success)
					{
						string value3 = match2.Groups[1].Value;
						int num3 = int.Parse(match2.Groups[2].Value);
						string value4 = match2.Groups[3].Value;
						num3++;
						text = $"{value3}{num3}{value4}";
					}
					else
					{
						string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text);
						string extension = Path.GetExtension(text);
						text = $"{fileNameWithoutExtension}_save{num}{extension}";
					}
				}
				num++;
			}
			return Path.Combine(directoryName, text);
		}

		public static string StripGamedataPrefix(string filename)
		{
			if (filename == null)
			{
				return filename;
			}
			return Regex.Replace(filename, "^gamedata_\\d+_", "");
		}

		public static List<string> GetAllFilePaths(string directory)
		{
			if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
			{
				Plugin.LogDebug("GetAllFilePaths - directory missing: " + (directory ?? "(null)"));
				return new List<string>();
			}
			string text = ".ato";
			return (from f in Directory.GetFiles(directory, "*" + text, SearchOption.AllDirectories)
				orderby File.GetLastWriteTime(f) descending
				select Path.GetRelativePath(directory, f)).ToList();
		}

		public static void SetCustomName(bool isTurn = false)
		{
			if (isTurn)
			{
				AlertManager.buttonClickDelegate = SetFilenameTurn;
				AlertManager.Instance.AlertInput("Input Name of Save", Texts.Instance.GetText("accept").ToUpper());
			}
			else
			{
				AlertManager.buttonClickDelegate = SetFilenameGame;
				AlertManager.Instance.AlertInput("Input Name of Save", Texts.Instance.GetText("accept").ToUpper());
			}
		}

		public static void SetFilenameGame()
		{
			AlertManager.buttonClickDelegate = (AlertManager.OnButtonClickDelegate)Delegate.Remove(AlertManager.buttonClickDelegate, new AlertManager.OnButtonClickDelegate(SetFilenameGame));
			if (AlertManager.Instance.GetInputValue() != null)
			{
				string text = AlertManager.Instance.GetInputValue().ToLower();
				if (text.Trim() == "")
				{
					Globals.Instance.StartCoroutine(SaveGame());
					return;
				}
				QuickSavePatches.customName = text + ".ato";
				Globals.Instance.StartCoroutine(SaveGame(doCustomName: true));
			}
		}

		public static void SetFilenameTurn()
		{
			AlertManager.buttonClickDelegate = (AlertManager.OnButtonClickDelegate)Delegate.Remove(AlertManager.buttonClickDelegate, new AlertManager.OnButtonClickDelegate(SetFilenameGame));
			if (AlertManager.Instance.GetInputValue() != null)
			{
				string text = AlertManager.Instance.GetInputValue().ToLower();
				if (text.Trim() == "")
				{
					Globals.Instance.StartCoroutine(SaveGameTurn());
					return;
				}
				QuickSavePatches.customName = text + ".ato";
				Globals.Instance.StartCoroutine(SaveGameTurn(doCustomName: true));
			}
		}

		public static void LoadMP()
		{
		}

		public static void BinbinLoadGame(int saveSlot, bool isTurn = false)
		{
			isMP = GameManager.Instance.IsMultiplayer();
			isHost = GameManager.Instance.IsMultiplayer() && NetworkManager.Instance.IsMaster();
			Plugin.LogDebug($"BinbinLoadGame - saveSlot {saveSlot}, isTurn {isTurn}, multiplayer {isHost && isMP}");
			if (!isMP)
			{
				SaveManager.Instance.LoadGame(saveSlot, false);
			}
			else if (isHost)
			{
				if (isTurn)
				{
					GameManager.Instance.GameStatus = Enums.GameStatus.LoadGame;
					AtOManager.Instance.DoLoadGameFromMP();
				}
				else
				{
					GameManager.Instance.GameStatus = Enums.GameStatus.LoadGame;
					SaveManager.Instance.LoadGame(saveSlot, false);
					AtOManager.Instance.DoLoadGameFromMP();
				}
			}
		}

		// Reset Seed (reimplemented for the 2026-07-09 build).
		//
		// The old mod had four HandleResetSeed variants; only the "save + reload with a new
		// gameId" one (formerly HandleResetSeed4) is needed for the "reroll the starting path"
		// workflow, and its APIs all still exist after the July update (SaveGame/LoadGame/
		// GetSaveSlot moved AtOManager -> SaveManager; SetGameId/GetGameId stayed on AtOManager).
		// The team-rebuilding variant is intentionally NOT restored: it relied on AtOManager
		// methods the update removed (GetTeam/SetTeamFromArray/SetPlayerGold/Dust/Perks) and is
		// unnecessary for rerolling before the first battle.
		//
		// Map node contents (combats/events/rewards) are derived deterministically from the
		// gameId, so rolling a new seed and reloading regenerates the not-yet-resolved path.
		// This only makes sense before anything is locked in, hence ResetSeedAllowed().

		public static bool ResetSeedAllowed()
		{
			try
			{
				return Plugin.EnableResetSeed != null && Plugin.EnableResetSeed.Value
					&& AtOManager.Instance != null
					&& AtOManager.Instance.monstersKilled == 0
					&& AtOManager.Instance.bossesKilled == 0;
			}
			catch (Exception ex)
			{
				Plugin.LogError("ResetSeedAllowed check failed: " + ex);
				return false;
			}
		}

		public static void HandleResetSeed()
		{
			if (AtOManager.Instance == null || SaveManager.Instance == null)
			{
				Plugin.LogDebug("HandleResetSeed - null manager, aborting.");
				return;
			}
			if (!ResetSeedAllowed())
			{
				Plugin.LogDebug("HandleResetSeed - not allowed (run already in progress), ignoring.");
				return;
			}
			string oldSeed = AtOManager.Instance.GetGameId();
			AtOManager.Instance.SetGameId();
			string newSeed = AtOManager.Instance.GetGameId();
			Plugin.LogDebug("HandleResetSeed - reseeded '" + oldSeed + "' -> '" + newSeed + "'");
			int slot = SaveManager.Instance.GetSaveSlot();
			SaveManager.Instance.SaveGame(-1, false);
			SaveManager.Instance.LoadGame(slot, false);
		}

		public static void HSMStartLocal(HeroSelectionManager __instance)
		{
			MethodInfo method = __instance.GetType().GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
			object[] parameters = new object[0];
			method.Invoke(__instance, parameters);
		}
	}
}
