using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class StoryGraphWindow : EditorWindow
{
    private StoryGraph graph;
    private Vector2 scrollPos;
    
    private float zoom = 1.0f;
    private const float ZoomMin = 0.25f;
    private const float ZoomMax = 2.0f;
    
    private Vector2 panOffset = Vector2.zero;
    
    private Rect graphRect;
    
    private List<Rect> nodeRects = new List<Rect>();
    
    private int nodeToRemove = -1;

    
    private bool isDraggingLink = false;
    private int linkStartNodeIndex = -1;
    private Vector2 linkCurrentWorldPos;

    
    [MenuItem("Tools/Story Graph Editor")]
    public static void Open()
    {
        GetWindow<StoryGraphWindow>("Story Graph");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();

        graph = (StoryGraph)EditorGUILayout.ObjectField("Graph Asset", graph, typeof(StoryGraph), false);

        if (graph == null)
        {
            EditorGUILayout.HelpBox("스토리 그래프를 선택해주세요. (Project 뷰에서 Story/Graph 생성 후 드래그)", MessageType.Info);
            return;
        }

        DrawToolbar();
        
        graphRect = new Rect(0, 50, position.width, position.height - 50);
        
        HandleZoomAndPan();
        
        GUI.BeginGroup(graphRect);

        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(panOffset, Quaternion.identity, Vector3.one * zoom);
        Matrix4x4 graphMatrix = Matrix4x4.TRS(panOffset, Quaternion.identity, Vector3.one * zoom);
        Matrix4x4 invGraphMatrix = graphMatrix.inverse;
        
        //DrawGrid(20, 0.2f);
        //DrawGrid(100, 0.4f);

        nodeRects.Clear();

        BeginWindows();
        
        int windowIndex = 0;
        
        for (int i = 0; i < graph.storyNodes.Count; i++)
        {
            StoryNode node = graph.storyNodes[i];

            int nodeHeight = 220; // no more .nextNodeIds.Count here
            Vector2 size = new Vector2(220, nodeHeight);

            Rect rect = new Rect(node.nodePosition, size);
            rect = GUI.Window(windowIndex, rect, id => DrawNodeWindow(id, node), node.nodeTitle);

            node.nodePosition = rect.position;
            nodeRects.Add(rect);

            windowIndex++;
        }
        
        for (int i = 0; i < graph.branchNodes.Count; i++)
        {
            BranchNode node = graph.branchNodes[i];

            int nodeHeight = 200 + node.nextNodeIds.Count * 20;
            Vector2 size = new Vector2(220, nodeHeight);

            Rect rect = new Rect(node.nodePosition, size);
            rect = GUI.Window(windowIndex, rect, id => DrawNodeWindow(id, node), node.nodeTitle);

            node.nodePosition = rect.position;
            nodeRects.Add(rect);

            windowIndex++;
        }

        EndWindows();
        
        DrawConnections();
        GUI.matrix = oldMatrix;
        HandleLinkEvents(invGraphMatrix);
        GUI.EndGroup();
        
        if (nodeToRemove >= 0 && nodeToRemove < graph.storyNodes.Count)
        {
            DeleteNode(nodeToRemove);
            nodeToRemove = -1;
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(graph);
        }
    }



    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("+ Add Node", GUILayout.Width(100)))
        {
            Undo.RecordObject(graph, "Add Node");

            var newNode = new StoryNode();
            
            Vector2 viewCenterScreen = graphRect.center;
            Vector2 viewCenterGraph = (viewCenterScreen - panOffset) / zoom;
            
            Vector2 nodeSize = new Vector2(220, 200);
            newNode.nodePosition = viewCenterGraph - nodeSize * 0.5f;

            graph.storyNodes.Add(newNode);
        }
        
        if (GUILayout.Button("+ Add Branch", GUILayout.Width(100)))
        {
            Undo.RecordObject(graph, "Add Branch");

            var newNode = new BranchNode();
            
            Vector2 viewCenterScreen = graphRect.center;
            Vector2 viewCenterGraph = (viewCenterScreen - panOffset) / zoom;
            
            Vector2 nodeSize = new Vector2(220, 200);
            newNode.nodePosition = viewCenterGraph - nodeSize * 0.5f;

            graph.branchNodes.Add(newNode);
        }
        
        if (GUILayout.Button("Reset View", GUILayout.Width(100)))
        {
            zoom = 1f;
            panOffset = Vector2.zero;
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }


    private void DrawNodeWindow(int index, Node node)
    {
        EditorGUI.BeginChangeCheck();
            
        GUILayout.Label("Title:");
        node.nodeTitle = EditorGUILayout.TextField(node.nodeTitle);
        
        if (node is StoryNode storyNode)
        {
            GUILayout.Label("Text:");
            storyNode.nodeContent = EditorGUILayout.TextArea(storyNode.nodeContent, GUILayout.Height(40));
        }

        // ───────────── StoryNode: exactly ONE next node ─────────────
        if (node is StoryNode story)
        {
            GUILayout.Label("Next Node:");

            string[] options = BuildNodeNameArray();

            int currentIndex = FindNodeIndexById(story.nextNodeId);
            int newIndex = EditorGUILayout.Popup(currentIndex, options);

            if (newIndex >= 0 && newIndex < TotalNodeCount)
            {
                Node target = GetNodeByFlatIndex(newIndex);
                if (target != null)
                {
                    story.nextNodeId = target.nodeId;
                }
            }

            // no +Add Connection, no "-" here
        }
        // ───────────── BranchNode: MULTIPLE next nodes ─────────────
        else if (node is BranchNode branch)
        {
            GUILayout.Label("Next Nodes:");

            if (branch.nextNodeIds == null)
                branch.nextNodeIds = new List<string>();

            string[] options = BuildNodeNameArray();

            for (int i = 0; i < branch.nextNodeIds.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                int currentIndex = FindNodeIndexById(branch.nextNodeIds[i]);
                int newIndex = EditorGUILayout.Popup(currentIndex, options);

                if (newIndex >= 0 && newIndex < TotalNodeCount)
                {
                    Node target = GetNodeByFlatIndex(newIndex);
                    if (target != null)
                    {
                        branch.nextNodeIds[i] = target.nodeId;
                    }
                }

                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    branch.nextNodeIds.RemoveAt(i);
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("+ Add Connection"))
            {
                if (TotalNodeCount > 0)
                {
                    Node first = GetNodeByFlatIndex(0);
                    branch.nextNodeIds.Add(first.nodeId);
                }
            }
        }


        GUILayout.Space(5);
        
        // Common bottom bar (Delete + drag-link)
        EditorGUILayout.BeginHorizontal();
            
        GUILayout.FlexibleSpace();
        GUI.color = Color.red;
        if (node is StoryNode)   // for now: only allow delete for story nodes since DeleteNode uses story index
        {
            if (GUILayout.Button("Delete", GUILayout.Width(60)))
            {
                nodeToRemove = index;
            }
        }
        GUI.color = Color.white;
            
        if (GUILayout.RepeatButton("●", GUILayout.Width(24)))
        {
            if (!isDraggingLink)
            {
                isDraggingLink = true;
                linkStartNodeIndex = index;
                
                if (index < nodeRects.Count)
                {
                    Rect r = nodeRects[index];
                    linkCurrentWorldPos = new Vector2(r.xMax, r.center.y);
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        GUI.DragWindow();

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(graph);
        }
    }
    
    private void DrawConnections()
    {
        if (graph == null) return;
        if (nodeRects.Count < TotalNodeCount) return;

        Handles.BeginGUI();

        // STORY → (any node)
        for (int i = 0; i < graph.storyNodes.Count; i++)
        {
            StoryNode node = graph.storyNodes[i];
            if (string.IsNullOrEmpty(node.nextNodeId))
                continue;

            Rect fromRect = nodeRects[i]; // story windows are first
            Vector3 fromPos = new Vector3(fromRect.xMax, fromRect.center.y, 0);

            int targetIndex = FindNodeIndexById(node.nextNodeId);
            if (targetIndex < 0 || targetIndex >= nodeRects.Count) continue;
            if (targetIndex == i) continue;

            Rect toRect = nodeRects[targetIndex];
            Vector3 toPos = new Vector3(toRect.xMin, toRect.center.y, 0);

            Vector3 startTangent = fromPos + Vector3.right * 50f;
            Vector3 endTangent   = toPos   + Vector3.left  * 50f;

            Handles.DrawBezier(
                fromPos,
                toPos,
                startTangent,
                endTangent,
                Color.white,
                null,
                3f
            );
        }

        // BRANCH → (any node)
        int branchBaseIndex = graph.storyNodes.Count;

        for (int b = 0; b < graph.branchNodes.Count; b++)
        {
            BranchNode node = graph.branchNodes[b];
            Rect fromRect = nodeRects[branchBaseIndex + b];
            Vector3 fromPos = new Vector3(fromRect.xMax, fromRect.center.y, 0);

            foreach (string nextId in node.nextNodeIds)
            {
                int targetIndex = FindNodeIndexById(nextId);
                if (targetIndex < 0 || targetIndex >= nodeRects.Count) continue;

                Rect toRect = nodeRects[targetIndex];
                Vector3 toPos = new Vector3(toRect.xMin, toRect.center.y, 0);

                Vector3 startTangent = fromPos + Vector3.right * 50f;
                Vector3 endTangent   = toPos   + Vector3.left  * 50f;

                Handles.DrawBezier(
                    fromPos,
                    toPos,
                    startTangent,
                    endTangent,
                    Color.white,
                    null,
                    3f
                );
            }
        }

        // yellow temp drag line part can stay as you wrote
        Handles.EndGUI();
    }




    
    private void DeleteNode(int index)
    {
        if (index < 0 || index >= graph.storyNodes.Count)
            return;

        Undo.RecordObject(graph, "Delete Node");

        string removedId = graph.storyNodes[index].nodeId;
    
        graph.storyNodes.RemoveAt(index);
    
        // Fix references in StoryNodes
        foreach (var node in graph.storyNodes)
        {
            if (node.nextNodeId == removedId)
                node.nextNodeId = null; // or "" if you prefer
        }

        // Fix references in BranchNodes
        foreach (var branch in graph.branchNodes)
        {
            branch.nextNodeIds.RemoveAll(id => id == removedId);
        }
    }

    
    private void HandleZoomAndPan()
    {
        Event e = Event.current;
        
        if (e.type == EventType.ScrollWheel && graphRect.Contains(e.mousePosition))
        {
            float zoomDelta = -e.delta.y * 0.05f;
            float oldZoom = zoom;
            float newZoom = Mathf.Clamp(zoom + zoomDelta, ZoomMin, ZoomMax);

            if (!Mathf.Approximately(newZoom, oldZoom))
            {
                Vector2 mousePos = e.mousePosition;
                Vector2 mouseGraphBefore = (mousePos - panOffset) / oldZoom;
                zoom = newZoom;
                Vector2 mouseGraphAfter = mouseGraphBefore * zoom + panOffset;
                panOffset += mousePos - mouseGraphAfter;
            }

            e.Use();
        }
        
        if (e.type == EventType.MouseDrag &&
            (e.button == 2 || e.button == 1) &&
            graphRect.Contains(e.mousePosition))
        {
            panOffset += e.delta;
            e.Use();
        }
    }
    
    
    
    private void DrawGrid(float spacing, float alpha)
    {
        Handles.BeginGUI();
        Handles.color = new Color(0.5f, 0.5f, 0.5f, alpha);
        
        float worldWidth  = position.width / zoom;
        float worldHeight = position.height / zoom;
        
        float halfWidth  = worldWidth  * 1.5f;
        float halfHeight = worldHeight * 1.5f;
        
        for (float x = -halfWidth; x <= halfWidth; x += spacing)
        {
            Handles.DrawLine(
                new Vector3(x, -halfHeight, 0),
                new Vector3(x,  halfHeight, 0));
        }

        for (float y = -halfHeight; y <= halfHeight; y += spacing)
        {
            Handles.DrawLine(
                new Vector3(-halfWidth, y, 0),
                new Vector3( halfWidth, y, 0));
        }

        Handles.EndGUI();
    }
    
    private void HandleLinkEvents(Matrix4x4 invGraphMatrix)
    {
        Event e = Event.current;
        Vector2 mouseWorld = invGraphMatrix.MultiplyPoint(e.mousePosition);

        if (!isDraggingLink)
            return;

        switch (e.type)
        {
            case EventType.MouseDrag:
            case EventType.MouseMove:
            {
                linkCurrentWorldPos = mouseWorld;
                Repaint();
                break;
            }

            case EventType.MouseUp:
            {
                if (e.button == 0)
                {
                    int targetIndex = -1;
                    for (int i = 0; i < nodeRects.Count; i++)
                    {
                        if (i == linkStartNodeIndex) continue;
                        if (nodeRects[i].Contains(mouseWorld))
                        {
                            targetIndex = i;
                            break;
                        }
                    }

                    if (targetIndex != -1)
                    {
                        Undo.RecordObject(graph, "Add Connection (Drag)");

                        // Determine from-node type based on index
                        Node fromNode;
                        bool fromIsStory;
                        int storyCount = graph.storyNodes.Count;

                        if (linkStartNodeIndex < storyCount)
                        {
                            fromNode = graph.storyNodes[linkStartNodeIndex];
                            fromIsStory = true;
                        }
                        else
                        {
                            int branchIndex = linkStartNodeIndex - storyCount;
                            fromNode = graph.branchNodes[branchIndex];
                            fromIsStory = false;
                        }

                        // Target is always a StoryNode (by your BuildNodeNameArray logic)
                        string targetId = graph.storyNodes[targetIndex].nodeId;

                        if (fromIsStory)
                        {
                            // StoryNode: overwrite single nextNodeId
                            ((StoryNode)fromNode).nextNodeId = targetId;
                        }
                        else
                        {
                            // BranchNode: add to list if not already present
                            BranchNode bn = (BranchNode)fromNode;
                            if (!bn.nextNodeIds.Contains(targetId))
                            {
                                bn.nextNodeIds.Add(targetId);
                            }
                        }

                        EditorUtility.SetDirty(graph);
                    }
                    
                    isDraggingLink = false;
                    linkStartNodeIndex = -1;
                    e.Use();
                }
                break;
            }
        }
    }

    private int TotalNodeCount => graph.storyNodes.Count + graph.branchNodes.Count;

    private Node GetNodeByFlatIndex(int index)
    {
        int storyCount = graph.storyNodes.Count;

        if (index < 0 || index >= TotalNodeCount)
            return null;

        if (index < storyCount)
            return graph.storyNodes[index];
        else
            return graph.branchNodes[index - storyCount];
    }

    private int FindNodeIndexById(string id)
    {
        if (string.IsNullOrEmpty(id)) return -1;

        // Story nodes first
        for (int i = 0; i < graph.storyNodes.Count; i++)
        {
            if (graph.storyNodes[i].nodeId == id)
                return i; // 0..storyCount-1
        }

        // Then branch nodes
        int offset = graph.storyNodes.Count;
        for (int i = 0; i < graph.branchNodes.Count; i++)
        {
            if (graph.branchNodes[i].nodeId == id)
                return offset + i; // storyCount..end
        }

        return -1;
    }

    private string[] BuildNodeNameArray()
    {
        int storyCount  = graph.storyNodes.Count;
        int branchCount = graph.branchNodes.Count;

        string[] arr = new string[storyCount + branchCount];

        for (int i = 0; i < storyCount; i++)
        {
            arr[i] = $"S{i}: {graph.storyNodes[i].nodeTitle}";
        }

        for (int i = 0; i < branchCount; i++)
        {
            int idx = storyCount + i;
            arr[idx] = $"B{i}: {graph.branchNodes[i].nodeTitle}";
        }

        return arr;
    }

}



