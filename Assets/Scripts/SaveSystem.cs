using UnityEngine;
using System.IO;

/// <summary>
/// Lightweight save system using JSON + PlayerPrefs fallback.
/// Stores player progress, settings, and level completion.
/// </summary>
public static class SaveSystem
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    [System.Serializable]
    public class SaveData
    {
        public int currentLevel     = 0;
        public int highestLevel     = 0;
        public float musicVolume    = 0.7f;
        public float sfxVolume      = 1.0f;
        public bool soundEnabled    = true;
    }

    private static SaveData _cache;

    public static SaveData Load()
    {
        if (_cache != null) return _cache;

        if (File.Exists(SavePath))
        {
            try
            {
                string json = File.ReadAllText(SavePath);
                _cache = JsonUtility.FromJson<SaveData>(json);
                return _cache;
            }
            catch
            {
                Debug.LogWarning("[SaveSystem] Corrupt save file, resetting.");
            }
        }

        _cache = new SaveData();
        return _cache;
    }

    public static void Save(SaveData data)
    {
        _cache = data;
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to save: {e.Message}");
        }
    }

    public static void DeleteAll()
    {
        _cache = null;
        if (File.Exists(SavePath))
            File.Delete(SavePath);
    }
}
