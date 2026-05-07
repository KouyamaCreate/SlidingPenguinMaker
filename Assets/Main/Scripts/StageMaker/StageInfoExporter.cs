using System;
using System.Text;
using UnityEngine;

namespace StageMaker
{
    /// <summary>
    /// CSV エクスポート時に同梱する stage.json のバイト列を生成する。
    /// </summary>
    public static class StageInfoExporter
    {
        public const string FileName = "stage.json";

        [Serializable]
        private class ExportedStageInfo
        {
            public string exportedAt;
            public string stageType;
            public string customStageId;
            public CustomStageData customStage; // Custom のときのみ実体が入る
        }

        /// <summary>
        /// 直近にプレイしたステージ情報を JSON バイト列として返す。
        /// </summary>
        public static byte[] BuildJsonBytes()
        {
            var info = new ExportedStageInfo
            {
                exportedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                stageType = StageGenerator.GetStageType().ToString(),
                customStageId = StageGenerator.GetSelectedCustomStageId(),
                customStage = StageGenerator.GetStageType() == StageType.Custom
                    ? StageGenerator.GetLastBuiltCustomStage()
                    : null,
            };

            string json = JsonUtility.ToJson(info, true);
            // BOM 付き UTF-8 にして Excel などでの可読性を担保
            byte[] preamble = Encoding.UTF8.GetPreamble();
            byte[] body = Encoding.UTF8.GetBytes(json);
            byte[] result = new byte[preamble.Length + body.Length];
            Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
            Buffer.BlockCopy(body, 0, result, preamble.Length, body.Length);
            return result;
        }
    }
}
