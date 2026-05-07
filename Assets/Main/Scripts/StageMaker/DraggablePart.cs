using UnityEngine;

namespace StageMaker
{
    /// <summary>
    /// 編集中ステージに置かれた 3D パーツに付くマーカ MonoBehaviour。
    /// 入力ハンドリングはエディタ本体 (StageMakerEditorView) が一括管理するため、
    /// このコンポーネントはデータ参照だけを保持する。
    /// </summary>
    public class DraggablePart : MonoBehaviour
    {
        public CustomStagePartPlacement placement;
        public StagePartDefinition definition;

        public void Initialize(CustomStagePartPlacement p, StagePartDefinition def)
        {
            placement = p;
            definition = def;
        }
    }
}
