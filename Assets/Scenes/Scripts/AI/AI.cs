using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class AI : MonoBehaviour
{
    [SerializeField] Board main_board;
    TreeNode root;
    TreeNode current_node;
    VirtualBoard virtual_board;

    public static bool AI_team = true;
    int AI_depth = 2;

    List<TreeNode> end_points;

    private void Start()
    {
        if (!AI_team) MakeAIMove(new(), new(), 0);
        if (Settings.GameMode == GameMode.AI)
            main_board.MoveEndEvent += MakeAIMove;
    }


    private void MakeAIMove(Vector2Int start, Vector2Int end, int transform_info)
    {
        if (main_board.game_over) return;
        MakeTree(AI_depth);
        int best_valution = AI_team ? (end_points.Min(x => x.valuation)) : (end_points.Max(x => x.valuation));
        List <TreeNode> best_nodes = end_points.FindAll(x => x.valuation == best_valution);

        int random = new System.Random().Next(best_nodes.Count);
        TreeNode node = best_nodes[random];

        while(node.depth != 1)
        {
            node = node.parent;
        }
        Move move = node.move;
        main_board.MovePiece(new(move.start_x, move.start_y), new(move.end_x, move.end_y), (int)move.end_type);
    }

    private void MakeTree(int depth)
    {
        virtual_board = new VirtualBoard(main_board.pieces, 0);
        root = new TreeNode();
        end_points = new List<TreeNode>();
        current_node = root;

        while (true)
        {
            if (current_node.viewed_children == null)
            {
                CreateAllChildren(current_node);
            }

            if(current_node.depth == depth-1)
            {

                int best_valuation = (AI_team) ? current_node.unviewed_children.Max(x => x.valuation) : current_node.unviewed_children.Min(x => x.valuation);
                TreeNode best_node = current_node.unviewed_children.Find(x => x.valuation == best_valuation);
                end_points.Add(best_node);
                GoUp();
                continue;
            }
            
            if(current_node.unviewed_children.Count != 0)
            {
                GoDown();
                continue;
            }

            if (current_node.parent != null)
            {
                GoUp();
            }
            else return;
            
        }
    }

    private void CreateAllChildren(TreeNode node)
    {
        node.viewed_children = new();
        node.unviewed_children = new();

        List<Move> moves = virtual_board.GetAllMoves();
        foreach (Move move in moves)
        {
            virtual_board.MakeMove(move);
            node.unviewed_children.Add(new TreeNode(move, virtual_board.Valuation, node));
            virtual_board.CancelMove(move);
        }
    }

    private void GoUp()
    {
        virtual_board.CancelMove(current_node.move);
        current_node = current_node.parent;
    }

    private void GoDown()
    {
        TreeNode node_to_view = current_node.unviewed_children[0];
        current_node.viewed_children.Add(node_to_view);
        current_node.unviewed_children.Remove(node_to_view);

        virtual_board.MakeMove(node_to_view.move);
        current_node = node_to_view;
    }
}
