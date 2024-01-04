using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TraceNode : MonoBehaviour
{
    //for circle
    float theta_scale = 0.01f;        //Set lower to add more points
    int size; //Total number of points in circle

    float mainRadius;
    LineRenderer mainLineRenderer;

    public int nodeId;
    public int[] adjacent;
    public bool isHint;

    public void Initialize(int nodeId, int[] adjacent, bool isHint)
    {
        this.nodeId = nodeId;
        this.adjacent = adjacent;
        this.isHint = isHint;

        mainLineRenderer = GetComponent<LineRenderer>();
        mainLineRenderer.positionCount = size;

        if (!isHint)
        {
            mainRadius = .05f;
            mainLineRenderer.startWidth = 0.185f; //42/100*.5 / 2 --> width of dotted line material pixels / material pixels per unit * edge line line thickness / 2?
            mainLineRenderer.endWidth = 0.185f;
        } else
        {
            mainRadius = .025f;
            mainLineRenderer.startWidth = 0.16f; //42/100*.5 / 2 --> width of dotted line material pixels / material pixels per unit * edge line line thickness / 2?
            mainLineRenderer.endWidth = 0.16f;
        }
    }

    private void Awake()
    {

        float sizeValue = (2.0f * Mathf.PI) / theta_scale;
        size = (int)sizeValue;
        size++;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //for circle
        Vector3 pos;
        float theta = 0f;
        for (int i = 0; i < size; i++)
        {
            theta += (2.0f * Mathf.PI * theta_scale);
            float x = mainRadius * Mathf.Cos(theta);
            float y = mainRadius * Mathf.Sin(theta);
            x += gameObject.transform.position.x;
            y += gameObject.transform.position.y;
            pos = new Vector3(x, y, 0);
            mainLineRenderer.SetPosition(i, pos);
        }
    }
}
