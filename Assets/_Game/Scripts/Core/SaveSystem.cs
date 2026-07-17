using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Crumble.Core
{
    /// <summary>
    /// Pure (non-MonoBehaviour) save serialization and atomic file IO, fully testable in
    /// EditMode. SaveManager wraps this with paths, autosave timing and app lifecycle.
    ///
    /// Corruption safety: writes go to a .tmp file first, the previous save becomes .bak,
    /// then the .tmp is swapped in. Reads fall back to .bak if the main file is unreadable.
    /// </summary>
    public static class SaveSystem
    {
        public static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Converters = { new BigDoubleJsonConverter() },
            // Unknown fields in newer/older saves are ignored; missing fields keep their
            // defaults — this is what lets saves survive game updates.
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
        };

        public static string ToJson(SaveData data) => JsonConvert.SerializeObject(data, JsonSettings);

        public static SaveData FromJson(string json) => JsonConvert.DeserializeObject<SaveData>(json, JsonSettings);

        public static void Write(SaveData data, string path)
        {
            var json = ToJson(data);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tmp = path + ".tmp";
            var bak = path + ".bak";

            File.WriteAllText(tmp, json);
            if (File.Exists(path))
            {
                if (File.Exists(bak))
                {
                    File.Delete(bak);
                }

                File.Move(path, bak);
            }

            File.Move(tmp, path);
        }

        /// <summary>Returns null when no readable save exists (fresh install).</summary>
        public static SaveData Read(string path)
        {
            return TryRead(path) ?? TryRead(path + ".bak");
        }

        private static SaveData TryRead(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                var data = FromJson(File.ReadAllText(path));
                if (data != null)
                {
                    Migrate(data);
                }

                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Failed to read '{path}': {e.Message}");
                return null;
            }
        }

        /// <summary>Version migration hook: bring older saves up to CurrentVersion, step by step.</summary>
        private static void Migrate(SaveData data)
        {
            // switch (data.Version) { case 1: ...; goto case 2; ... } as versions accrue.
            data.Version = SaveData.CurrentVersion;
        }
    }
}
