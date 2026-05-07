using System.Collections.Generic;
using UnityEngine;

namespace StageMaker
{
    /// <summary>
    /// パレット項目のサムネイル用に、各パーツの 3D プレビューを RenderTexture に描き出すヘルパー。
    /// 編集ビュー表示中だけ使用するので、シーン再構築時に専用リグを作って一度だけレンダする。
    /// </summary>
    public static class PalettePreviewRenderer
    {
        // プレビュー用に隔離するレイヤー (Unity のデフォルトでは 30 / 31 は通常未使用)
        private const int PreviewLayer = 30;

        // メインステージから十分離れた位置でレンダリングする
        private static readonly Vector3 PreviewRigOrigin = new Vector3(2000f, 0f, 0f);
        private const float SpacingX = 8f;
        private const int TexSize = 128;

        private static GameObject rigRoot;
        private static readonly Dictionary<string, RenderTexture> cache = new Dictionary<string, RenderTexture>();

        public static RenderTexture GetPreview(StagePartDefinition def)
        {
            if (def == null || def.prefab == null) { return null; }
            if (cache.TryGetValue(def.id, out var rt) && rt != null) { return rt; }
            return RenderPart(def);
        }

        public static void Cleanup()
        {
            if (rigRoot != null)
            {
                Object.Destroy(rigRoot);
                rigRoot = null;
            }
            foreach (var kv in cache)
            {
                if (kv.Value != null) { kv.Value.Release(); Object.Destroy(kv.Value); }
            }
            cache.Clear();
        }

        private static RenderTexture RenderPart(StagePartDefinition def)
        {
            EnsureRig();
            int index = cache.Count;

            Vector3 anchor = PreviewRigOrigin + new Vector3(index * SpacingX, 0f, 0f);

            // パーツを配置
            var inst = Object.Instantiate(def.prefab, anchor + def.spawnOffset, Quaternion.identity, rigRoot.transform);
            inst.name = "Preview_" + def.id;
            DisableGameLogic(inst);
            SetLayerRecursive(inst, PreviewLayer);

            // バウンディングボックスを使ってフィッティング
            Bounds b = ComputeBounds(inst, anchor);
            float radius = Mathf.Max(b.extents.magnitude, 0.5f);

            // 専用カメラ
            var camGo = new GameObject("PreviewCam_" + def.id);
            camGo.transform.SetParent(rigRoot.transform, false);
            Vector3 dir = new Vector3(0.6f, 0.7f, -1f).normalized;
            camGo.transform.position = b.center + dir * (radius * 3.2f);
            camGo.transform.LookAt(b.center);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.11f, 0.20f, 1f);
            cam.fieldOfView = 35f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            cam.cullingMask = 1 << PreviewLayer;
            cam.depth = -100; // メインカメラより先に処理

            var rt = new RenderTexture(TexSize, TexSize, 16, RenderTextureFormat.ARGB32);
            rt.name = "PaletteRT_" + def.id;
            rt.Create();
            cam.targetTexture = rt;
            cam.Render();
            // 一度だけレンダリングしたら無効化 (パフォーマンス節約)
            cam.enabled = false;

            cache[def.id] = rt;
            return rt;
        }

        private static void EnsureRig()
        {
            if (rigRoot != null) { return; }
            rigRoot = new GameObject("PalettePreviewRig");

            // プレビュー専用ライト
            var lightGo = new GameObject("PreviewLight");
            lightGo.transform.SetParent(rigRoot.transform, false);
            lightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = Color.white;
            light.cullingMask = 1 << PreviewLayer;
        }

        private static Bounds ComputeBounds(GameObject go, Vector3 fallback)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                return new Bounds(fallback, Vector3.one * 1.5f);
            }
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                b.Encapsulate(renderers[i].bounds);
            }
            return b;
        }

        private static void DisableGameLogic(GameObject root)
        {
            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                mb.enabled = false;
            }
            foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            foreach (var rb2 in root.GetComponentsInChildren<Rigidbody2D>(true))
            {
                rb2.bodyType = RigidbodyType2D.Kinematic;
                rb2.simulated = false;
            }
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }
    }
}
