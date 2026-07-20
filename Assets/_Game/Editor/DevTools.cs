using System.IO;
using Crumble.Core;
using UnityEditor;
using UnityEngine;

namespace Crumble.EditorTools
{
    /// <summary>
    /// Development-only helpers (editor assembly — never ships in a build).
    /// </summary>
    public static class DevTools
    {
        /// <summary>
        /// Wipes the save. In play mode the game resets live (fresh tablet, zero coins);
        /// outside play mode the save files are simply deleted from disk.
        /// </summary>
        /// <summary>Play-mode only: grants 100 KP for testing the Research Tree.</summary>
        [MenuItem("Crumble/Dev/Grant 100 KP")]
        public static void GrantKnowledge()
        {
            if (!Application.isPlaying || Crumble.Gameplay.CurrencyManager.Instance == null)
            {
                Debug.LogWarning("[DevTools] Grant KP only works in Play mode.");
                return;
            }

            Crumble.Gameplay.CurrencyManager.Instance.AddKnowledge(100);
            Debug.Log("[DevTools] Granted 100 KP.");
        }

        [MenuItem("Crumble/Dev/Reset Save")]
        public static void ResetSave()
        {
            if (Application.isPlaying && SaveManager.Instance != null)
            {
                SaveManager.Instance.ResetSave();
                Debug.Log("[DevTools] Save reset live — fresh run started.");
            }
            else
            {
                var path = Path.Combine(Application.persistentDataPath, "save.json");
                SaveSystem.Delete(path);
                Debug.Log($"[DevTools] Save files deleted: {path}");
            }
        }
    }
}
