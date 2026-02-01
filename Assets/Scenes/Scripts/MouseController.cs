using UnityEngine;

public class MouseController : MonoBehaviour
{
    [SerializeField] private Board board;
    private Camera _currentCamera;

    private static readonly string[] _layersNames = {"Tile","HoverTile","SelectedTile", "Available", "HoverAvailable", "HoverSelected"};

    void Update()
    {
        if (board.game_over || board.wait_for_transformation) return;
        if (!_currentCamera)
        {
            _currentCamera = Camera.main;
            return;
        }

        Ray ray = _currentCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit info, 500, LayerMask.GetMask(_layersNames)))
        {
            //Выделение поля
            Vector2Int hitPosition = board.LookUpTileIndex(info.transform.gameObject);
            board.HoverTile(hitPosition);
        }
        else board.RemoveHover();

        //обработка нажатия ЛКМ
        if (Input.GetMouseButtonDown(0) && (Physics.Raycast(ray, out info, 500, LayerMask.GetMask(_layersNames))))
        {
            Vector2Int hitPosition = board.LookUpTileIndex(info.transform.gameObject);
            board.SelectTile(hitPosition);
        }
    }
}
