using System.Collections.Generic;

public class TreeNode
{
    public AIMove move;
    public int valuation;
    public int depth;
    public TreeNode parent;
    public List<TreeNode> unviewed_children;
    public List<TreeNode> viewed_children;

    public TreeNode() //для корня
    {
        move = null;
        valuation = 0;
        depth = 0;
        parent = null;
        unviewed_children = null;
        viewed_children = null;
    }

    public TreeNode(AIMove move, int valuation, TreeNode parent)
    {
        this.move = move;
        this.valuation = valuation;
        this.depth = parent.depth + 1;
        this.parent = parent;
        this.unviewed_children = null;
        this.viewed_children = null;
    }
}
