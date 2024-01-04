using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Linq;

//Responsible for the graph data structure, which contains node objects and all other info about placed nodes
public class NetworkHandler : MonoBehaviour
{

    [SerializeField] public NodeObject nodeObject;
    [SerializeField] public EdgeLine edgeLineObject;
    [SerializeField] public Value valueObject;

    public EditSetting oldEditSetting;
    [SerializeField] public EditSetting editSetting;
    public int specificMoveNode = -1;

    //The edit settings, chosen by pressing GUI button
    public enum EditSetting { add, move, moveSpecificNode, delete, book }

    //Direct access to the graph
    public Graph graph;

    //The distance that nodes being drawn snap to
    public const float characterColliderRadius = 0.32f; //used for nodes calculating if still player colliding
    public const float nodeColliderRadius = 0.32f;

    public bool isOutsidePlayerColliderRadius(Vector3 pos) //for when cant wait a frame for collisions
    {
        return Vector3.Distance(pos, playerCharacter.transform.position) > (NetworkHandler.characterColliderRadius + NetworkHandler.nodeColliderRadius);
    }
    public bool isInsidePlayerColliderRadius(Vector3 pos) //for when cant wait a frame for collisions
    {
        return !isOutsidePlayerColliderRadius(pos);
    }

    public const float snapDistance = 1f; //should be same size or less as player collider to avoid flickering
    public const float lineSnapDistance = .31f;

    public GameObject playerCharacter;

    CameraControl cameraControl;

    public void SetEditSettingFromButton(string s)
    {
        if (s == "book" && editSetting == EditSetting.book) //exit book when pressing book button twice
            SetEditSetting("add");
        else
            SetEditSetting(s);
    }

    //change the edit setting (with a string)
    public void SetEditSetting(string s, bool callCameraControl=true) //make so no effect if animate transitioning
    {
        EditSetting initialEditSetting = editSetting;

        switch (s)
        {
            case "add": editSetting = EditSetting.add; break;
            case "move": editSetting = EditSetting.move; break;
            case "delete": editSetting = EditSetting.delete; break;
            case "book": editSetting = EditSetting.book; break;
        }

        if (editSetting != initialEditSetting)
        {
            ChangedEditSetting.Invoke();
            cameraControl.ChangedEditSetting(0);
        }
    }

    //These events trigger when a node is activated or deactivated. Their one argument tells which node.
    public class ActivatedNode : UnityEvent<int, bool> { }
    public ActivatedNode NodeActivated;

    public class DeactivatedNode : UnityEvent<int, bool> { }
    public DeactivatedNode NodeDeactivated;

    public UnityEvent ChangedEditSetting;

    void Awake()
    {
        NodeActivated = new ActivatedNode();
        NodeDeactivated = new DeactivatedNode();

        ChangedEditSetting = new UnityEvent();

        graph = new Graph(this);

        playerCharacter = GameObject.Find("PlayerCharacter");
        cameraControl = GameObject.Find("Main Camera").GetComponent<CameraControl>();

        //StartCoroutine(deleteANode());
    }

    //public IEnumerator deleteANode()
    //{
    //    yield return new WaitForSeconds(5);
    //    deleteEdge(0, 1);
    //    yield return new WaitForSeconds(5);
    //    addEge(0, 1);
    //}

    public void deleteEdge(int node1, int node2)
    {
        for (int i = graph.edgeLines.Count - 1; i >= 0; i--)
        {
            if ((graph.edgeLines[i].start.myNum == node1 && graph.edgeLines[i].end.myNum == node2) || (graph.edgeLines[i].start.myNum == node2 && graph.edgeLines[i].end.myNum == node1))
            {
                graph.graph[node1].RemoveAdjacency(node2);
                graph.graph[node2].RemoveAdjacency(node1);

                Destroy(graph.edgeLines[i].gameObject);

                graph.edgeLines.RemoveAt(i);
            }
        }
    }

    public void deleteAllEdges(int node) //hasnt been tested
    {
        for (int i = graph.graph[node].adjacentNodes.Count-1; i >= 0; i--)
        {
            deleteEdge(node, graph.graph[node].adjacentNodes[i]);
        }
    }

    public void addAllEdges(int node, List<int> edges)
    {
        foreach(int i in edges)
        {
            addEge(node, i);
        }
    }

    public void addEge(int node1, int node2)
    {
        bool alreadyEdgeObject = false;
        foreach (EdgeLine el in graph.edgeLines)
        {
            if ((el.start.myNum == node1 && el.end.myNum == node2) || (el.start.myNum == node2 && el.end.myNum == node1))
            {
                alreadyEdgeObject = true;
                break;
            }
        }
        graph.graph[node1].AddAdjacency(node2, graph, edgeLineObject, !alreadyEdgeObject);
        graph.graph[node2].AddAdjacency(node1, graph, edgeLineObject, false);
    }

