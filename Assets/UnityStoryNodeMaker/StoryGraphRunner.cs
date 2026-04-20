using UnityEngine;

public class StoryGraphRunner : MonoBehaviour
{
    public StoryGraph graph;

    public Node CurrentNode { get; private set; }

    private void Start()
    {
        StartGraph();
    }

    public void StartGraph()
    {
        if (graph == null)
        {
            Debug.LogError("StoryGraphRunner: graph is null.");
            return;
        }

        CurrentNode = graph.GetDefaultStartNode();
        EnterCurrentNode();
    }

    /// <summary>
    /// Called whenever we move into a node.
    /// Triggers that node's event.
    /// </summary>
    private void EnterCurrentNode()
    {
        if (CurrentNode == null)
        {
            Debug.Log("Story finished or no start node.");
            return;
        }

        Debug.Log($"[Story] Enter node: {CurrentNode.nodeTitle}");
        
        CurrentNode.NodeEvent?.Invoke();

        if (CurrentNode is StoryNode storyNode)
        {
            Debug.Log($"[Story Text] {storyNode.nodeContent}");
        }
        else if (CurrentNode is BranchNode)
        {
            Debug.Log("[Branch] Waiting for player choice...");
        }
    }

    /// <summary>
    /// Called by UI / scripts when you want to go from a StoryNode
    /// to its single next node.
    /// </summary>
    public void Next()
    {
        if (CurrentNode is not StoryNode story)
        {
            Debug.LogWarning("Next() called but current node is not a StoryNode. Use ChooseBranch() for branches.");
            return;
        }

        if (string.IsNullOrEmpty(story.nextNodeId))
        {
            Debug.Log("No next node, story ends here.");
            CurrentNode = null;
            return;
        }

        CurrentNode = graph.GetNodeById(story.nextNodeId);
        EnterCurrentNode();
    }

    /// <summary>
    /// Called by UI / scripts when you're on a BranchNode and the player picks a branch.
    /// </summary>
    public void ChooseBranch(int optionIndex)
    {
        if (CurrentNode is not BranchNode branch)
        {
            Debug.LogWarning("ChooseBranch called but current node is not a BranchNode.");
            return;
        }

        if (optionIndex < 0 || optionIndex >= branch.nextNodeIds.Count)
        {
            Debug.LogWarning($"Invalid branch index {optionIndex}");
            return;
        }

        string nextId = branch.nextNodeIds[optionIndex];
        Node nextNode = graph.GetNodeById(nextId);

        if (nextNode == null)
        {
            Debug.LogWarning($"Branch points to missing node id: {nextId}");
            return;
        }

        CurrentNode = nextNode;
        EnterCurrentNode();
    }
}
