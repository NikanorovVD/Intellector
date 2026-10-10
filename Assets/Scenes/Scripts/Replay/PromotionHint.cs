using UnityEngine;

public class PromotionHint : MonoBehaviour
{
    [SerializeField] private Board board;
    [SerializeField] private GameObject[] piecesPrefabs;
    [SerializeField] private Material material;

    private GameObject ghost;
    private PieceType? ghostType;
    private bool ghostTeam;

    public void Show(Vector2Int square, PieceType type, bool team)
    {
        if (board == null || !OnBoard(square))
        {
            Clear();
            return;
        }
        if (ghost == null || ghostType != type || ghostTeam != team)
        {
            Clear();
            ghost = Instantiate(piecesPrefabs[(int)type], board.transform);
            ghostType = type;
            ghostTeam = team;
            if (type == PieceType.Agressor && team)
                ghost.transform.rotation = Quaternion.Euler(0, 90, 0);
            Collider[] colliders = ghost.GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;
            if (material != null)
            {
                MeshRenderer[] renderers = ghost.GetComponentsInChildren<MeshRenderer>();
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].sharedMaterials = new Material[] { material };
                    renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderers[i].receiveShadows = false;
                }
            }
        }
        ghost.transform.position = board.TransformCoordinates(square.x, square.y);
    }

    public void Clear()
    {
        if (ghost != null)
        {
            Destroy(ghost);
            ghost = null;
        }
        ghostType = null;
    }

    private void OnDestroy()
    {
        Clear();
    }

    private bool OnBoard(Vector2Int square)
    {
        IPiece[][] pieces = board.Pieces;
        if (pieces == null || square.x < 0 || square.x >= pieces.Length)
            return false;
        return square.y >= 0 && square.y < pieces[square.x].Length;
    }
}
