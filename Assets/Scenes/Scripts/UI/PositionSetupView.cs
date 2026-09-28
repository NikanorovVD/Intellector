using System.Globalization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PositionSetupView : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] Image[] pieceFrames;
    [SerializeField] Image whiteTeamFrame;
    [SerializeField] Image blackTeamFrame;
    [SerializeField] Image eraseFrame;
    [SerializeField] Image whiteMoveFrame;
    [SerializeField] Image blackMoveFrame;
    [SerializeField] InputField halfmoveInput;
    [SerializeField] InputField fullmoveInput;
    [SerializeField] Text preview;
    [SerializeField] RectTransform copyButton;
    [SerializeField] Text status;
    [SerializeField] Camera previewCamera;
    [SerializeField] Transform previewRoot;

    private const int PreviewLayer = 3;
    private const float MaxIfenWidth = 348f;
    private const float CopyGap = 8f;
    private const float CopySize = 28f;

    private static readonly Color Selected = new Color(0.91f, 0.72f, 0.54f, 1f);
    private static readonly Color Idle = new Color(0.25f, 0.25f, 0.25f, 1f);
    private static readonly Color PieceIdle = new Color(0.62f, 0.54f, 0.44f, 1f);

    private GameObject[] previewPieces;
    private RenderTexture[] previewTextures;

    public string HalfmoveText => halfmoveInput != null ? halfmoveInput.text : "0";
    public string FullmoveText => fullmoveInput != null ? fullmoveInput.text : "1";

    private void OnDestroy()
    {
        ClearClockListeners();
        ReleasePreviews();
    }

    public void SetPanelActive(bool on)
    {
        if (panel != null)
            panel.SetActive(on);
    }

    public void SetClocks(int half, int full)
    {
        if (halfmoveInput != null)
            halfmoveInput.text = half.ToString(CultureInfo.InvariantCulture);
        if (fullmoveInput != null)
            fullmoveInput.text = full.ToString(CultureInfo.InvariantCulture);
    }

    public void AddClockListener(UnityAction<string> listener)
    {
        if (halfmoveInput != null)
            halfmoveInput.onValueChanged.AddListener(listener);
        if (fullmoveInput != null)
            fullmoveInput.onValueChanged.AddListener(listener);
    }

    public void ClearClockListeners()
    {
        if (halfmoveInput != null)
            halfmoveInput.onValueChanged.RemoveAllListeners();
        if (fullmoveInput != null)
            fullmoveInput.onValueChanged.RemoveAllListeners();
    }

    public void SetIfen(string ifen)
    {
        if (preview == null)
            return;

        preview.text = ifen ?? string.Empty;
        float width = Mathf.Min(preview.preferredWidth, MaxIfenWidth);
        RectTransform rect = preview.rectTransform;
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(preview.preferredHeight, CopySize));
        if (copyButton == null)
            return;

        Vector2 position = copyButton.anchoredPosition;
        position.x = rect.anchoredPosition.x + width + CopyGap;
        position.y = rect.anchoredPosition.y - (rect.sizeDelta.y - CopySize) * 0.5f;
        copyButton.anchoredPosition = position;
    }

    public void CopyIfen()
    {
        if (preview == null)
            return;
        GUIUtility.systemCopyBuffer = preview.text ?? string.Empty;
    }

    public void SetStatus(string text)
    {
        if (status != null)
            status.text = text ?? string.Empty;
    }

    public void ShowSelection(int pieceIndex, bool erase, bool blackPieces, bool blackToMove)
    {
        if (pieceFrames != null)
        {
            for (int i = 0; i < pieceFrames.Length; i++)
            {
                if (pieceFrames[i] == null) continue;
                pieceFrames[i].color = !erase && pieceIndex == i ? Selected : PieceIdle;
            }
        }
        Paint(whiteTeamFrame, !erase && !blackPieces);
        Paint(blackTeamFrame, !erase && blackPieces);
        Paint(eraseFrame, erase);
        Paint(whiteMoveFrame, !blackToMove);
        Paint(blackMoveFrame, blackToMove);
    }

    public void BuildPreviews(Board board, bool black)
    {
        if (board == null || previewCamera == null || previewRoot == null || pieceFrames == null || pieceFrames.Length == 0)
            return;

        ReleasePreviews();
        previewPieces = new GameObject[pieceFrames.Length];
        previewTextures = new RenderTexture[pieceFrames.Length];
        for (int i = 0; i < pieceFrames.Length; i++)
        {
            previewPieces[i] = board.CreatePalettePiece((PieceType)i, black);
            SetLayer(previewPieces[i], PreviewLayer);
            previewPieces[i].transform.SetParent(previewRoot, true);
            previewTextures[i] = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
            previewTextures[i].antiAliasing = 4;
            previewTextures[i].Create();

            if (pieceFrames[i] == null)
                continue;
            Text letter = pieceFrames[i].GetComponentInChildren<Text>(true);
            if (letter != null)
                letter.gameObject.SetActive(false);
            RawImage view = pieceFrames[i].GetComponentInChildren<RawImage>(true);
            if (view != null)
            {
                view.texture = previewTextures[i];
                view.raycastTarget = false;
            }
        }

        RenderPreviews(board, black);
    }

    public void RenderPreviews(Board board, bool black)
    {
        if (board == null || previewCamera == null || previewPieces == null)
            return;

        for (int i = 0; i < previewPieces.Length; i++)
        {
            GameObject piece = previewPieces[i];
            if (piece == null)
                continue;

            board.ApplyPieceMaterial(piece, black);
            piece.SetActive(true);
            for (int j = 0; j < previewPieces.Length; j++)
                if (j != i && previewPieces[j] != null)
                    previewPieces[j].SetActive(false);

            piece.transform.position = new Vector3(0f, -200f, 0f);
            piece.transform.rotation = (PieceType)i == PieceType.agressor && black
                ? Quaternion.Euler(0f, 90f, 0f)
                : Quaternion.identity;

            Renderer[] renderers = piece.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                continue;
            Bounds bounds = renderers[0].bounds;
            for (int r = 1; r < renderers.Length; r++)
                bounds.Encapsulate(renderers[r].bounds);

            float extent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            if (extent < 0.0001f)
                extent = 0.5f;
            Vector3 center = bounds.center;
            previewCamera.orthographicSize = Mathf.Max(bounds.extents.y, bounds.extents.x) * 1.2f;
            previewCamera.transform.position = center + new Vector3(0f, bounds.extents.y * 0.15f, extent * 4f);
            previewCamera.transform.LookAt(center, Vector3.up);
            previewCamera.targetTexture = previewTextures[i];
            previewCamera.Render();
            piece.SetActive(false);
        }
    }

    private void ReleasePreviews()
    {
        if (previewTextures != null)
        {
            for (int i = 0; i < previewTextures.Length; i++)
            {
                if (previewTextures[i] != null)
                    previewTextures[i].Release();
            }
            previewTextures = null;
        }
        if (previewPieces != null)
        {
            for (int i = 0; i < previewPieces.Length; i++)
                if (previewPieces[i] != null)
                    Destroy(previewPieces[i]);
            previewPieces = null;
        }
    }

    private static void SetLayer(GameObject root, int layer)
    {
        root.layer = layer;
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
            children[i].gameObject.layer = layer;
    }

    private static void Paint(Image image, bool selected)
    {
        if (image != null)
            image.color = selected ? Selected : Idle;
    }
}
