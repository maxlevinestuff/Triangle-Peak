using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeLine : MonoBehaviour
{
    NodeObject nodeObject;

    //for circle
    float theta_scale = 0.01f;        //Set lower to add more points
    int size; //Total number of points in circle

    float mainRadius = .09f;
    LineRenderer mainLineRenderer;

    float distRadius = .05f;
    LineRenderer disLineRenderer;

    float angleRadius = .19f;
    LineRenderer angleLineRenderer;

    private void Awake()
    {
        nodeObject = GetComponent<NodeObject>();

        float sizeValue = (2.0f * Mathf.PI) / theta_scale;
        size = (int)sizeValue;
        size++;

        mainLineRenderer = GetComponent<LineRenderer>();
        mainLineRenderer.startWidth = 0.105f; //42/100*.5 / 2 --> width of dotted line material pixels / material pixels per unit * edge line line thickness / 2?
        mainLineRenderer.endWidth = 0.105f;
        mainLineRenderer.positionCount = size;

        disLineRenderer = transform.Find("LengthLineRenderer").GetComponent<LineRenderer>();
        disLineRenderer.startWidth = 0.105f;
        disLineRenderer.endWidth = 0.105f;
        disLineRenderer.positionCount = 2;

        angleLineRenderer = transform.Find("AngleLineRenderer").GetComponent<LineRenderer>();
        angleLineRenderer.startWidth = 0.025f;
        angleLineRenderer.endWidth = 0.025f;
        angleLineRenderer.positionCount = size;

        disLineRenderer.transform.SetParent(null); //because child elements are all values there to be deleted. sorry organized project hierarchy!

        angleLineRenderer.transform.SetParent(null); //because child elements are all values there to be deleted. sorry organized project hierarchy!

        SetColor();
    }

    private void OnDestroy()
    {
        if (angleLineRenderer != null)
            Destroy(angleLineRenderer.gameObject);
        if (disLineRenderer != null)
            Destroy(disLineRenderer.gameObject);
    }

    public void SetColor()
    {
        //if (nodeObject.angleActivated && nodeObject.lengthActivated)
        //{
        //    mainLineRenderer.startColor = Color.magenta;
        //    mainLineRenderer.endColor = Color.magenta;
        //}
        //else if (nodeObject.angleActivated)
        //{
        //    mainLineRenderer.startColor = Color.red;
        //    mainLineRenderer.endColor = Color.red;
        //}
        //else if (nodeObject.lengthActivated)
        //{
        //    mainLineRenderer.startColor = Color.green;
        //    mainLineRenderer.endColor = Color.green;
        //}
        //else
        //{
        //    mainLineRenderer.startColor = Color.black;
        //    mainLineRenderer.endColor = Color.black;
        //}

        if (nodeObject.angleActivated)
            angleLineRenderer.enabled = true;
        else
            angleLineRenderer.enabled = false;

        if (nodeObject.lengthActivated)
            disLineRenderer.enabled = true;
        else
            disLineRenderer.enabled = false;
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

            x = angleRadius * Mathf.Cos(theta);
            y = angleRadius * Mathf.Sin(theta);
            x += gameObject.transform.position.x;
            y += gameObject.transform.position.y;
            pos = new Vector3(x, y, 0);
            angleLineRenderer.SetPosition(i, pos);
        }

        for (int i = 0; i < 2; i++)
        {
            theta += (2.0f * Mathf.PI * .5f);
            float x = mainRadius * Mathf.Cos(theta);
            float y = mainRadius * Mathf.Sin(theta);
            x += gameObject.transform.position.x;
            y += gameObject.transform.position.y;
            pos = new Vector3(x, y, 0);
            disLineRenderer.SetPosition(i, pos);
        }
    }
}
