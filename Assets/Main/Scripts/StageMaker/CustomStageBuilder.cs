using System.Collections.Generic;
using UnityEngine;

namespace StageMaker
{
    /// <summary>
    /// CustomStageData から実際のゲームオブジェクトを生成するビルダー。
    /// PlayerRespawnController が GameObject.Find("Platforms") を呼ぶので、
    /// PlatformController を持つパーツは "Platforms" 配下に配置する。
    /// SharkController は親に SharkManager を要求するので、Shark は "Sharks" 配下に置く。
    /// </summary>
    public static class CustomStageBuilder
    {
        public static GameObject Build(CustomStageData data, Transform parent = null)
        {
            if (data == null)
            {
                Debug.LogError("[CustomStageBuilder] data is null");
                return null;
            }

            var catalog = StagePartCatalog.Load();
            if (catalog == null)
            {
                Debug.LogError("[CustomStageBuilder] catalog not loaded");
                return null;
            }

            var root = new GameObject("CustomStage_" + data.displayName);
            if (parent != null) { root.transform.SetParent(parent, false); }
            root.AddComponent<CustomStageInfo>().SetData(data);

            // カテゴリごとの親を準備
            var groupRoots = new Dictionary<StagePartCategory, Transform>();
            groupRoots[StagePartCategory.Platform] = CreateGroup(root.transform, "Platforms");
            groupRoots[StagePartCategory.Special]  = groupRoots[StagePartCategory.Platform]; // Start/Goal も Platforms 配下
            groupRoots[StagePartCategory.Item]     = CreateGroup(root.transform, "Items");
            groupRoots[StagePartCategory.Gimmick]  = CreateGroup(root.transform, "Gimmicks");

            // Enemy はパーツごとにラッパー親が異なる (Seal は単独配置 OK、Shark は SharkManager 必須)
            var enemyRoot = CreateGroup(root.transform, "Enemies");
            var sealsRoot = CreateGroup(enemyRoot, "Seals");
            // Sharks は専用 manager 付きの親にぶら下げる
            var sharksGo = new GameObject("Sharks");
            sharksGo.transform.SetParent(enemyRoot, false);
            sharksGo.AddComponent<SharkManager>();
            var sharksRoot = sharksGo.transform;

            foreach (var p in data.parts)
            {
                var def = catalog.Find(p.partId);
                if (def == null || def.prefab == null)
                {
                    Debug.LogWarning($"[CustomStageBuilder] Unknown part id '{p.partId}' — skipping");
                    continue;
                }

                Transform groupParent;
                if (def.category == StagePartCategory.Enemy)
                {
                    groupParent = (def.id == "Shark") ? sharksRoot : sealsRoot;
                }
                else
                {
                    groupParent = groupRoots.TryGetValue(def.category, out var g) ? g : root.transform;
                }

                Vector3 pos = p.worldPosition + def.spawnOffset;
                Quaternion rot = Quaternion.Euler(0f, p.rotationY, 0f);
                var go = Object.Instantiate(def.prefab, pos, rot, groupParent);
                go.name = def.id;
            }

            return root;
        }

        private static Transform CreateGroup(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }
    }

    /// <summary>
    /// 生成された CustomStage に貼り付けて、元の CustomStageData を保持する。
    /// CSV エクスポート時にステージ情報を取り出すために使用される。
    /// </summary>
    public class CustomStageInfo : MonoBehaviour
    {
        private CustomStageData data;
        public CustomStageData Data => data;

        public void SetData(CustomStageData d) { data = d; }
    }
}
