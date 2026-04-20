using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StoryNode: Node
{
    public string nodeSpeaker;
    [TextArea(2, 5)]
    public string nodeContent;

    public string nextNodeId;
}