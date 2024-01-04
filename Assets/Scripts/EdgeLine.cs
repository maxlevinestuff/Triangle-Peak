using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//A game object with a line renderer for rendering edges
public class EdgeLine : MonoBehaviour
{
    public DrawLineBetweenTwoObjects drawLine;

    public NodeObject start;
    public NodeObject end;

    Value lengthDisplay;

    public Material[] materials;

    private NetworkHandler networkHandler;

    //Set up the new edge line
    public void Initialize(int start, int end)
    {
        networkHandler = GameObject.Find("Control").GetComponent<NetworkHandler>();

        drawLine = GetComponent<DrawLineBetweenTwoObjects>();
        drawLine.Initialize(networkHandler.getNodeFromGraph(start).nodeGameObject.transform, networkHandler.getNodeFromGraph(end).nodeGameObject.transform, .14f, .14f);

        this.start = networkHandler.getNodeFromGraph(start).nodeGameObject;
        this.end = networkHandler.getNodeFromGraph(end).nodeGameObject;

        networkHandler.NodeActivated.AddListener(NodeActivated);
        networkHandler.NodeDeactivated.AddListener(NodeDeactivated);
    }

    //Is the edge connected to a node on either side?
    public bool isConnectedToNode(int id)
    {
        return start.myNum == id || end.myNum == id;
    }

    //// Update is called once per frame
    //void Update()
    //{
    //    lineRenderer.SetPosition(0, start.transform.position);
    //    lineRenderer.SetPosition(1, end.transform.position);
    //}

    private Value createLengthDisplay()
    {
        //create distance value display
        float distance = Vector3.Distance(start.transform.position, end.transform.position);
        Vector3 position = (start.transform.position + end.transform.position) / 2;
        //position.z = -1;
        Value display = Instantiate(networkHandler.valueObject);
        display.Initialize(distance, Value.Unit.Meters, position, null);
        return display;
    }

    //Event called: if node attached to this is activated, and when both nodes are activated, then get the length and display it
    public void NodeActivated(int id, bool isAngle)
    {
        if (! isAngle)
        {
            if (networkHandler.graph.graph[start.myNum] == null || networkHandler.graph.graph[end.myNum] == null) return; //added to fix bug

            if ((id == start.myNum || id == end.myNum) && networkHandler.graph.graph[start.myNum].nodeGameObject.isActivated(false) && networkHandler.graph.graph[end.myNum].nodeGameObject.isActivated(false))
            {
                //lineRenderer.startColor = Color.green;
                //lineRenderer.endColor = Color.green;

                drawLine.CreateLineRenderer(materials[0]);

                //create distance value display
                Value newDisplay = createLengthDisplay();
                if (lengthDisplay == null || ! Value.IsValueTheSame(lengthDisplay, newDisplay))
                {
                    if (lengthDisplay != null)
                        Destroy(lengthDisplay.gameObject);
                    lengthDisplay = newDisplay;
                } else
                {
                    Destroy(newDisplay.gameObject);
                }
            }
        }
    }

    //Event called: if node attached to this is deactivated, and either node is deactivated, delete length
    void NodeDeactivated(int id, bool isAngle)
    {
        if (!isAngle)
        {
            if (networkHandler.graph.graph[start.myNum] == null || networkHandler.graph.graph[end.myNum] == null) return; //added to fix bug

            if ((id == start.myNum || id == end.myNum) && (!networkHandler.graph.graph[start.myNum].nodeGameObject.isActivated(false) || !networkHandler.graph.graph[end.myNum].nodeGameObject.isActivated(false)))
            {
                //lineRenderer.startColor = Color.black;
                //lineRenderer.endColor = Color.black;

                drawLine.CreateLineRenderer(materials[1]);

                //destroy distance value display
                if (lengthDisplay != null)
                    Destroy(lengthDisplay.gameObject);
            }
        }
    }

    private void OnDestroy()
    {
        //destroy distance value display
        if (lengthDisplay != null)
            Destroy(lengthDisplay.gameObject);
    }
}
