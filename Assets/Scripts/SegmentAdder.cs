using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using System;

//Responsible for handling the adding of Segments
public class SegmentAdder : MonoBehaviour
{
    [SerializeField] public Segment segmentObject;

    private NetworkHandler networkHandler;

    private Segment newSegment; //segment currently being placed

    private Snap snap;

    bool newLineDrawMode = false;

    private void Start()
    {
        networkHandler = GetComponent<NetworkHandler>();
        snap = GetComponent<Snap>();
    }

    void Update()
    {
        if (networkHandler.editSetting == NetworkHandler.EditSetting.add)
        {

            //Mouse is pressed so start creating a new segment
            if (Input.GetMouseButton(0) && !newLineDrawMode && !EventSystem.current.IsPointerOverGameObject()) //start create new line
            {
                newLineDrawMode = true;
                newSegment = Instantiate(segmentObject);
                newSegment.Initialize();
                newSegment.setStart(getCoordsFromMouse(Input.mousePosition));
                newSegment.setEnd(getCoordsFromMouse(Input.mousePosition));

                //need to update this with new snapping features (see below)
                NetworkHandler.Graph.Node nearestNode = networkHandler.graph.getNearestNodeWithinDistance(newSegment.start.transform.position, NetworkHandler.snapDistance);
                if (nearestNode != null)
                {
                    newSegment.setStart(nearestNode.nodeGameObject.transform.position);
                    newSegment.setEnd(getCoordsFromMouse(Input.mousePosition));
                }

                networkHandler.oldEditSetting = NetworkHandler.EditSetting.add;
                networkHandler.editSetting = NetworkHandler.EditSetting.moveSpecificNode;
                networkHandler.ChangedEditSetting.Invoke();

                Tuple<NodeObject, NodeObject> segment = networkHandler.AddSegment(newSegment);

                //perhaps here get start as well (or do this inside networkhandler case 2) and then call one round of initialSnap from nodeobject's update script
                //just need to call initialSnap and then place in correct location
                if (segment.Item1 != null)
                {
                    Tuple<string, Vector3> s = snap.initialSnap(segment.Item1.gameObject, segment.Item1.transform.position, segment.Item1.myNum, segment.Item2.myNum);
                    if (s != null)
                    {
                        segment.Item1.transform.position = s.Item2;
                    }
                }
                if (segment.Item2 != null)
                {
                    networkHandler.specificMoveNode = segment.Item2.myNum;
                    segment.Item2.mouseDown = true;
                    //end.offset = end.gameObject.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0));
                    //Debug.Log("oofset: " + end.offset);
                    //end.offset = new Vector3(end.offset.x, end.offset.y, 0);
                }
                else
                    Debug.Log("shouldnt");
                Destroy(newSegment.gameObject);

            }

            //Move the other end of the node to the mouse position while being placed
            if (newLineDrawMode)
            {

                if (!Input.GetMouseButton(0))
                {
                    newLineDrawMode = false;
                }
            }

        }
    }

    //Converts mouse coords to world coords
    public static Vector3 getCoordsFromMouse(Vector3 mouseCoords)
    {
        Vector3 coords = Camera.main.ScreenToWorldPoint(mouseCoords);
        return new Vector3(coords.x, coords.y, 0);
    }
}