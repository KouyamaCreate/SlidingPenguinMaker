using UnityEngine;
using UnityEngine.EventSystems;

namespace StageMaker
{
    /// <summary>
    /// パレット項目をドラッグして 3D シーンにパーツを配置するためのハンドラ。
    /// IBeginDragHandler / IDragHandler / IEndDragHandler を使用してドラッグ操作を受け取る。
    /// </summary>
    public class PaletteDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        private StageMakerEditorView editor;
        private string partId;
        private GameObject ghost;
        private DraggablePart ghostDraggable;

        public void Initialize(StageMakerEditorView e, string id)
        {
            editor = e;
            partId = id;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (editor == null) return;
            // クリックでパーツ選択 (消しゴム選択のため)
            editor.SelectPaletteItem(partId);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (editor == null) return;
            if (partId == StageMakerEditorView.EraserId) return;

            editor.SelectPaletteItem(partId);
            ghost = editor.SpawnGhostFromPalette(partId, eventData);
            if (ghost != null)
            {
                ghostDraggable = ghost.GetComponent<DraggablePart>();
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (ghost == null) return;
            if (editor.TryRaycastGround(eventData.position, out Vector3 hitPoint))
            {
                ghost.transform.position = hitPoint + (ghostDraggable != null && ghostDraggable.definition != null
                    ? ghostDraggable.definition.spawnOffset
                    : Vector3.zero);
                if (ghostDraggable != null && ghostDraggable.placement != null && ghostDraggable.definition != null)
                {
                    ghostDraggable.placement.worldPosition = hitPoint;
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (ghost == null) return;
            // ドラッグ終了時にゴーストが地面より上にある場合のみ確定
            bool overScene = editor.TryRaycastGround(eventData.position, out _);
            editor.FinalizeGhost(ghost, ghostDraggable, accepted: overScene);
            ghost = null;
            ghostDraggable = null;
        }
    }
}
