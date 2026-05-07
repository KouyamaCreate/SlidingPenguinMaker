using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace StageMaker
{
    /// <summary>
    /// CustomStageData を JSON ファイルとして persistentDataPath に保存・読み込みする。
    /// </summary>
    public static class CustomStageRepository
    {
        public const string FolderName = "CustomStages";
        public const string FileExtension = ".json";

        public static string GetFolderPath()
        {
            string path = Path.Combine(Application.persistentDataPath, FolderName);
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }
            return path;
        }

        public static string GetFilePath(string stageId)
        {
            return Path.Combine(GetFolderPath(), stageId + FileExtension);
        }

        public static List<CustomStageData> LoadAll()
        {
            var result = new List<CustomStageData>();
            string folder = GetFolderPath();
            if (!Directory.Exists(folder)) { return result; }

            foreach (var file in Directory.GetFiles(folder, "*" + FileExtension))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var data = CustomStageData.FromJson(json);
                    if (data != null && !string.IsNullOrEmpty(data.id))
                    {
                        result.Add(data);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CustomStageRepository] Failed to read {file}: {ex.Message}");
                }
            }

            result.Sort((a, b) => string.Compare(b.updatedAt, a.updatedAt, StringComparison.Ordinal));
            return result;
        }

        public static CustomStageData Load(string stageId)
        {
            if (string.IsNullOrEmpty(stageId)) { return null; }
            string path = GetFilePath(stageId);
            if (!File.Exists(path)) { return null; }
            try
            {
                return CustomStageData.FromJson(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CustomStageRepository] Load failed: {ex.Message}");
                return null;
            }
        }

        public static void Save(CustomStageData data)
        {
            if (data == null || string.IsNullOrEmpty(data.id))
            {
                Debug.LogWarning("[CustomStageRepository] Cannot save data without id");
                return;
            }

            data.updatedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            string json = data.ToJson(prettyPrint: true);
            string path = GetFilePath(data.id);
            File.WriteAllText(path, json);
            Debug.Log($"[CustomStageRepository] Saved: {path}");
        }

        public static void Delete(string stageId)
        {
            if (string.IsNullOrEmpty(stageId)) { return; }
            string path = GetFilePath(stageId);
            if (File.Exists(path)) { File.Delete(path); }
        }
    }
}
