using System;
using TMPro;
using UnityEngine;

public class UImanager : MonoBehaviour
{
    public TMP_Text title;
    public TMP_Text subtitle;
    public GameObject contents;
    
    StoryGraphRunner runner;

    private void Awake()
    {
        runner = FindFirstObjectByType<StoryGraphRunner>();
    }


    private void Update()
    {

        if (runner.CurrentNode is StoryNode storyNode)
        {
            title.text = runner.CurrentNode.nodeTitle;
            subtitle.text = storyNode.nodeContent;
        }

        if (runner.CurrentNode is BranchNode branchNode)
        {
            for (int i = 0; i < branchNode.nextNodeIds.Count; i++)
            {
                //GameObject option = contents.transform.GetChild(i).gameObject;
            }
        }
    }
}
