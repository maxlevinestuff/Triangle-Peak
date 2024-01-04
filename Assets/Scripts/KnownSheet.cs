using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnownSheet : MonoBehaviour
{
    private int knownChildCount = -1;

    private Snap snap;
    private GameControl gameControl;

    // Start is called before the first frame update
    void Start()
    {
        snap = GameObject.Find("Control").GetComponent<Snap>();
        gameControl = GameObject.Find("Control").GetComponent<GameControl>();

        //90 and 180 degrees always start out known
        Value display90 = Instantiate(gameControl.valueObject);
        display90.Initialize(90f, Value.Unit.Degrees, transform.position + new Vector3(-2.8f, 1.4f, 0), null, true);
        display90.SetOnPaper(transform);

        Value display180 = Instantiate(gameControl.valueObject);
        display180.Initialize(180f, Value.Unit.Degrees, transform.position + new Vector3(-2f, 1.4f, 0), null, true);
        display180.SetOnPaper(transform);
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.childCount != knownChildCount)
        {
            knownChildCount = transform.childCount;
            Value[] values = transform.GetComponentsInChildren<Value>();

            snap.knownLengths = new List<float>();
            snap.knownAngles = new List<float>();

            foreach (Value value in values)
            {
                if (value.unit == Value.Unit.Meters)
                    snap.knownLengths.Add(value.value);
                else if (value.unit == Value.Unit.Degrees)
                    snap.knownAngles.Add(value.value);
            }
        }
    }
}
