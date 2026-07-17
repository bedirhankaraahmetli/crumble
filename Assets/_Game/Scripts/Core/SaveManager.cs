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
