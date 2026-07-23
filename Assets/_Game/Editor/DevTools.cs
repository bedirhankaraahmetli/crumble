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

        /// <summary>Play-mode only: grants 25 Time Crystals for testing the Cosmic Altar.</summary>
        [MenuItem("Crumble/Dev/Grant 25 Time Crystals")]
        public static void GrantTimeCrystals()
        {
            if (!Application.isPlaying || Crumble.Gameplay.CurrencyManager.Instance == null)
            {
                Debug.LogWarning("[DevTools] Grant Time Crystals only works in Play mode.");
                return;
            }

            Crumble.Gameplay.CurrencyManager.Instance.AddTimeCrystals(25);
            Debug.Log("[DevTools] Granted 25 Time Crystals.");
        }

        /// <summary>
        /// Play-mode only: maxes every research node so the Cosmic Archive unlocks —
        /// the fastest route to testing the Hard Prestige flow.
        /// </summary>
        [MenuItem("Crumble/Dev/Max All Research (Unlock Cosmic)")]
        public static void MaxAllResearch()
        {
            var research = Crumble.Gameplay.ResearchManager.Instance;
            var save = SaveManager.Instance;
            if (!Application.isPlaying || research == null || save == null || save.Data == null)
            {
                Debug.LogWarning("[DevTools] Max All Research only works in Play mode.");
                return;
            }

            foreach (var node in research.Nodes)
            {
                if (node != null)
                {
                    save.Data.ResearchTree[node.Id] = node.MaxLevel;
                }
            }

            GameEvents.RaiseGameLoaded(save.Data); // rebind so aggregates and panels absorb it
            Debug.Log("[DevTools] All research maxed — the Cosmic Archive is unlocked.");
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
