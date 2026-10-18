using UnityEngine;

public class MouseController : MonoBehaviour
{
    [SerializeField] private Board board;
    private Camera currentCamera;

    private void Update()
    {
        if (!Settings.Playing && Settings.GameMode != GameMode.Replay) return;
        if (board.IsGameOver || board.IsWaitingForTransformation) return;
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit info, 500, TileLayers.Mask))
        {
            // Выделение поля
            Vector2Int hitPosition = board.LookUpTileIndex(info.transform.gameObject);
            board.HoverTile(hitPosition);
        }
        else board.RemoveHover();

        // Обработка нажатия ЛКМ
        if (Input.GetMouseButtonDown(0) && (Physics.Raycast(ray, out info, 500, TileLayers.Mask)))
        {
            Vector2Int hitPosition = board.LookUpTileIndex(info.transform.gameObject);
            board.SelectTile(hitPosition);
        }
    }
}