    //Calls on the graph to add a new segment (before direct access to graph was added)
    public Tuple<NodeObject, NodeObject> AddSegment(Segment segment)
    {
        return graph.AddSegment(segment, nodeObject, edgeLineObject);
    }

    //Get a node directly from graph (before direct access to graph was added)
    public Graph.Node getNodeFromGraph(int id)
    {
        return graph.graph[id];
    }

    //Contains all the data about the graph structure
    public class Graph
    {
        public Graph(NetworkHandler handler) { this.Handler = handler; }

        public Dictionary<int, Node> graph = new Dictionary<int, Node>();
        public List<EdgeLine> edgeLines = new List<EdgeLine>();

        private NetworkHandler Handler;

        //Contains data for node, including a game object, which has its position, as well as a list of adjacent node ids.
        public class Node
        {
            public Node(int num, NodeObject obj)
            {
                adjacentNodes = new List<int>();
                nodeGameObject = obj;
                myNum = num;

                nodeGameObject.myNum = num;
            }

            //Add an adjecency to this node. graph and edgeLineObject are just provided to help. If adding mutual adjacency, only need one edge line object.
            public void AddAdjacency(int adjacentNode, Graph graph, EdgeLine edgeLineObject, bool shouldCreateNewEdgeLine)
            {
                if (!adjacentNodes.Contains(adjacentNode))
                {
                    adjacentNodes.Add(adjacentNode);

                    if (shouldCreateNewEdgeLine)
                    {
                        EdgeLine edgeLine = Instantiate(edgeLineObject);
                        edgeLine.Initialize(myNum, adjacentNode);
                        graph.edgeLines.Add(edgeLine);
                    }

                    graph.deactivateNode(myNum, true);
                }
            }

            //Remove adjacency to this node.
            public void RemoveAdjacency(int adjacentNode)
            {
                adjacentNodes.Remove(adjacentNode);
            }

            public List<int> adjacentNodes;
            public NodeObject nodeGameObject;
            public int myNum;
        }

        //Deletes a given node from the graph, all edges, and any nodes that become isolated as a result
        public void deleteNode(int id, bool removeNeighbors=true)
        {
            Destroy(graph[id].nodeGameObject.gameObject);
            graph[id] = null;

            for (int i = 0; i < edgeLines.Count; i++) //do this once
            {
                if (edgeLines[i].isConnectedToNode(id))
                {
                    Destroy(edgeLines[i].gameObject);
                    edgeLines.RemoveAt(i);
                    i = 0;
                }
            }

            for (int i = 0; i < nodeCountId; i++)
            {
                if (graph.ContainsKey(i) && graph[i] != null)
                {
                    graph[i].RemoveAdjacency(id);
                    if (graph[i].adjacentNodes.Count == 0 && removeNeighbors)
                        deleteNode(graph[i].myNum);
                }
            }

            for (int i = 0; i < edgeLines.Count; i++) //do it again for good measure (after the recursion)
            {
                if (edgeLines[i].isConnectedToNode(id))
                {
                    Destroy(edgeLines[i].gameObject);
                    edgeLines.RemoveAt(i);
                    i = 0;
                }
            }
        }

        //Gets the node with the nearest world position to a given position, within a certain distance
        public Node getNearestNodeWithinDistance(Vector3 pos, float dist, int[] excludeNode=null)
        {
            Transform tMin = null;
            float minDist = Mathf.Infinity;
            Vector3 currentPos = pos;
            int id = -1;
            for (int i = 0; i < nodeCountId; i++)
            {
                if (graph.ContainsKey(i) && graph[i] != null && (excludeNode != null && !excludeNode.Contains(i)))
                {
                    float distance = Vector3.Distance(graph[i].nodeGameObject.transform.position, currentPos);
                    if (distance < minDist)
                    {
                        tMin = graph[i].nodeGameObject.transform;
                        id = i;
                        minDist = distance;
                    }
                }
            }

            if (tMin != null && Vector3.Distance(tMin.position, pos) <= dist)
            {
                return graph[id];
            }
            else
                return null;
        }

        //The internal id of nodes, always incremented, deleted node ids not recovered
        private int nodeCountId = 0;
        public int getNodeCountId() { return nodeCountId; }

