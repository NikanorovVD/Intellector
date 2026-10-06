using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

// FIXME: смешение главной логики игры и UI
public class Board : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject[] piecesPrefabs;
    [SerializeField] private float tileSize;

    [Header("Materials")]
    [SerializeField] private MaterialSelector materialSelector;
    private Material whiteTeamMaterial;
    private Material blackTeamMaterial;

    [Header("UI")]
    [SerializeField] private GameObject progressorPromotionDialog;
    [SerializeField] private GameObject intellectorSupportDialog;
    [SerializeField] private EndGame endGame;

    [NonSerialized] public bool NetworkGame;
    [NonSerialized] public bool PlayerTeam;

    public delegate void MoveDelegate(Vector2Int start, Vector2Int end, int transformInfo);
    public delegate void EndGameDelegate(bool? winner, EndGameReason reason);
    public event MoveDelegate MoveStartEvent;
    public event MoveDelegate MoveEndEvent;
    public event Action RestartEvent;
    public event EndGameDelegate EndGameEvent;

    [NonSerialized] public bool Turn;
    [NonSerialized] public bool GameOver;
    [NonSerialized] public bool WaitForTransformation;
    public IPiece[][] Pieces;
    private readonly Dictionary<IPiece, GameObject> piecesObjects = new();
    public GameObject[][] Tiles;

    private List<Vector2Int> availableMoves;

    private Vector2Int currentHover = -Vector2Int.one;
    private Vector2Int currentSelect = -Vector2Int.one;
    private Vector2Int lastMove1 = -Vector2Int.one;
    private Vector2Int lastMove2 = -Vector2Int.one;
    private Vector2Int hintMove1 = -Vector2Int.one;
    private Vector2Int hintMove2 = -Vector2Int.one;

    private static float xOffset;
    private static float yOffset;

    public void Awake()
    {
        // FIXME: не поддается понимаю почему 1.51, а не 1.5
        xOffset = tileSize / Mathf.Sqrt(3) * 1.51f;
        yOffset = tileSize;

        NetworkGame = Settings.GameMode == GameMode.Network;
        if (NetworkGame)
        {
            PlayerTeam = GameInfo.Load().Team;
        }
        else if (Settings.GameMode == GameMode.AI)
        {
            PlayerTeam = Settings.PlayerTeam;
        }
        else
        {
            PlayerTeam = false;
        }
        (whiteTeamMaterial, blackTeamMaterial) = materialSelector.GetCurrentMaterials(Settings.PieceMaterials);

        GenerateAllTiles();
        GenerateAllPieces();

        Turn = false;
        GameOver = false;
        ApplyConfiguredStart();
    }

    private void Update()
    {
        for (int i = 0; i < 9; i++)
            for (int j = 0; j < Tiles[i].Length; j++)
            {
                Vector2Int coor = new Vector2Int(i, j);
                string layer;
                if (coor == currentSelect)
                {
                    if (coor == currentHover) layer = "HoverSelected";
                    else layer = "SelectedTile";
                }
                else if (availableMoves != null && availableMoves.Contains(coor))
                {
                    if (coor == currentHover) layer = "HoverAvailable";
                    else layer = "Available";
                }
                else if (coor == hintMove1 || coor == hintMove2)
                {
                    if (coor == currentHover) layer = "HoverHint";
                    else layer = "Hint";
                }
                else if (coor == currentHover)
                    layer = "HoverTile";
                else if (coor == lastMove1 || coor == lastMove2)
                    layer = "SelectedTile";
                else
                    layer = "Tile";

                Tiles[coor.x][coor.y].layer = LayerMask.NameToLayer(layer);
            }
    }
    public Vector3 TransformCoordinates(int x, int y)
        => new Vector3(x * xOffset, 0, y * yOffset + (yOffset / 2) * (x % 2));

    //Создание поля и фигур
    public void GenerateAllTiles()
    {
        Tiles = new GameObject[9][];
        for (int i = 0; i < 9; i++)
        {
            Tiles[i] = new GameObject[7 - (i % 2)];
            for (int j = 0; j < Tiles[i].Length; j++)
                Tiles[i][j] = GenerateOneTile(i, j);
        }
    }
    public GameObject GenerateOneTile(int x, int y)
    {
        GameObject tile = Instantiate(tilePrefab, transform);
        tile.name = $"tile {x} {y}";
        tile.transform.position = TransformCoordinates(x, y);

        tile.AddComponent<BoxCollider>();
        return tile;
    }
    public void GenerateAllPieces()
    {
        Pieces = new IPiece[9][];
        for (int i = 0; i < 9; i++)
            Pieces[i] = new IPiece[7 - (i % 2)];

        Pieces[0][0] = GenerateSinglePiece(PieceType.Dominator, false, 0, 0);
        Pieces[1][0] = GenerateSinglePiece(PieceType.Liberator, false, 1, 0);
        Pieces[2][0] = GenerateSinglePiece(PieceType.Agressor, false, 2, 0);
        Pieces[3][0] = GenerateSinglePiece(PieceType.Defensor, false, 3, 0);
        Pieces[4][0] = GenerateSinglePiece(PieceType.Intellector, false, 4, 0);
        Pieces[5][0] = GenerateSinglePiece(PieceType.Defensor, false, 5, 0);
        Pieces[6][0] = GenerateSinglePiece(PieceType.Agressor, false, 6, 0);
        Pieces[7][0] = GenerateSinglePiece(PieceType.Liberator, false, 7, 0);
        Pieces[8][0] = GenerateSinglePiece(PieceType.Dominator, false, 8, 0);
        for (int i = 0; i < 9; i += 2)
            Pieces[i][1] = GenerateSinglePiece(PieceType.Progressor, false, i, 1);

        Pieces[0][6] = GenerateSinglePiece(PieceType.Dominator, true, 0, 6);
        Pieces[1][5] = GenerateSinglePiece(PieceType.Liberator, true, 1, 5);
        Pieces[2][6] = GenerateSinglePiece(PieceType.Agressor, true, 2, 6);
        Pieces[3][5] = GenerateSinglePiece(PieceType.Defensor, true, 3, 5);
        Pieces[4][6] = GenerateSinglePiece(PieceType.Intellector, true, 4, 6);
        Pieces[5][5] = GenerateSinglePiece(PieceType.Defensor, true, 5, 5);
        Pieces[6][6] = GenerateSinglePiece(PieceType.Agressor, true, 6, 6);
        Pieces[7][5] = GenerateSinglePiece(PieceType.Liberator, true, 7, 5);
        Pieces[8][6] = GenerateSinglePiece(PieceType.Dominator, true, 8, 6);
        for (int i = 0; i < 9; i += 2)
            Pieces[i][5] = GenerateSinglePiece(PieceType.Progressor, true, i, 5);

    }
    public IPiece GenerateSinglePiece(PieceType type, bool team, int x, int y)
    {
        GameObject gameObject = Instantiate(piecesPrefabs[(int)type], transform);
        IPiece piece = gameObject.GetComponent<IPiece>();
        piecesObjects[piece] = gameObject;
        piece.Team = team;
        piece.Board = Pieces;
        piece.X = x;
        piece.Y = y;

        gameObject.transform.position = TransformCoordinates(x, y);
        if ((type == PieceType.Agressor) && (team == true))
        {
            // FIXME: можно нормально повернутый префаб агрессора, пожалуйста?
            gameObject.transform.rotation = Quaternion.Euler(0, 90, 0);
        }
        gameObject.GetComponent<MeshRenderer>().materials = (team == true) ? new Material[] { blackTeamMaterial } : new Material[] { whiteTeamMaterial };
        return piece;
    }

    public void SetTiles(Vector2Int from, TileState? fromState, Vector2Int to, TileState? toState)
    {
        ClearTile(from);
        ClearTile(to);
        SetTileState(from, fromState);
        SetTileState(to, toState);
    }

    public void HighlightLastMove(Vector2Int from, Vector2Int to)
    {
        lastMove1 = from;
        lastMove2 = to;
    }

    public void HighlightHint(Vector2Int from, Vector2Int to)
    {
        hintMove1 = from;
        hintMove2 = to;
    }

    public void ClearSelection()
    {
        availableMoves = null;
        currentSelect = -Vector2Int.one;
    }

    public TileState? GetTileState(Vector2Int pos)
    {
        IPiece piece = Pieces[pos.x][pos.y];
        if (piece == null) return null;
        return new TileState { Type = piece.Type, Team = piece.Team };
    }

    private void ClearTile(Vector2Int pos)
    {
        IPiece piece = Pieces[pos.x][pos.y];
        if (piece == null) return;
        if (piecesObjects.TryGetValue(piece, out GameObject gameObject))
        {
            piecesObjects.Remove(piece);
            Destroy(gameObject);
        }
        Pieces[pos.x][pos.y] = null;
    }

    private void SetTileState(Vector2Int pos, TileState? state)
    {
        if (state == null) return;
        Pieces[pos.x][pos.y] = GenerateSinglePiece(state.Type, state.Team, pos.x, pos.y);
    }

    public void LoadPosition(RecordedPosition position)
    {
        for (int x = 0; x < Pieces.Length; x++)
            for (int y = 0; y < Pieces[x].Length; y++)
                ClearTile(new Vector2Int(x, y));
        for (int x = 0; x < Pieces.Length; x++)
            for (int y = 0; y < Pieces[x].Length; y++)
                SetTileState(new Vector2Int(x, y), position.Pieces[x][y]);
        Turn = position.BlackToMove;
    }

    public RecordedPosition ToRecordedPosition(int halfmoveClock, int fullmoveNumber)
    {
        var position = new RecordedPosition
        {
            BlackToMove = Turn,
            HalfmoveClock = halfmoveClock,
            FullmoveNumber = fullmoveNumber
        };
        for (int x = 0; x < Pieces.Length; x++)
            for (int y = 0; y < Pieces[x].Length; y++)
                position.Pieces[x][y] = GetTileState(new Vector2Int(x, y));
        return position;
    }

    public void ApplyConfiguredStart()
    {
        if (Settings.StartPosition == null) return;
        if (Settings.GameMode != GameMode.Local && Settings.GameMode != GameMode.AI) return;
        LoadPosition(Settings.StartPosition);
    }

    //очистка
    public void Restart()
    {
        if (Settings.StartPosition != null
            && (Settings.GameMode == GameMode.Local || Settings.GameMode == GameMode.AI))
        {
            LoadPosition(Settings.StartPosition);
        }
        else
        {
            DeleteAllPieces();
            GenerateAllPieces();
            Turn = false;
        }
        if (Settings.GameMode == GameMode.Network)
        {
            PlayerTeam = !PlayerTeam;
        }
        GameOver = false;
        endGame.Hide();
        RestartEvent?.Invoke();
    }
    private void DeleteAllPieces()
    {
        for (int i = 0; i < 9; i++)
            for (int j = 0; j < Pieces[i].Length; j++)
                if (Pieces[i][j] != null)
                {
                    Destroy(piecesObjects[Pieces[i][j]].GetComponent<MeshRenderer>());
                    Pieces[i][j] = null;
                }
    }
    private void DeleteAllHighlights()
    {
        availableMoves = null;
        currentHover = -Vector2Int.one;
        currentSelect = -Vector2Int.one;
        lastMove1 = -Vector2Int.one;
        lastMove2 = -Vector2Int.one;
        hintMove1 = -Vector2Int.one;
        hintMove2 = -Vector2Int.one;
    }

    //операции с полями и слоями
    public Vector2Int LookUpTileIndex(GameObject hitInfo)
    {
        for (int i = 0; i < 9; i++)
            for (int j = 0; j < Tiles[i].Length; j++)
                if (Tiles[i][j] == hitInfo)
                    return new Vector2Int(i, j);

        throw new Exception("Не найден тайл на который указывал курсор");
    }

    public void HoverTile(Vector2Int coordinates)
    {
        currentHover = coordinates;
    }

    public void RemoveHover()
    {
        currentHover = -Vector2Int.one;
    }

    //перемещение фигур
    public void SelectTile(Vector2Int coordinates)
    {
        if ((NetworkGame || Settings.GameMode == GameMode.AI) && PlayerTeam != Turn) return; //не трогаем чужие фигуры

        //если не выбрана никакая фигура
        if (currentSelect == -Vector2Int.one)
            if ((Pieces[coordinates.x][coordinates.y] != null) && (Pieces[coordinates.x][coordinates.y].Team == Turn)) //если нажали на фигуру выбираем её
            {
                currentSelect = coordinates;
                availableMoves = Pieces[coordinates.x][coordinates.y].GetAvailableMoves();
                return;
            }
            else { return; }

        //если уже выбрана
        else
        {
            if (currentSelect == coordinates) //если нажали на ту же фигуру сбрасываем выделение
            {
                currentSelect = -Vector2Int.one;
                availableMoves = null;
            }
            else //а если нажали на другое поле
            {
                if (availableMoves.Contains(coordinates)) // и туда можно пойти, то идём туда
                {
                    // и сбрасываем выделение
                    var selectBuff = currentSelect;
                    currentSelect = -Vector2Int.one;
                    availableMoves = null;

                    if (!ChekAndAskForTransformaton(selectBuff, coordinates)) //если нет никаких превращений то можно просто пойти
                        MovePiece(selectBuff, coordinates);
                }
                else // а если пойти туда нельзя
                {
                    if(Pieces[coordinates.x][coordinates.y] != null && (Pieces[coordinates.x][coordinates.y].Team == Turn)) // и там есть фигура доступная для выделения
                    {
                        //то переключаем выделение на неё
                        currentSelect = coordinates;
                        availableMoves = Pieces[coordinates.x][coordinates.y].GetAvailableMoves();
                        return;
                    }
                    else //если нет фигуры доступной для выделения
                    {
                        //сбрасываем выделение
                        currentSelect = -Vector2Int.one;
                        availableMoves = null;
                    }
                }
            }
        }
    }

    private bool ChekAndAskForTransformaton(Vector2Int start, Vector2Int end)
    {
        if ((Pieces[start.x][start.y].Type == PieceType.Progressor) && // если ходил прогрессор
        ((Pieces[start.x][start.y].Team == false && (end.y == 6)) || (Pieces[start.x][start.y].Team == true && (end.y == 0) && (end.x % 2 == 0))) //и он дошёл до поля превращения
        && ((Pieces[end.x][end.y] == null) || (Pieces[end.x][end.y].Type != PieceType.Intellector))) //и мы не съели интеллектора
        {
            progressorPromotionDialog.SetActive(true);
            StartCoroutine(WaitForPieceType(start, end));
            WaitForTransformation = true;
            return true;
        }

        else if (Pieces[end.x][end.y] != null && (Pieces[end.x][end.y].Team != Pieces[start.x][start.y].Team) //если едим вражескую фигуру
            && (Pieces[start.x][start.y].HasIntellectorNearby()) //и рядом есть интеллектор
            && (Pieces[start.x][start.y].Type != PieceType.Progressor) //и ходил не прогрессор
            && (Pieces[start.x][start.y].Type != Pieces[end.x][end.y].Type) //и тип съеденной фигуры отличается
            && (Pieces[end.x][end.y].Type != PieceType.Intellector)) //и мы съели не интеллектора
        {
            // то можно превратиться в съеденную фигуру
            intellectorSupportDialog.SetActive(true);
            StartCoroutine(WaitForTransformation(start, end));
            WaitForTransformation = true;
            return true;
        }

        return false;
    }

    public void MovePiece(Vector2Int start, Vector2Int end, int transformInfo = 200)
    {
        //проверка очерёдности хода
        if (Pieces[start.x][start.y].Team != Turn) return;

        // событие начала хода
        MoveStartEvent?.Invoke(start, end, transformInfo);

        //Переключение очерёдности хода
        Turn = !Turn;

        //Сохранение последнего хода
        lastMove1 = start;
        lastMove2 = end;

        //едим если занято вражеской фигурой
        if (Pieces[end.x][end.y] != null && (Pieces[end.x][end.y].Team != Pieces[start.x][start.y].Team))
        {
            Destroy(piecesObjects[Pieces[end.x][end.y]].GetComponent<MeshRenderer>());
            if (Pieces[end.x][end.y].Type == PieceType.Intellector)
            {
                GameOver(Pieces[start.x][start.y].Team, EndGameReason.IntellectorCapture);
            }
        }

        //перемещение в пространстве
        piecesObjects[Pieces[start.x][start.y]].transform.position = TransformCoordinates(end.x, end.y);

        //при ходе на свою фигуру
        if (Pieces[end.x][end.y] != null && (Pieces[end.x][end.y].Team == Pieces[start.x][start.y].Team))
        {
            if ( //если это дефенсор и интеллектор
                (Pieces[start.x][start.y].Type == PieceType.Intellector && Pieces[end.x][end.y].Type == PieceType.Defensor) ||
                (Pieces[start.x][start.y].Type == PieceType.Defensor && Pieces[end.x][end.y].Type == PieceType.Intellector)
               )
            {   //то меняем их местами
                Castling();
            }
            else
                throw new InvalidOperationException("Невозможный ход: ход на свою фигуру");
        }
        else
        {
            //изменение ссылок
            Pieces[end.x][end.y] = Pieces[start.x][start.y];
            Pieces[end.x][end.y].X = end.x;
            Pieces[end.x][end.y].Y = end.y;
            Pieces[start.x][start.y] = null;
        }

        //превращаемя если надо
        if (transformInfo != 200)
        {
            Destroy(piecesObjects[Pieces[end.x][end.y]].GetComponent<MeshRenderer>());
            Pieces[end.x][end.y] = GenerateSinglePiece((PieceType)transformInfo, Pieces[end.x][end.y].Team, end.x, end.y);
        }

        //"рокировка"
        void Castling()
        {
            piecesObjects[Pieces[end.x][end.y]].transform.position = TransformCoordinates(start.x, start.y);
            (Pieces[start.x][start.y], Pieces[end.x][end.y]) = (Pieces[end.x][end.y], Pieces[start.x][start.y]);
            Pieces[start.x][start.y].X = start.x;
            Pieces[start.x][start.y].Y = start.y;
            Pieces[end.x][end.y].X = end.x;
            Pieces[end.x][end.y].Y = end.y;
        }

        //достижение интеллектором базовой линии
        if ((Pieces[end.x][end.y].Type == PieceType.Intellector) &&
            (
                ((Pieces[end.x][end.y].Team == false) && (end.y == 6)) ||
                ((Pieces[end.x][end.y].Team == true) && (end.y == 0) && (end.x % 2 == 0))
            ))
        {
            GameOver(Pieces[end.x][end.y].Team, EndGameReason.IntellectorReachLustRank);
        }

        //вызов события хода
        MoveEndEvent?.Invoke(start, end, transformInfo);
    }

    //превращения
    private IEnumerator WaitForTransformation(Vector2Int start, Vector2Int end)
    {
        yield return new WaitUntil(() => !intellectorSupportDialog.activeInHierarchy);

        TransformToEaten(start, end);

        intellectorSupportDialog.GetComponent<IntellectorSupportDialog>().Answer = null;
        WaitForTransformation = false;
    }

    private IEnumerator WaitForPieceType(Vector2Int start, Vector2Int end)
    {
        yield return new WaitUntil(() => !progressorPromotionDialog.activeInHierarchy);

        ProgressorTransformation(start, end);

        progressorPromotionDialog.GetComponent<ProgressorPromotionDialog>().Answer = null;
        WaitForTransformation = false;
    }

    private void ProgressorTransformation(Vector2Int start, Vector2Int end)
    {
        PieceType newType = (PieceType)progressorPromotionDialog.GetComponent<ProgressorPromotionDialog>().Answer;
        MovePiece(start, end, (int)newType);
    }

    private void TransformToEaten(Vector2Int start, Vector2Int end)
    {
        if (intellectorSupportDialog.GetComponent<IntellectorSupportDialog>().Answer == true)
        {
            PieceType newType = Pieces[end.x][end.y].Type;
            MovePiece(start, end, (int)newType);
        }
        else
        {
            MovePiece(start, end);
        }
    }

    //конец игры
    public void GameOver(bool? winner, EndGameReason reason)
    {
        if (Settings.GameMode == GameMode.Replay) return;
        if (GameOver) return;
        GameOver = true;
        DeleteAllHighlights();
        endGame.DisplayResult(NetworkGame, winner, PlayerTeam, reason);
        EndGameEvent?.Invoke(winner, reason);
    }
}
