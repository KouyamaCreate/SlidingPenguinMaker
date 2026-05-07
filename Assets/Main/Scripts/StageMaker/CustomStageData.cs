using System;
using System.Collections.Generic;
using UnityEngine;

namespace StageMaker
{
    [Serializable]
    public class CustomStagePartPlacement
    {
        public string partId;
        public Vector3 worldPosition;
        public float rotationY;
    }

    [Serializable]
    public class CustomStageData
    {
        public const int CurrentSchemaVersion = 2;

        public int schemaVersion = CurrentSchemaVersion;
        public string id = "";
        public string displayName = "Untitled";
        public string createdAt = "";
        public string updatedAt = "";
        public List<CustomStagePartPlacement> parts = new();

        public static CustomStageData CreateNew(string name)
        {
            var now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            var data = new CustomStageData
            {
                id = Guid.NewGuid().ToString("N"),
                displayName = string.IsNullOrEmpty(name) ? "Untitled" : name,
                createdAt = now,
                updatedAt = now,
            };
            // 新規ステージは最低限の Start を中央に配置 (削除可能)
            data.parts.Add(new CustomStagePartPlacement
            {
                partId = "PlatformStart",
                worldPosition = new Vector3(0f, 0f, 0f),
                rotationY = 0f,
            });
            return data;
        }

        public string ToJson(bool prettyPrint = true)
        {
            return JsonUtility.ToJson(this, prettyPrint);
        }

        public static CustomStageData FromJson(string json)
        {
            return JsonUtility.FromJson<CustomStageData>(json);
        }
    }
}
