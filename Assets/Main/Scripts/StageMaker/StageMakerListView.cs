using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StageMaker
{
    /// <summary>
    /// ステージ一覧ビュー (デフォルト4ステージ + 自作ステージ)。
    /// </summary>
    public class StageMakerListView : MonoBehaviour
    {
        private static readonly (StageType type, string label)[] DefaultStages =
        {
            (StageType.Practice,    "Practice Stage"),
            (StageType.FirstStage,  "1st Stage"),
            (StageType.SecondStage, "2nd Stage"),
            (StageType.ThirdStage,  "3rd Stage"),
        };

        private StageMakerSceneController controller;
        private RectTransform listContent;

        public void Initialize(StageMakerSceneController c)
        {
            controller = c;
            BuildLayout();
        }

        private void BuildLayout()
        {
            // 一覧ビューだけがフルスクリーン背景を持つ (編集ビューでは 3D シーンを見せるため非表示)
            var bg = StageMakerUIFactory.AddRect(gameObject, "Background",
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            StageMakerUIFactory.AddImage(bg.gameObject, new Color(0.07f, 0.10f, 0.20f, 1f));

            // ヘッダ
            var header = StageMakerUIFactory.AddRect(gameObject, "Header",
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -120), new Vector2(0, 0));
            StageMakerUIFactory.AddImage(header.gameObject, new Color(0.10f, 0.13f, 0.25f, 1f));

            StageMakerUIFactory.CreateText(header.gameObject, "Title", "Sliding Penguin Maker",
                36, Color.white, TextAnchor.MiddleLeft,
                new Vector2(0, 0), new Vector2(1, 1));
            ((RectTransform)header.GetChild(header.childCount - 1)).offsetMin = new Vector2(40, 0);

            // 戻るボタン (タイトルへ)
            var (backGo, backBtn, _) = StageMakerUIFactory.CreateButton(header.gameObject, "BackButton",
                "← Title", new Color(0.20f, 0.25f, 0.45f, 1f), Color.white, new Vector2(180, 60));
            var backRt = backGo.GetComponent<RectTransform>();
            backRt.anchorMin = new Vector2(1, 0.5f);
            backRt.anchorMax = new Vector2(1, 0.5f);
            backRt.anchoredPosition = new Vector2(-110, 0);
            backBtn.onClick.AddListener(() => controller.BackToTitle());

            // 一覧スクロール領域
            var scrollGo = new GameObject("StageScrollView", typeof(RectTransform));
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.SetParent(transform, false);
            scrollRt.anchorMin = new Vector2(0, 0);
            scrollRt.anchorMax = new Vector2(1, 1);
            scrollRt.offsetMin = new Vector2(60, 100);
            scrollRt.offsetMax = new Vector2(-60, -140);

            var scrollImage = scrollGo.AddComponent<Image>();
            scrollImage.color = new Color(0.05f, 0.07f, 0.15f, 0.6f);
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollGo.AddComponent<RectMask2D>();

            // Viewport
            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.SetParent(scrollGo.transform, false);
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            scrollRect.viewport = viewportRt;

            // Content
            var contentGo = new GameObject("Content", typeof(RectTransform));
            listContent = contentGo.GetComponent<RectTransform>();
            listContent.SetParent(viewportGo.transform, false);
            listContent.anchorMin = new Vector2(0, 1);
            listContent.anchorMax = new Vector2(1, 1);
            listContent.pivot = new Vector2(0.5f, 1);
            listContent.anchoredPosition = Vector2.zero;
            listContent.sizeDelta = new Vector2(0, 0);
            scrollRect.content = listContent;

            var vlayout = contentGo.AddComponent<VerticalLayoutGroup>();
            vlayout.padding = new RectOffset(20, 20, 20, 20);
            vlayout.spacing = 12;
            vlayout.childAlignment = TextAnchor.UpperCenter;
            vlayout.childControlHeight = false;
            vlayout.childControlWidth = true;
            vlayout.childForceExpandHeight = false;
            vlayout.childForceExpandWidth = true;

            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            BuildFooterButtons();
        }

        private Text statusText;

        private void BuildFooterButtons()
        {
            // 新規作成ボタン (中央)
            var (newGo, newBtn, _) = StageMakerUIFactory.CreateButton(gameObject, "NewStageButton",
                "+ New Stage", new Color(0.20f, 0.55f, 0.30f, 1f), Color.white, new Vector2(260, 70));
            var newRt = newGo.GetComponent<RectTransform>();
            newRt.anchorMin = new Vector2(0.5f, 0);
            newRt.anchorMax = new Vector2(0.5f, 0);
            newRt.anchoredPosition = new Vector2(0, 60);
            newBtn.onClick.AddListener(() =>
            {
                var newData = CustomStageData.CreateNew("New Stage");
                controller.EnterEditor(newData);
            });

            // Import ボタン (左)
            var (importGo, importBtn, _) = StageMakerUIFactory.CreateButton(gameObject, "ImportButton",
                "Import", new Color(0.30f, 0.45f, 0.65f, 1f), Color.white, new Vector2(160, 60));
            var importRt = importGo.GetComponent<RectTransform>();
            importRt.anchorMin = new Vector2(0.5f, 0);
            importRt.anchorMax = new Vector2(0.5f, 0);
            importRt.anchoredPosition = new Vector2(-220, 60);
            importBtn.onClick.AddListener(() =>
            {
                int count = CustomStageRepository.ImportFromFolder();
                ShowStatus(count > 0
                    ? $"Imported {count} stage(s) from {CustomStageRepository.GetImportFolderPath()}"
                    : $"No JSON found in {CustomStageRepository.GetImportFolderPath()} (open and drop files there).");
                controller.ReloadStages();
                controller.ShowList();
            });

            // Open Folder ボタン (右) — エクスポート/インポート両フォルダの親を開く
            var (openGo, openBtn, _) = StageMakerUIFactory.CreateButton(gameObject, "OpenFolderButton",
                "Open Folder", new Color(0.45f, 0.40f, 0.55f, 1f), Color.white, new Vector2(160, 60));
            var openRt = openGo.GetComponent<RectTransform>();
            openRt.anchorMin = new Vector2(0.5f, 0);
            openRt.anchorMax = new Vector2(0.5f, 0);
            openRt.anchoredPosition = new Vector2(220, 60);
            openBtn.onClick.AddListener(() =>
            {
                CustomStageRepository.OpenFolderInOS(CustomStageRepository.GetExportFolderPath());
                CustomStageRepository.OpenFolderInOS(CustomStageRepository.GetImportFolderPath());
            });

            // ステータス表示テキスト (フッタ下)
            var statusGo = new GameObject("Status", typeof(RectTransform));
            var statusRt = statusGo.GetComponent<RectTransform>();
            statusRt.SetParent(transform, false);
            statusRt.anchorMin = new Vector2(0, 0);
            statusRt.anchorMax = new Vector2(1, 0);
            statusRt.pivot = new Vector2(0.5f, 0);
            statusRt.anchoredPosition = new Vector2(0, 16);
            statusRt.sizeDelta = new Vector2(-40, 24);
            statusText = statusGo.AddComponent<Text>();
            statusText.font = StageMakerUIFactory.GetFont();
            statusText.fontSize = 14;
            statusText.color = new Color(0.85f, 0.92f, 1f, 1f);
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.horizontalOverflow = HorizontalWrapMode.Overflow;
            statusText.text = "";
        }

        private void ShowStatus(string msg)
        {
            if (statusText != null) { statusText.text = msg; }
            Debug.Log("[StageMakerListView] " + msg);
        }

        public void Refresh(List<CustomStageData> customStages)
        {
            // 既存の行を削除
            for (int i = listContent.childCount - 1; i >= 0; i--)
            {
                Destroy(listContent.GetChild(i).gameObject);
            }

            // デフォルトステージを表示
            foreach (var (type, label) in DefaultStages)
            {
                CreateDefaultRow(label, type);
            }

            // 自作ステージを表示
            foreach (var data in customStages)
            {
                CreateCustomRow(data);
            }
        }

        private void CreateDefaultRow(string label, StageType type)
        {
            var row = CreateRowBase(label, "Default", new Color(0.18f, 0.22f, 0.32f, 1f));
            CreateRowButton(row, "Play", new Color(0.20f, 0.50f, 0.85f, 1f),
                () => controller.PlayDefaultStage(type));
        }

        private void CreateCustomRow(CustomStageData data)
        {
            string subTitle = $"Custom · {data.parts.Count} parts";
            var row = CreateRowBase(data.displayName, subTitle, new Color(0.20f, 0.30f, 0.42f, 1f));
            CreateRowButton(row, "Edit", new Color(0.50f, 0.55f, 0.20f, 1f),
                () => controller.EnterEditor(data));
            CreateRowButton(row, "Play", new Color(0.20f, 0.50f, 0.85f, 1f),
                () => controller.PlayCustomStage(data.id));
            CreateRowButton(row, "Export", new Color(0.30f, 0.45f, 0.65f, 1f),
                () =>
                {
                    string path = CustomStageRepository.Export(data);
                    ShowStatus(string.IsNullOrEmpty(path) ? "Export failed." : "Exported to: " + path);
                });
            CreateRowButton(row, "Delete", new Color(0.65f, 0.20f, 0.20f, 1f),
                () =>
                {
                    CustomStageRepository.Delete(data.id);
                    controller.ReloadStages();
                    controller.ShowList();
                });
        }

        private GameObject CreateRowBase(string title, string subtitle, Color bg)
        {
            var rowGo = new GameObject(title, typeof(RectTransform));
            var rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.SetParent(listContent, false);
            rowRt.sizeDelta = new Vector2(0, 90);

            var img = rowGo.AddComponent<Image>();
            img.color = bg;

            // タイトルテキスト
            var titleGo = new GameObject("Title", typeof(RectTransform));
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.SetParent(rowGo.transform, false);
            titleRt.anchorMin = new Vector2(0, 0.5f);
            titleRt.anchorMax = new Vector2(0.7f, 1);
            titleRt.offsetMin = new Vector2(20, 0);
            titleRt.offsetMax = new Vector2(0, 0);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = StageMakerUIFactory.GetFont();
            titleText.text = title;
            titleText.fontSize = 26;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.LowerLeft;

            // サブタイトル
            var subGo = new GameObject("Subtitle", typeof(RectTransform));
            var subRt = subGo.GetComponent<RectTransform>();
            subRt.SetParent(rowGo.transform, false);
            subRt.anchorMin = new Vector2(0, 0);
            subRt.anchorMax = new Vector2(0.7f, 0.5f);
            subRt.offsetMin = new Vector2(20, 4);
            subRt.offsetMax = new Vector2(0, 0);
            var subText = subGo.AddComponent<Text>();
            subText.font = StageMakerUIFactory.GetFont();
            subText.text = subtitle;
            subText.fontSize = 16;
            subText.color = new Color(0.78f, 0.84f, 0.96f, 1f);
            subText.alignment = TextAnchor.UpperLeft;

            // ボタン用 HorizontalLayoutGroup
            var btnsGo = new GameObject("Buttons", typeof(RectTransform));
            var btnsRt = btnsGo.GetComponent<RectTransform>();
            btnsRt.SetParent(rowGo.transform, false);
            btnsRt.anchorMin = new Vector2(0.7f, 0);
            btnsRt.anchorMax = new Vector2(1, 1);
            btnsRt.offsetMin = new Vector2(0, 10);
            btnsRt.offsetMax = new Vector2(-10, -10);
            var hlayout = btnsGo.AddComponent<HorizontalLayoutGroup>();
            hlayout.padding = new RectOffset(0, 0, 0, 0);
            hlayout.spacing = 8;
            hlayout.childAlignment = TextAnchor.MiddleRight;
            hlayout.childControlHeight = true;
            hlayout.childControlWidth = false;
            hlayout.childForceExpandHeight = true;
            hlayout.childForceExpandWidth = false;

            return btnsGo;
        }

        private void CreateRowButton(GameObject parent, string label, Color color, System.Action onClick)
        {
            var (go, btn, _) = StageMakerUIFactory.CreateButton(parent, label, label, color, Color.white, new Vector2(110, 60));
            btn.onClick.AddListener(() => onClick?.Invoke());
        }
    }
}
