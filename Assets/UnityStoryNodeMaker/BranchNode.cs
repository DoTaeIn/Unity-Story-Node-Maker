using System;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class BranchNode: Node
{
    public List<string> nextNodeIds = new List<string>();
}
