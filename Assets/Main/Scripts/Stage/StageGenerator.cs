using System.Collections.Generic;
using StageMaker;
using UnityEngine;

public enum StageType
{
    Practice = 0,
    FirstStage = 1,
    SecondStage = 2,
    ThirdStage = 3,
    Custom = 4,
}

public class StageGenerator : MonoBehaviour
{
    [SerializeField]
    private static StageType stageType = StageType.Practice;

    /// <summary>
    /// stageType == Custom のときに読み込む CustomStageData の id
    /// </summary>
    private static string selectedCustomStageId = string.Empty;

    /// <summary>
    /// 直近に生成した CustomStageData。CSV エクスポート時にステージ情報を書き出すために使う。
    /// </summary>
    private static CustomStageData lastBuiltCustomStage;

    [SerializeField]
    private List<GameObject> stagePrefabs;

    // ===== プレイ用のシーンか本番用か設定用かを判別するための変数 =====
    [SerializeField]
    private bool isSettingMode = false;
    public bool IsSettingMode => isSettingMode;

    private static bool isLatestSceneSettingMode; // 最後にプレイしたモードが本番用か設定用かを判別するための変数

    private void Awake()
    {
        GenerateStage((int)stageType);
        isLatestSceneSettingMode = isSettingMode;

        // カスタムステージのときはプレイヤーを PlatformStart の上に再配置する。
        // (PlayerRespawnController は最初に踏んだ Platform を respawn 候補にするため、
        //  プレイヤーが落水する位置から始まると即座にスタート不能になる)
        if ((StageType)((int)stageType) == StageType.Custom)
        {
            RepositionPlayerToStart();
        }
    }

    private static void RepositionPlayerToStart()
    {
        var startGo = GameObject.FindGameObjectWithTag("Start");
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (startGo == null || playerGo == null) { return; }

        // PlatformStart の少し上に置く
        Vector3 startPos = startGo.transform.position + new Vector3(0f, 1.0f, 0f);
        playerGo.transform.position = startPos;
        playerGo.transform.rotation = Quaternion.identity;
    }

    private void GenerateStage(int index)
    {
        Debug.Log("Generating stage with index: " + index);

        if ((StageType)index == StageType.Custom)
        {
            BuildCustomStage();
            return;
        }

        if (stagePrefabs == null || stagePrefabs.Count == 0)
        {
            Debug.LogWarning("[StageGenerator] stagePrefabs is empty.");
            return;
        }

        // リストの範囲内にインデックスを正規化
        int normalizedIndex = ((index % stagePrefabs.Count) + stagePrefabs.Count) % stagePrefabs.Count;
        Instantiate(stagePrefabs[normalizedIndex], Vector3.zero, Quaternion.identity);
    }

    private void BuildCustomStage()
    {
        var data = CustomStageRepository.Load(selectedCustomStageId);
        if (data == null)
        {
            Debug.LogError($"[StageGenerator] Custom stage '{selectedCustomStageId}' not found. Falling back to Practice.");
            stageType = StageType.Practice;
            GenerateStage((int)StageType.Practice);
            return;
        }

        CustomStageBuilder.Build(data);
        lastBuiltCustomStage = data;
    }

    public static CustomStageData GetLastBuiltCustomStage()
    {
        return lastBuiltCustomStage;
    }

    public static void SetStageType(StageType newStageType)
    {
        stageType = newStageType;
    }

    public static StageType GetStageType()
    {
        return stageType;
    }

    public static void SetSelectedCustomStageId(string stageId)
    {
        selectedCustomStageId = stageId ?? string.Empty;
    }

    public static string GetSelectedCustomStageId()
    {
        return selectedCustomStageId;
    }

    public static bool GetIsLatestSceneSettingMode()
    {
        return isLatestSceneSettingMode;
    }
}
