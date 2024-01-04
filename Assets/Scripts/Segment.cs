using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//A segment is for placing lines/nodes. It is sent to the network handler to be incorporated into the graph and is then deleted.
public class Segment : MonoBehaviour
{
    public GameObject start;
    public GameObject end;

    private LineRenderer lineRenderer;

    //Set start position of line
    public void setStart(Vector3 startPos)
    {
        transform.position = new Vector3(0, 0, 0);

        start.transform.position = startPos;
    }

    //Set end position of line
    public void setEnd(Vector3 endPos)
    {
        transform.position = new Vector3(0, 0, 0);

        end.transform.position = endPos;
    }

    //Set up the newly created segment
    public void Initialize()
    {
        start = transform.Find("Start").gameObject;
        end = transform.Find("End").gameObject;

        lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.startColor = Color.black;
        lineRenderer.endColor = Color.black;

        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
    }

    // Update is called once per frame
    void Update()
    {
        //draw line between points
        lineRenderer.SetPosition(0, start.transform.position);
        lineRenderer.SetPosition(1, end.transform.position);
    }
}
