using System;
using System.IO;
using UnityEngine;

namespace Crumble.Core
{
    /// <summary>
    /// Owns the live SaveData instance and persists it: 30s autosave, plus saves on app
    /// pause (mobile home button) and quit. Boot order is driven by GameManager.
    /// </summary>
    public sealed class SaveManager : Singleton<SaveManager>
    {
        private const float AutoSaveIntervalSeconds = 30f;

        public SaveData Data { get; private set; }

        public string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

        private float _autoSaveTimer;

        public void LoadOrCreate()
        {
            Data = SaveSystem.Read(SavePath) ?? new SaveData();
            GameEvents.RaiseGameLoaded(Data);
        }

        public void Save()
        {
            if (Data == null)
            {
                return;
            }

            Data.LastLoginUnixUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            SaveSystem.Write(Data, SavePath);
            GameEvents.RaiseGameSaved();
        }

        /// <summary>
        /// Wipes the save on disk and restarts from a fresh SaveData. All managers rebind
        /// via GameLoaded, so the game resets live without leaving play mode.
        /// Development tool today; the Hard Prestige flow gets its own dedicated logic.
        /// </summary>
        public void ResetSave()
        {
            SaveSystem.Delete(SavePath);
            Data = new SaveData();
            _autoSaveTimer = 0f;
            GameEvents.RaiseGameLoaded(Data);
        }

        private void Update()
        {
            if (Data == null)
            {
                return;
            }

            _autoSaveTimer += Time.unscaledDeltaTime;
            if (_autoSaveTimer >= AutoSaveIntervalSeconds)
            {
                _autoSaveTimer = 0f;
                Save();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Save();
            }
        }

        private void OnApplicationQuit()
        {
            Save();
        }
    }
}
