using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ReplayController : MonoBehaviour
{
    [SerializeField] public Board Board;

    private List<ReplayMove> moves;
    private int index;

    void Start()
    {
        if (Settings.GameMode != GameMode.Replay)
        {
            enabled = false;
            return;
        }

        string text = File.ReadAllText(Settings.ReplayFilePath);
        GameRecord record = IpgnParser.Parse(text);
        moves = ReplayExpander.Expand(record);
        index = 0;
        Board.HighlightLastMove(-Vector2Int.one, -Vector2Int.one);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
            Forward();
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            Back();
    }

    private void Forward()
    {
        if (moves == null || index >= moves.Count) return;
        ReplayMove move = moves[index];
        Board.SetTiles(move.From, move.FromAfter, move.To, move.ToAfter);
        index++;
        Highlight();
    }

    private void Back()
    {
        if (moves == null || index <= 0) return;
        index--;
        ReplayMove move = moves[index];
        Board.SetTiles(move.From, move.FromBefore, move.To, move.ToBefore);
        Highlight();
    }

    private void Highlight()
    {
        if (index == 0)
        {
            Board.HighlightLastMove(-Vector2Int.one, -Vector2Int.one);
            return;
        }
        ReplayMove move = moves[index - 1];
        Board.HighlightLastMove(move.From, move.To);
    }
}
