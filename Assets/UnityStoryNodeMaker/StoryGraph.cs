using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewStoryGraph", menuName = "Story/Graph")]
public class StoryGraph : ScriptableObject
{
    public List<StoryNode> storyNodes = new List<StoryNode>();
    public List<BranchNode> branchNodes = new List<BranchNode>();
    
    
    public string startNodeId;

    public Node GetNodeById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        foreach (var s in storyNodes)
            if (s.nodeId == id) return s;

        foreach (var b in branchNodes)
            if (b.nodeId == id) return b;

        return null;
    }

    public Node GetDefaultStartNode()
    {
        if (!string.IsNullOrEmpty(startNodeId))
            return GetNodeById(startNodeId);

        // Fallback: first story node
        if (storyNodes.Count > 0)
            return storyNodes[0];

        // Or first branch node if no stories
        if (branchNodes.Count > 0)
            return branchNodes[0];

        return null;
    }
}
