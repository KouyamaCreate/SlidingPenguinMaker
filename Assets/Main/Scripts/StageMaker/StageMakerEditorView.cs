using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StageMaker
{
    /// <summary>
    /// 編集ビュー。3D の世界に置かれた実際のパーツプレハブを掴んでドラッグ移動できる。
    /// 左にパーツパレット、上部にステージ名 / 保存 / プレイのボタンを表示する。
    /// </summary>
    public class StageMakerEditorView : MonoBehaviour
    {
        public const string EraserId = "__eraser__";

        private StageMakerSceneController controller;
        private StagePartCatalog catalog;
        private CustomStageData currentData;

        private InputField nameField;
        private GameObject paletteContent;
        private readonly Dictionary<string, Image> paletteSelectionFrame = new();
        private bool eraserMode;
        private string selectedPartId;   // クリックで配置するためのカレント選択
        public bool IsEraserMode => eraserMode;

        private Camera editorCamera;
        private GameObject sceneRoot;       // 3D シーンの親 (light, ground, parts)
        private Transform partsRoot;        // 配置パーツの親

        // 地面プレーンと衝突するレイヤー (デフォルトレイヤーで十分)
        private static readonly Plane GroundPlane = new Plane(Vector3.up, Vector3.zero);

        public void Initialize(StageMakerSceneController c)
        {
            controller = c;
            catalog = StagePartCatalog.Load();
            // パレットの3Dプレビュー用に先にシーンを準備しておく
            EnsureSceneInfra();
            BuildLayout();
        }

        public void LoadStage(CustomStageData data)
        {
            currentData = data;
            if (nameField != null) { nameField.text = data.displayName; }
            RebuildScene();
        }

        // ========== UI 構築 ==========

        private void BuildLayout()
        {
            // ヘッダ (戻る + ステージ名 + 保存 + プレイ)
            var header = StageMakerUIFactory.AddRect(gameObject, "Header",
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -100), new Vector2(0, 0)).gameObject;
            StageMakerUIFactory.AddImage(header, new Color(0.10f, 0.13f, 0.25f, 1f));

            var (backGo, backBtn, _) = StageMakerUIFactory.CreateButton(header, "BackButton",
                "← Back", new Color(0.20f, 0.25f, 0.45f, 1f), Color.white, new Vector2(140, 60));
            var backRt = backGo.GetComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0, 0.5f);
            backRt.anchorMax = new Vector2(0, 0.5f);
            backRt.anchoredPosition = new Vector2(90, 0);
            backBtn.onClick.AddListener(() => controller.ShowList());

            // 名前入力
            var nameGo = new GameObject("NameField", typeof(RectTransform));
            var nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.SetParent(header.transform, false);
            nameRt.anchorMin = new Vector2(0, 0.5f);
            nameRt.anchorMax = new Vector2(0, 0.5f);
            nameRt.anchoredPosition = new Vector2(420, 0);
            nameRt.sizeDelta = new Vector2(380, 60);
            var nameImg = nameGo.AddComponent<Image>();
            nameImg.color = new Color(0.94f, 0.96f, 1f, 1f);
            nameField = nameGo.AddComponent<InputField>();

            var nameTextGo = new GameObject("Text", typeof(RectTransform));
            var nameTextRt = nameTextGo.GetComponent<RectTransform>();
            nameTextRt.SetParent(nameGo.transform, false);
            nameTextRt.anchorMin = Vector2.zero;
            nameTextRt.anchorMax = Vector2.one;
            nameTextRt.offsetMin = new Vector2(12, 6);
            nameTextRt.offsetMax = new Vector2(-12, -6);
            var nameText = nameTextGo.AddComponent<Text>();
            nameText.font = StageMakerUIFactory.GetFont();
            nameText.fontSize = 22;
            nameText.color = Color.black;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameField.targetGraphic = nameImg;
            nameField.textComponent = nameText;
            nameField.onValueChanged.AddListener(v =>
            {
                if (currentData != null) { currentData.displayName = string.IsNullOrEmpty(v) ? "Untitled" : v; }
            });

            // 保存ボタン
            var (saveGo, saveBtn, _) = StageMakerUIFactory.CreateButton(header, "SaveButton",
                "Save", new Color(0.20f, 0.55f, 0.30f, 1f), Color.white, new Vector2(150, 60));
            var saveRt = saveGo.GetComponent<RectTransform>();
            saveRt.anchorMin = new Vector2(1, 0.5f);
            saveRt.anchorMax = new Vector2(1, 0.5f);
            saveRt.anchoredPosition = new Vector2(-330, 0);
            saveBtn.onClick.AddListener(SaveCurrent);

            // プレイ (即反映) ボタン
            var (playGo, playBtn, _) = StageMakerUIFactory.CreateButton(header, "PlayButton",
                "Play", new Color(0.20f, 0.50f, 0.85f, 1f), Color.white, new Vector2(160, 60));
            var playRt = playGo.GetComponent<RectTransform>();
            playRt.anchorMin = new Vector2(1, 0.5f);
            playRt.anchorMax = new Vector2(1, 0.5f);
            playRt.anchoredPosition = new Vector2(-150, 0);
            playBtn.onClick.AddListener(SaveAndPlay);

            // パレット
            BuildPalette();

            // クリア / カメラ操作のフッタ
            BuildFooter();
        }

        private void BuildPalette()
        {
            var paletteGo = new GameObject("Palette", typeof(RectTransform));
            var paletteRt = paletteGo.GetComponent<RectTransform>();
            paletteRt.SetParent(transform, false);
            paletteRt.anchorMin = new Vector2(0, 0);
            paletteRt.anchorMax = new Vector2(0, 1);
            paletteRt.pivot = new Vector2(0, 0.5f);
            paletteRt.offsetMin = new Vector2(20, 80);
            paletteRt.offsetMax = new Vector2(0, -120);
            paletteRt.sizeDelta = new Vector2(280, paletteRt.sizeDelta.y);
            var paletteImg = paletteGo.AddComponent<Image>();
            paletteImg.color = new Color(0.05f, 0.07f, 0.15f, 0.7f);

            // タイトル
            var titleText = StageMakerUIFactory.CreateText(paletteGo, "PaletteTitle", "Parts",
                20, Color.white, TextAnchor.MiddleCenter, new Vector2(0, 1), new Vector2(1, 1));
            ((RectTransform)titleText.transform).offsetMin = new Vector2(0, -40);

            // 消しゴム (専用枠)
            BuildEraserSlot(paletteGo);

            // パーツ一覧 (Vertical Layout)
            var contentGo = new GameObject("Content", typeof(RectTransform));
            paletteContent = contentGo;
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.SetParent(paletteGo.transform, false);
            contentRt.anchorMin = new Vector2(0, 0);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.offsetMin = new Vector2(8, 8);
            contentRt.offsetMax = new Vector2(-8, -110);
            var vlayout = contentGo.AddComponent<VerticalLayoutGroup>();
            vlayout.padding = new RectOffset(8, 8, 8, 8);
            vlayout.spacing = 6;
            vlayout.childControlHeight = false;
            vlayout.childControlWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.childForceExpandWidth = true;

            if (catalog != null)
            {
                foreach (var def in catalog.Parts)
                {
                    if (def == null) continue;
                    CreatePartRow(def);
                }
            }
        }

        private void BuildEraserSlot(GameObject parent)
        {
            var rowGo = new GameObject("EraserSlot", typeof(RectTransform));
            var rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.SetParent(parent.transform, false);
            rowRt.anchorMin = new Vector2(0, 1);
            rowRt.anchorMax = new Vector2(1, 1);
            rowRt.pivot = new Vector2(0.5f, 1);
            rowRt.anchoredPosition = new Vector2(0, -42);
            rowRt.offsetMin = new Vector2(8, rowRt.offsetMin.y);
            rowRt.offsetMax = new Vector2(-8, rowRt.offsetMax.y);
            rowRt.sizeDelta = new Vector2(rowRt.sizeDelta.x, 60);

            var bgImg = rowGo.AddComponent<Image>();
            bgImg.color = new Color(0.40f, 0.20f, 0.20f, 1f);
            bgImg.raycastTarget = true;

            // 選択枠
            var frameGo = new GameObject("Frame", typeof(RectTransform));
            var frameRt = frameGo.GetComponent<RectTransform>();
            frameRt.SetParent(rowGo.transform, false);
            frameRt.anchorMin = Vector2.zero;
            frameRt.anchorMax = Vector2.one;
            frameRt.offsetMin = new Vector2(2, 2);
            frameRt.offsetMax = new Vector2(-2, -2);
            var frameImg = frameGo.AddComponent<Image>();
            frameImg.color = new Color(1, 1, 1, 0);
            frameImg.raycastTarget = false;
            paletteSelectionFrame[EraserId] = frameImg;

            // 消しゴムアイコン (UnityEngineの組み込み画像が無いので、絵文字 ✕ で代用)
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.SetParent(rowGo.transform, false);
            iconRt.anchorMin = new Vector2(0, 0);
            iconRt.anchorMax = new Vector2(0.4f, 1);
            iconRt.offsetMin = new Vector2(8, 4);
            iconRt.offsetMax = new Vector2(-4, -4);
            var iconText = iconGo.AddComponent<Text>();
            iconText.font = StageMakerUIFactory.GetFont();
            iconText.text = "✕"; // ✕
            iconText.fontSize = 36;
            iconText.color = Color.white;
            iconText.alignment = TextAnchor.MiddleCenter;
            iconText.raycastTarget = false;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.SetParent(rowGo.transform, false);
            labelRt.anchorMin = new Vector2(0.4f, 0);
            labelRt.anchorMax = new Vector2(1, 1);
            labelRt.offsetMin = new Vector2(0, 0);
            labelRt.offsetMax = new Vector2(-8, 0);
            var labelText = labelGo.AddComponent<Text>();
            labelText.font = StageMakerUIFactory.GetFont();
            labelText.text = "Eraser";
            labelText.fontSize = 18;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.raycastTarget = false;

            var handler = rowGo.AddComponent<PaletteDragHandler>();
            handler.Initialize(this, EraserId);
        }

        private void CreatePartRow(StagePartDefinition def)
        {
            var rowGo = new GameObject(def.id, typeof(RectTransform));
            var rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.SetParent(paletteContent.transform, false);
            rowRt.sizeDelta = new Vector2(0, 70);

            var bgImg = rowGo.AddComponent<Image>();
            bgImg.color = new Color(0.18f, 0.22f, 0.32f, 1f);
            bgImg.raycastTarget = true;

            // 選択枠
            var frameGo = new GameObject("Frame", typeof(RectTransform));
            var frameRt = frameGo.GetComponent<RectTransform>();
            frameRt.SetParent(rowGo.transform, false);
            frameRt.anchorMin = Vector2.zero;
            frameRt.anchorMax = Vector2.one;
            frameRt.offsetMin = new Vector2(2, 2);
            frameRt.offsetMax = new Vector2(-2, -2);
            var frameImg = frameGo.AddComponent<Image>();
            frameImg.color = new Color(1, 1, 1, 0);
            frameImg.raycastTarget = false;
            paletteSelectionFrame[def.id] = frameImg;

            // 3Dプレビューサムネイル
            var thumbGo = new GameObject("Thumb", typeof(RectTransform));
            var thumbRt = thumbGo.GetComponent<RectTransform>();
            thumbRt.SetParent(rowGo.transform, false);
            thumbRt.anchorMin = new Vector2(0, 0);
            thumbRt.anchorMax = new Vector2(0, 1);
            thumbRt.pivot = new Vector2(0, 0.5f);
            thumbRt.anchoredPosition = new Vector2(8, 0);
            thumbRt.sizeDelta = new Vector2(60, 0);
            thumbRt.offsetMin = new Vector2(thumbRt.offsetMin.x, 5);
            thumbRt.offsetMax = new Vector2(thumbRt.offsetMax.x, -5);
            var rawImage = thumbGo.AddComponent<RawImage>();
            rawImage.raycastTarget = false;
            var rt = PalettePreviewRenderer.GetPreview(def);
            if (rt != null)
            {
                rawImage.texture = rt;
            }
            else
            {
                // フォールバック (色塗りつぶし)
                Destroy(rawImage);
                var img = thumbGo.AddComponent<Image>();
                img.color = def.paletteColor;
                img.raycastTarget = false;
            }

            // ラベル
            var labelGo = new GameObject("Label", typeof(RectTransform));
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.SetParent(rowGo.transform, false);
            labelRt.anchorMin = new Vector2(0, 0);
            labelRt.anchorMax = new Vector2(1, 1);
            labelRt.offsetMin = new Vector2(80, 0);
            labelRt.offsetMax = new Vector2(-8, 0);
            var labelText = labelGo.AddComponent<Text>();
            labelText.font = StageMakerUIFactory.GetFont();
            labelText.text = def.displayName;
            labelText.fontSize = 18;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.raycastTarget = false;

            var handler = rowGo.AddComponent<PaletteDragHandler>();
            handler.Initialize(this, def.id);
        }

        private void BuildFooter()
        {
            var (clearGo, clearBtn, _) = StageMakerUIFactory.CreateButton(gameObject, "ClearAll",
                "Clear All", new Color(0.45f, 0.20f, 0.20f, 1f), Color.white, new Vector2(160, 50));
            var clearRt = clearGo.GetComponent<RectTransform>();
            clearRt.anchorMin = new Vector2(0, 0);
            clearRt.anchorMax = new Vector2(0, 0);
            clearRt.anchoredPosition = new Vector2(120, 30);
            clearBtn.onClick.AddListener(() =>
            {
                if (currentData != null) { currentData.parts.Clear(); }
                RebuildScene();
            });

            // 操作説明
            var hint = StageMakerUIFactory.CreateText(gameObject, "Hint",
                "Drag from palette to place • Drag placed parts to move • Eraser then click to delete",
                16, new Color(0.85f, 0.9f, 1f, 1f), TextAnchor.LowerCenter,
                new Vector2(0, 0), new Vector2(1, 0));
            var hintRt = (RectTransform)hint.transform;
            hintRt.anchorMin = new Vector2(0, 0);
            hintRt.anchorMax = new Vector2(1, 0);
            hintRt.pivot = new Vector2(0.5f, 0);
            hintRt.anchoredPosition = new Vector2(0, 8);
            hintRt.sizeDelta = new Vector2(-300, 26);
        }

        // ========== パレット選択 ==========

        public void SelectPaletteItem(string id)
        {
            eraserMode = (id == EraserId);
            selectedPartId = eraserMode ? null : id;
            foreach (var kv in paletteSelectionFrame)
            {
                kv.Value.color = (kv.Key == id)
                    ? new Color(1, 1, 1, 0.55f)
                    : new Color(1, 1, 1, 0);
            }
        }

        // ========== 3D シーン構築 ==========

        private void EnsureSceneInfra()
        {
            if (sceneRoot != null) return;

            sceneRoot = new GameObject("StageMakerScene");

            // カメラ: 既存の Main Camera を流用
            editorCamera = Camera.main;
            if (editorCamera == null)
            {
                var camGo = new GameObject("EditorCamera");
                camGo.tag = "MainCamera";
                editorCamera = camGo.AddComponent<Camera>();
            }
            ConfigureCamera();

            // ライト
            var lightGo = new GameObject("DirectionalLight");
            lightGo.transform.SetParent(sceneRoot.transform, false);
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = Color.white;

            // 地面プレーン (水面風) — 視覚的なガイド
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "EditorGround";
            ground.transform.SetParent(sceneRoot.transform, false);
            ground.transform.position = new Vector3(0, -0.05f, 35f);
            ground.transform.localScale = new Vector3(20f, 1f, 20f); // 200x200
            var groundRenderer = ground.GetComponent<MeshRenderer>();
            // 既存マテリアルを複製してから変更 (共有マテリアル汚染を防ぐ)
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.20f, 0.45f, 0.65f, 1f);
            groundRenderer.material = mat;

            // パーツ親
            var partsGo = new GameObject("Parts");
            partsGo.transform.SetParent(sceneRoot.transform, false);
            partsRoot = partsGo.transform;

            // ステージ範囲ガイド (ワイヤー)
            DrawBoundsGuide(sceneRoot.transform);
        }

        private void ConfigureCamera()
        {
            if (editorCamera == null) return;
            editorCamera.transform.position = new Vector3(0f, 60f, 30f);
            editorCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            editorCamera.orthographic = true;
            editorCamera.orthographicSize = 38f;
            editorCamera.nearClipPlane = 0.1f;
            editorCamera.farClipPlane = 200f;
            editorCamera.clearFlags = CameraClearFlags.SolidColor;
            editorCamera.backgroundColor = new Color(0.07f, 0.10f, 0.20f, 1f);
            // パレットプレビュー用のレイヤー (30) はエディタカメラに映さない
            editorCamera.cullingMask &= ~(1 << 30);
        }

        private static void DrawBoundsGuide(Transform parent)
        {
            // 中央線 (Z 方向) の薄いラインを描く
            var guide = new GameObject("BoundsGuide");
            guide.transform.SetParent(parent, false);
            var lr = guide.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.widthMultiplier = 0.05f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = new Color(1f, 1f, 1f, 0.3f);
            lr.positionCount = 2;
            lr.SetPosition(0, new Vector3(0, 0.02f, 0));
            lr.SetPosition(1, new Vector3(0, 0.02f, 80));
        }

        public void RebuildScene()
        {
            EnsureSceneInfra();

            // 既存パーツを破棄
            if (partsRoot != null)
            {
                for (int i = partsRoot.childCount - 1; i >= 0; i--)
                {
                    Destroy(partsRoot.GetChild(i).gameObject);
                }
            }

            if (currentData == null || catalog == null) return;

            foreach (var p in currentData.parts)
            {
                SpawnPlacementInScene(p);
            }
        }

        private GameObject SpawnPlacementInScene(CustomStagePartPlacement p)
        {
            var def = catalog.Find(p.partId);
            if (def == null || def.prefab == null) return null;

            Vector3 pos = p.worldPosition + def.spawnOffset;
            Quaternion rot = Quaternion.Euler(0f, p.rotationY, 0f);
            var go = Instantiate(def.prefab, pos, rot, partsRoot);
            go.name = def.id;

            // 編集中はゲームロジック (Controller類) を走らせない。
            // (例: SharkController は SharkManager 親を要求し、Update() で NRE を起こす)
            DisableGameLogicForEditor(go);

            // 入力ハンドリングはエディタの Update() で行うので Collider 追加は不要。
            var dragger = go.AddComponent<DraggablePart>();
            dragger.Initialize(p, def);
            return go;
        }

        private static void DisableGameLogicForEditor(GameObject root)
        {
            // すべての MonoBehaviour を無効化 (Animator/Renderer/Collider/Rigidbody は MonoBehaviour ではない)
            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                mb.enabled = false;
            }
            // 物理を凍結
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

        // ========== レイキャスト (UI ↔ 3D) ==========

        public bool TryRaycastGround(out Vector3 worldHit)
        {
            return TryRaycastGround(Input.mousePosition, out worldHit);
        }

        public bool TryRaycastGround(Vector3 screenPos, out Vector3 worldHit)
        {
            if (editorCamera == null) editorCamera = Camera.main;
            if (editorCamera == null) { worldHit = Vector3.zero; return false; }

            Ray ray = editorCamera.ScreenPointToRay(screenPos);
            if (GroundPlane.Raycast(ray, out float enter))
            {
                worldHit = ray.GetPoint(enter);
                return true;
            }
            worldHit = Vector3.zero;
            return false;
        }

        // ========== パレットドラッグ ==========

        public GameObject SpawnGhostFromPalette(string partId, PointerEventData eventData)
        {
            EnsureSceneInfra();
            if (catalog == null) return null;
            var def = catalog.Find(partId);
            if (def == null || def.prefab == null) return null;
            if (currentData == null) return null;

            // ドラッグ開始時は配置データ未追加。EndDrag で確定する。
            var placement = new CustomStagePartPlacement
            {
                partId = partId,
                worldPosition = Vector3.zero,
                rotationY = 0f,
            };

            if (TryRaycastGround(eventData.position, out Vector3 hit))
            {
                placement.worldPosition = hit;
            }

            var go = SpawnPlacementInScene(placement);
            if (go == null) return null;

            // Pending: 確定するまで currentData.parts には入れない
            return go;
        }

        public void FinalizeGhost(GameObject ghost, DraggablePart ghostDraggable, bool accepted)
        {
            if (ghost == null) return;
            if (!accepted || currentData == null || ghostDraggable == null || ghostDraggable.placement == null)
            {
                Destroy(ghost);
                return;
            }

            var placement = ghostDraggable.placement;
            var def = ghostDraggable.definition;

            // unique なら既存を消してから入れ替え
            if (def != null && def.unique)
            {
                currentData.parts.RemoveAll(x => x.partId == def.id);
                // 既存のシーン上 unique 物体も破棄
                for (int i = partsRoot.childCount - 1; i >= 0; i--)
                {
                    var child = partsRoot.GetChild(i).gameObject;
                    if (child == ghost) continue;
                    var dp = child.GetComponent<DraggablePart>();
                    if (dp != null && dp.definition != null && dp.definition.id == def.id)
                    {
                        Destroy(child);
                    }
                }
            }

            currentData.parts.Add(placement);
        }

        // ========== マウス入力 (配置済みパーツのドラッグ移動 / 削除) ==========

        private DraggablePart currentDrag;
        private Vector3 currentDragGroundOffset;

        // 画面ピクセル単位の判定半径
        private const float PartPickPixelRadius = 60f;

        private void Update()
        {
            if (sceneRoot == null) { return; }
            if (editorCamera == null) { return; }

            if (Input.GetMouseButtonDown(0))
            {
                // UI の上 (パレット等) なら 3D 入力を無視
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) { return; }

                var part = FindPartUnderCursor(Input.mousePosition);

                // 既存パーツの上をクリック
                if (part != null)
                {
                    if (eraserMode)
                    {
                        RequestDelete(part);
                        return;
                    }
                    if (TryRaycastGround(out Vector3 hitForDrag))
                    {
                        currentDrag = part;
                        currentDragGroundOffset = part.transform.position - hitForDrag;
                    }
                    return;
                }

                // 既存パーツがない場所のクリック: 選択中のパーツがあれば置く (クリック配置モード)
                if (!eraserMode && !string.IsNullOrEmpty(selectedPartId)
                    && TryRaycastGround(out Vector3 placePos))
                {
                    PlacePartAt(selectedPartId, placePos);
                }
            }
            else if (Input.GetMouseButton(0) && currentDrag != null)
            {
                if (TryRaycastGround(out Vector3 hit))
                {
                    Vector3 newPos = hit + currentDragGroundOffset;
                    currentDrag.transform.position = newPos;
                    if (currentDrag.placement != null && currentDrag.definition != null)
                    {
                        currentDrag.placement.worldPosition = newPos - currentDrag.definition.spawnOffset;
                    }
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                currentDrag = null;
            }
        }

        /// <summary>
        /// カーソル位置から最も近い配置済みパーツを探す (画面距離で判定)。
        /// パーツに 3D Collider を強制せずに済むよう、Physics ではなく
        /// WorldToScreenPoint 距離で選ぶ。
        /// </summary>
        private DraggablePart FindPartUnderCursor(Vector3 mouseScreen)
        {
            if (partsRoot == null) { return null; }

            DraggablePart best = null;
            float bestDist = PartPickPixelRadius;

            for (int i = 0; i < partsRoot.childCount; i++)
            {
                var child = partsRoot.GetChild(i);
                var part = child.GetComponent<DraggablePart>();
                if (part == null) { continue; }

                Vector3 sp = editorCamera.WorldToScreenPoint(child.position);
                if (sp.z < 0f) { continue; } // カメラ後ろ
                float dx = sp.x - mouseScreen.x;
                float dy = sp.y - mouseScreen.y;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = part;
                }
            }
            return best;
        }

        public void RequestDelete(DraggablePart part)
        {
            if (part == null || currentData == null) return;
            if (part.placement != null) { currentData.parts.Remove(part.placement); }
            Destroy(part.gameObject);
        }

        /// <summary>
        /// パレット選択中のパーツを指定位置に新規配置する (クリック配置モード)。
        /// </summary>
        private void PlacePartAt(string partId, Vector3 worldPos)
        {
            if (currentData == null || catalog == null) return;
            var def = catalog.Find(partId);
            if (def == null) return;

            var placement = new CustomStagePartPlacement
            {
                partId = partId,
                worldPosition = worldPos,
                rotationY = 0f,
            };

            // unique なパーツは既存を取り除く
            if (def.unique)
            {
                currentData.parts.RemoveAll(x => x.partId == def.id);
                if (partsRoot != null)
                {
                    for (int i = partsRoot.childCount - 1; i >= 0; i--)
                    {
                        var child = partsRoot.GetChild(i);
                        var dp = child.GetComponent<DraggablePart>();
                        if (dp != null && dp.definition != null && dp.definition.id == def.id)
                        {
                            Destroy(child.gameObject);
                        }
                    }
                }
            }

            currentData.parts.Add(placement);
            SpawnPlacementInScene(placement);
        }

        // ========== 保存・プレイ ==========

        private void SaveCurrent()
        {
            if (currentData == null) return;
            CustomStageRepository.Save(currentData);
            controller.ReloadStages();
        }

        private void SaveAndPlay()
        {
            if (currentData == null) return;
            CustomStageRepository.Save(currentData);
            controller.PlayCustomStage(currentData.id);
        }

        private void OnDisable()
        {
            // 編集ビューを抜けるときは 3D シーンを破棄。プレビューリグは再表示時に再利用するので残す。
            if (sceneRoot != null)
            {
                Destroy(sceneRoot);
                sceneRoot = null;
                partsRoot = null;
            }
        }

        private void OnDestroy()
        {
            // 編集ビュー自体が破棄されるとき (シーン遷移時) のみプレビューを破棄
            PalettePreviewRenderer.Cleanup();
        }
    }
}
