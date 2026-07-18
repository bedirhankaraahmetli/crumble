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
