// SaveSystem.cs
using System.IO;
using UnityEngine;

public static class SaveSystem
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnEnterPlayMode]
    private static void ResetCacheOnPlay(UnityEditor.EnterPlayModeOptions options)
    {
        _cache = null;
    }
#endif

    private static readonly string SavePath =
        Path.Combine(Application.persistentDataPath, "save.json");

    private static SaveData _cache;

    public static SaveData Load()
    {
        if (_cache != null) return _cache;

        if (!File.Exists(SavePath))
        {
            _cache = new SaveData();
            return _cache;
        }

        string json = File.ReadAllText(SavePath);
        _cache = JsonUtility.FromJson<SaveData>(json);
        return _cache;
    }

    public static void Save()
    {
        if (_cache == null) return;
        File.WriteAllText(SavePath, JsonUtility.ToJson(_cache, prettyPrint: true));
    }

    public static void Delete()
    {
        _cache = null;
        if (File.Exists(SavePath))
            File.Delete(SavePath);
    }
}