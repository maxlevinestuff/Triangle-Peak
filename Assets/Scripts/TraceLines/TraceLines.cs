using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TraceLines : MonoBehaviour
{
    public TraceNode traceNodeObject;
    public DrawLineBetweenTwoObjects traceLineObject;

    public Material traceMaterial;

    [System.Serializable]
    public struct NodeInput
    {
        public int nodeId;
        public Vector3 position;
        public int[] adjencent;
    }
    [System.Serializable]
    public struct EdgeInput
    {
        public int node1, node2;
    }

    public NodeInput[] startNodes;
    public NodeInput[] hintNodes;
    public EdgeInput[] hintEdges;

    private Dictionary<int, TraceNode> traceNodes = new Dictionary<int, TraceNode>();
    public List<DrawLineBetweenTwoObjects> hintEdgeObjects = new List<DrawLineBetweenTwoObjects>();

    // Start is called before the first frame update
    void Awake()
    {
        AddNodes(startNodes, false);
    }

    public void AddHint()
    {
        AddNodes(hintNodes, true);
    }

    public void AddNodes(NodeInput[] nodeInput, bool isHint)
    {
        foreach (NodeInput n in nodeInput)
        {
            TraceNode newNode = Instantiate(traceNodeObject);
            newNode.transform.position = n.position;
            newNode.Initialize(n.nodeId, n.adjencent, isHint);
            traceNodes[n.nodeId] = newNode;

        }

        foreach (NodeInput n in nodeInput)
        {
            TraceNode newlyAddedNode = traceNodes[n.nodeId];

            foreach (TraceNode other in traceNodes.Values)
            {
                if (newlyAddedNode != other)
                {
                    if (newlyAddedNode.adjacent.Contains(other.nodeId))
                    {
                        DrawLineBetweenTwoObjects newLine = Instantiate(traceLineObject);
                        if (!newlyAddedNode.isHint)
                        {
                            newLine.LineThickness = 0.5f;
                        }
                        else
                        {
                            newLine.LineThickness = 0.25f;
                            hintEdgeObjects.Add(newLine);
                        }

                        float startCutoff = newlyAddedNode.isHint ? .07f : .14f;
                        float endCutoff = other.isHint ? .07f : .14f;

                        newLine.Initialize(newlyAddedNode.transform, other.transform, startCutoff, endCutoff);
                        newLine.CreateLineRenderer(traceMaterial);
                    }
                }
            }
        }

        if (isHint)
        {
            foreach (EdgeInput edgeInput in hintEdges)
            {
                DrawLineBetweenTwoObjects newLine = Instantiate(traceLineObject);
                newLine.LineThickness = 0.25f;
                hintEdgeObjects.Add(newLine);
                newLine.Initialize(traceNodes[edgeInput.node1].transform, traceNodes[edgeInput.node2].transform, .14f, .14f);
                newLine.CreateLineRenderer(traceMaterial);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
