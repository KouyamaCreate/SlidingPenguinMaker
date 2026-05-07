using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using StageMaker;

public class TitleManager : MonoBehaviour
{
    private void Start()
    {
        AudioManager.Instance.bgm.Change(BgmType.Title);
        InjectStageMakerButton();
    }

    /// <summary>
    /// Title シーンに「Stage Maker」ボタンを動的に挿入する。
    /// シーンファイルを変更せずに済むよう実行時に追加している。
    /// </summary>
    private void InjectStageMakerButton()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) { return; }

        // 既に挿入済みなら何もしない
        var existing = canvas.transform.Find("StageMakerButton");
        if (existing != null) { return; }

        var go = new GameObject("StageMakerButton", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(1, 0);
        rt.anchoredPosition = new Vector2(-30, 30);
        rt.sizeDelta = new Vector2(280, 70);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.20f, 0.55f, 0.30f, 1f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.SetParent(go.transform, false);
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        var label = labelGo.AddComponent<Text>();
        label.font = StageMakerUIFactory.GetFont();
        label.text = "Stage Maker";
        label.fontSize = 24;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;

        btn.onClick.AddListener(OnStageMakerButtonClicked);
    }

    private void OnStageMakerButtonClicked()
    {
        AudioManager.Instance?.se.Play(SeTypeSystem.ButtonClickTransition);

        if (DataLogger.Instance != null) { DataLogger.Instance.AbortTrial(); }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.bgm.Stop();
            AudioManager.Instance.bgm.SetPitch(1.0f);
        }
        SceneManager.LoadScene("StageMaker");
    }
}
