using UnityEngine;

public class MouseController : MonoBehaviour
{
    [SerializeField] private Board board;
    private Camera currentCamera;

    private static readonly string[] layersNames = {"Tile","HoverTile","SelectedTile", "Available", "HoverAvailable", "HoverSelected", "Hint", "HoverHint"};

    private void Update()
    {
        if (Settings.Arrange.Editing) return;
        if (board.IsGameOver || board.IsWaitingForTransformation) return;
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit info, 500, LayerMask.GetMask(layersNames)))
        {
            // Выделение поля
            Vector2Int hitPosition = board.LookUpTileIndex(info.transform.gameObject);
            board.HoverTile(hitPosition);
        }
        else board.RemoveHover();

        // Обработка нажатия ЛКМ
        if (Input.GetMouseButtonDown(0) && (Physics.Raycast(ray, out info, 500, LayerMask.GetMask(layersNames))))
        {
            Vector2Int hitPosition = board.LookUpTileIndex(info.transform.gameObject);
            board.SelectTile(hitPosition);
        }
    }
}
