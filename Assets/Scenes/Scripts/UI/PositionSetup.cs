using System.Globalization;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PositionSetup : MonoBehaviour
{
    [SerializeField] private Board board;
    [SerializeField] private PositionSetupView view;

    private PieceType selectedType = PieceType.Progressor;
    private Side selectedSide;
    private bool erase;

    private void OnDestroy()
    {
        ArrangeSession.Clear();
    }

    private void Start()
    {
        if (!ArrangeSession.Arranging)
        {
            if (view != null)
                view.SetPanelActive(false);
            enabled = false;
            return;
        }

        view.SetPanelActive(true);
        int half = Settings.StartPosition != null ? Settings.StartPosition.HalfmoveClock : 0;
        int full = Settings.StartPosition != null ? Settings.StartPosition.FullmoveNumber : 1;
        view.SetClocks(half, full);
        view.AddClockListener(_ => RefreshPreview());
        view.BuildPreviews(board, selectedSide);
        RefreshSelection();
        RefreshPreview();
    }

    public void SelectPiece(int type)
    {
        erase = false;
        selectedType = (PieceType)type;
        RefreshSelection();
    }

    public void SelectWhite()
    {
        erase = false;
        selectedSide = Side.White;
        view.RenderPreviews(board, selectedSide);
        RefreshSelection();
    }

    public void SelectBlack()
    {
        erase = false;
        selectedSide = Side.Black;
        view.RenderPreviews(board, selectedSide);
        RefreshSelection();
    }

    public void SelectErase()
    {
        erase = true;
        RefreshSelection();
    }

    public void SelectWhiteMove()
    {
        board.Turn = Side.White;
        RefreshSelection();
        RefreshPreview();
    }

    public void SelectBlackMove()
    {
        board.Turn = Side.Black;
        RefreshSelection();
        RefreshPreview();
    }

    public void ClearBoard()
    {
        board.ClearPieces();
        RefreshPreview();
    }

    public void SetStandardPosition()
    {
        board.LoadPosition(IfenFormatter.Initial());
        view.SetClocks(0, 1);
        RefreshSelection();
        RefreshPreview();
    }

    public void Confirm()
    {
        if (!TryReadClocks(out int half, out int full, out string error))
        {
            view.SetStatus(error);
            return;
        }

        string ifen = IfenFormatter.Format(board.ToRecordedPosition(half, full));
        if (!Settings.TrySetStartIfen(ifen, out error))
        {
            view.SetStatus(error);
            return;
        }

        Leave();
    }

    public void Cancel()
    {
        if (string.IsNullOrEmpty(ArrangeSession.Backup))
            Settings.ClearStartPosition();
        else
            Settings.TrySetStartIfen(ArrangeSession.Backup, out _);
        Leave();
    }

    private void Update()
    {
        if (!ArrangeSession.Arranging || board == null) return;

        Camera camera = Camera.main;
        if (camera == null) return;

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        bool hit = Physics.Raycast(ray, out RaycastHit info, 500, TileLayers.Mask);
        if (hit)
            board.HoverTile(board.LookUpTileIndex(info.transform.gameObject));
        else
            board.RemoveHover();

        if (!Input.GetMouseButtonDown(0) || !hit || IsPointerOverUi())
            return;

        Vector2Int pos = board.LookUpTileIndex(info.transform.gameObject);
        TileState current = board.GetTileState(pos);
        TileState next = null;
        if (!erase && !(current != null && current.Type == selectedType && current.Team == selectedSide))
            next = new TileState { Type = selectedType, Team = selectedSide };
        board.SetPiece(pos, next);
        RefreshPreview();
    }

    private void RefreshSelection()
    {
        view.ShowSelection((int)selectedType, erase, selectedSide, board.Turn);
    }

    private void RefreshPreview()
    {
        TryReadClocks(out int half, out int full, out _);
        view.SetIfen(IfenFormatter.Format(board.ToRecordedPosition(half, full)));
        view.SetStatus(string.Empty);
    }

    private bool TryReadClocks(out int half, out int full, out string error)
    {
        if (!int.TryParse((view.HalfmoveText ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out half) || half < 0)
        {
            half = 0;
            full = 1;
            error = "Некорректное число полуходов";
            return false;
        }
        if (!int.TryParse((view.FullmoveText ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out full) || full < 1)
        {
            full = 1;
            error = "Номер хода должен быть не меньше 1";
            return false;
        }
        error = null;
        return true;
    }

    private static bool IsPointerOverUi()
    {
        if (EventSystem.current == null) return false;
        if (EventSystem.current.IsPointerOverGameObject()) return true;
        for (int i = 0; i < Input.touchCount; i++)
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                return true;
        return false;
    }

    private void Leave()
    {
        ArrangeSession.ReturnTo(ArrangeSession.Mode);
        SceneManager.LoadScene(0);
    }
}
