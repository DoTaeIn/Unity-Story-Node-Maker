using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public abstract class Node
{
    public string nodeId = Guid.NewGuid().ToString();
    public string nodeTitle;
    public UnityEvent NodeEvent;
    public Vector2 nodePosition;
    public string parentNodeId;
}