        //Activates a node
        public void activateNode(int nodeID, bool isAngle)
        {
            graph[nodeID].nodeGameObject.NodeActivated(nodeID, isAngle);

            Handler.NodeActivated.Invoke(nodeID, isAngle);
        }
        //Deactivates a node
        public void deactivateNode(int nodeID, bool isAngle) //note duplicate function below
        {
            if (graph[nodeID].nodeGameObject.isActivated(isAngle) && !graph[nodeID].nodeGameObject.isTouchingPlayer())
            {
                graph[nodeID].nodeGameObject.NodeDeactivated(nodeID, isAngle);

                Handler.NodeDeactivated.Invoke(nodeID, isAngle);
            }
            if (graph[nodeID].nodeGameObject.isTouchingPlayer())
            {
                activateNode(nodeID, isAngle);
            }
        }

        //Deactivates a node and all its surrounding nodes
        public void deactivateNodeAndSurroundingNodes(int nodeID, bool isAngle)
        {
            deactivateNode(nodeID, isAngle);
            foreach (int n in graph[nodeID].adjacentNodes)
            {
                deactivateNode(n, isAngle);
            }
        }

        //Takes a Segment that has been drawn to be added into the graph. Broken up into 4 main cases.
        public Tuple<NodeObject, NodeObject> AddSegment(Segment segment, NodeObject nodeObject, EdgeLine edgeLineObject)
        {
            Node nearestNodeStart = getNearestNodeWithinDistance(segment.start.gameObject.transform.position, 0);
            Node nearestNodeEnd = getNearestNodeWithinDistance(segment.end.gameObject.transform.position, 0);

            //if (Vector3.Distance(segment.start.transform.position, segment.end.transform.position) < NetworkHandler.snapDistance)
            //    return;

            if (nearestNodeStart == null && nearestNodeEnd == null) //case where both points on the new segment are not yet in the graph
            {

                NodeObject start = Instantiate(nodeObject);
                int startNum = nodeCountId;
                nodeCountId++;
                start.setPos(segment.start.transform.position);
                NodeObject end = Instantiate(nodeObject);
                int endNum = nodeCountId;
                nodeCountId++;
                end.setPos(segment.end.transform.position);

                Node startNode = new Node(startNum, start);
                Node endNode = new Node(endNum, end);

                graph[startNum] = startNode;
                graph[endNum] = endNode;

                startNode.AddAdjacency(endNum, this, edgeLineObject, true); //only create 1 edge line
                endNode.AddAdjacency(startNum, this, edgeLineObject, false);

                Debug.Log("case 1");

                return new Tuple<NodeObject, NodeObject>(startNode.nodeGameObject, endNode.nodeGameObject);
            }
            else if ((nearestNodeStart != null && nearestNodeEnd == null) || (nearestNodeStart != null && nearestNodeEnd != null)) //case where start is already in the graph, but end is new to the graph (now combined with case 4)
            {
                NodeObject end = Instantiate(nodeObject);
                int endNum = nodeCountId;
                nodeCountId++;
                end.setPos(segment.end.transform.position);

                Node endNode = new Node(endNum, end);

                graph[endNum] = endNode;

                endNode.AddAdjacency(nearestNodeStart.myNum, this, edgeLineObject, true);
                nearestNodeStart.AddAdjacency(endNode.myNum, this, edgeLineObject, false); //only create 1 edge line

                Debug.Log("case 2");

                return new Tuple<NodeObject, NodeObject>(null, endNode.nodeGameObject);
            }
            else if (nearestNodeStart == null && nearestNodeEnd != null) //case where start is not in the graph, but end is already in the graph
            {
                //NodeObject start = Instantiate(nodeObject);
                //int startNum = nodeCountId;
                //nodeCountId++;
                //start.setPos(segment.start.transform.position);

                //Node startNode = new Node(startNum, start);

                //graph[startNum] = startNode;

                //startNode.AddAdjacency(nearestNodeEnd.myNum, this, edgeLineObject, true);
                //nearestNodeEnd.AddAdjacency(startNode.myNum, this, edgeLineObject, false); //only create 1 edge line

                //Debug.Log("case 3");

                //return null;
            }
            else if (nearestNodeStart != null && nearestNodeEnd != null) //case where both start and end in the graph. just need to possibly add an edge.
            {
                //case 4 has been combined into case 2 for some reason it fixed bug

                //nearestNodeStart.AddAdjacency(nearestNodeEnd.myNum, this, edgeLineObject, true);
                //nearestNodeEnd.AddAdjacency(nearestNodeStart.myNum, this, edgeLineObject, false); //only create 1 edge line

                //Debug.Log("case 4");

                //return null;
            }
            return null;
        }
    }
}
