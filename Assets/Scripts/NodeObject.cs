using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

//This is the node game object, primarily representing its node's position in world space
public class NodeObject : MonoBehaviour
{
    public int myNum;

    public bool isSnapping;
    Vector3 snappingPoint;
    string snapId;

    private NetworkHandler networkHandler;
    private Snap snap;

    public List<Value> anglesDisplay;

    public bool mouseNeverReleased = true;

    public NodeLine nodeLine;

    public void setPos(Vector3 pos)
    {
        transform.position = pos;
    }

    //for circle
    void Awake()
    {
        snap = GameObject.Find("Control").GetComponent<Snap>();
        networkHandler = GameObject.Find("Control").GetComponent<NetworkHandler>();
        nodeLine = GetComponent<NodeLine>();
    }

    // Start is called before the first frame update
    void Start()
    {
        networkHandler.NodeActivated.AddListener(NodeActivated);
        networkHandler.NodeDeactivated.AddListener(NodeDeactivated);

        anglesDisplay = null;

        networkHandler.ChangedEditSetting.AddListener(ChangedEditSetting);
    }

    public Vector3 offset;

    public bool mouseDown;

    //Start moving or delete node when clicked on
    void OnMouseDown()
    {
        if (! mouseDown && ! mouseNeverReleased)
            snap.MakeNewSelfSnap(myNum);
        mouseDown = true;

        if (networkHandler.editSetting == NetworkHandler.EditSetting.move)
        {
            offset = gameObject.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0));
            offset = new Vector3(offset.x, offset.y, 0);

            Debug.Log("offset here is " + offset);

            networkHandler.oldEditSetting = NetworkHandler.EditSetting.move;
            networkHandler.editSetting = NetworkHandler.EditSetting.moveSpecificNode;
            networkHandler.specificMoveNode = myNum;
            networkHandler.ChangedEditSetting.Invoke();
        }

        if (networkHandler.editSetting == NetworkHandler.EditSetting.delete)
        {
            networkHandler.graph.deactivateNodeAndSurroundingNodes(myNum, true); //if they are selected by player on them, wont delete

            if (anglesDisplay != null) //so just delete them here
            {
                foreach (Value v in anglesDisplay)
                {
                    Destroy(v.gameObject);
                }
                anglesDisplay = null;
            }
            while (transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }

            List<int> nodesToReactivate = new List<int>(); //store nodes to re calculate angles for for after the recursion //added later
            for (int i = 0; i < networkHandler.graph.graph[myNum].adjacentNodes.Count; i++)
            {
                if (networkHandler.graph.graph[networkHandler.graph.graph[myNum].adjacentNodes[i]].nodeGameObject.isActivated(true))
                {
                    nodesToReactivate.Add(networkHandler.graph.graph[myNum].adjacentNodes[i]);
                }
            }
            networkHandler.graph.deleteNode(myNum);
            foreach (int i in nodesToReactivate)
            {
                networkHandler.NodeActivated.Invoke(i, true); //re calculate all the angles for the connected nodes //added later
            }
        }
    }

    private void OnMouseUp()
    {
        //mouseDown = false;

        //if (networkHandler.editSetting == NetworkHandler.EditSetting.moveSpecificNode && networkHandler.specificMoveNode == myNum)
        //{
        //    Debug.Log("place 1");
        //    //this is where a node is released after being dragged (see just below)
        //    networkHandler.editSetting = networkHandler.oldEditSetting;
        //    networkHandler.specificMoveNode = -1;
        //}
    }

    private void ChangedEditSetting()
    {
        //if (!Input.GetMouseButton(0)) //this used to be uncommented but seems to be find most of the time
        //{
        //    mouseDown = false;
        //    if (networkHandler.oldEditSetting == NetworkHandler.EditSetting.moveSpecificNode)
        //    {
        //        //this is where a node is released after being created OR dragged (see just above)
        //        networkHandler.specificMoveNode = -1;
        //        snappingMode = SnappingMode.none;
        //        if (networkHandler.graph.graph[myNum].adjacentNodes.Count == 0)
        //            networkHandler.graph.deleteNode(myNum);
        //    }
        //}
    }

    public bool isIntermStillSnapping(string snapString)
    {
        switch (snapString[0])
        {
            case 'r':
                return Int32.Parse(snapString.Remove(0, 1)) == myNum;
            default:
                return snapString == snapId;
        }
    }

    //Move node when dragged
    void Update()
    {
        collisons();

        if (mouseDown)
        {
            if (networkHandler.editSetting == NetworkHandler.EditSetting.move || (networkHandler.editSetting == NetworkHandler.EditSetting.moveSpecificNode && networkHandler.specificMoveNode == myNum))
            {

                Vector3 initialLoc = transform.position;
                string initialSnapId = snapId;

                retry_snap:

                Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
                Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;
                curPosition.z = 0;
                transform.position = curPosition;

                bool dontDeleteSelfSnapLine = false; //dontdeleteselfsnapline not needed if using the goto

                Tuple<string, Vector3> s = null;

                if (! isSnapping)
                    s = snap.initialSnap(this.gameObject, curPosition, myNum);

                if (s != null && ! isSnapping)
                {
                    snappingPoint = s.Item2;
                    isSnapping = true;
                    snapId = s.Item1;
                    dontDeleteSelfSnapLine = true; //unless new position not within snapping dist of self snap line
                    Debug.Log("initial id: " + snapId);
                } else if (s != null)
                {
                    transform.position = snappingPoint;
                }

                if (isSnapping)
                {

                    transform.position = snappingPoint;
                    Tuple<string, Vector3> mouse_s = snap.intermSnap(curPosition,myNum,snapId);
                    if (mouse_s != null)
                    {
                        transform.position = mouse_s.Item2;
                        Debug.Log("interm id: " + mouse_s.Item1);
                    }
                    Debug.Log("got");

                    if (mouse_s == null || ! isIntermStillSnapping(mouse_s.Item1))
                    {
                        snap.RestoreAdjacencies(snappingPoint, myNum);
                        Debug.Log("AEFSadsfds");
                        //transform.position = curPosition;
                        isSnapping = false;
                        snapId = null;
                        //add goto here?
                        //goto retry_snap;
                    }
                }

                if (s != null)
                    Debug.Log("snapping to " + s.Item1[0]);

                Debug.Log("just point snapped: " + snap.justPointSnapped);

                if (initialLoc != transform.position && snap.justPointSnapped != myNum)
                {
                    if ((initialSnapId == null && snapId == null) || (!dontDeleteSelfSnapLine && initialSnapId != null && snapId != null && initialSnapId.Substring(0, 2) == "ol" && snapId.Substring(0, 2) == "ol" && initialSnapId != snapId))
                    { //using goto, this can be changed to if ((snapId == null) || (initialSnapId != snapId)). need to add to id strings which edge lines produced the point, to be able to check here
                        //then check, for each self snap line, if still on valid point. for multiple self snap lines, just check if within distance to each
                        networkHandler.graph.deactivateNodeAndSurroundingNodes(myNum, true); //just dont deactivate here to implement 180 snap angle
                        snap.selfSnapLines = new List<Tuple<string, Ray>>();
                    }
                    else
                        networkHandler.graph.deactivateNode(myNum, true);
                    networkHandler.graph.deactivateNode(myNum, false);
                }
                else if (snap.justPointSnapped == myNum) //may need to remove conditional and just do else, at performance cost
                {
                    if (lengthActivated) networkHandler.graph.activateNode(myNum, false);
                    if (angleActivated) networkHandler.graph.activateNode(myNum, true);
                    Debug.Log("Oh no!");
                }

                //for keeping adjacent angles current
                if (initialLoc != transform.position)
                {
                    foreach (int n in networkHandler.graph.graph[myNum].adjacentNodes)
                    {
                        if (networkHandler.graph.graph[n].nodeGameObject.angleActivated) networkHandler.graph.graph[n].nodeGameObject.NodeActivated(n, true);
                    }
                }

                if (!Input.GetMouseButton(0)) //this used to be all the way outside brackets on update
                {
                    mouseDown = false;
                    if (networkHandler.editSetting == NetworkHandler.EditSetting.moveSpecificNode && networkHandler.specificMoveNode == myNum)
                    {
                        //this is where a node is released after being created OR dragged (see just above)
                        networkHandler.editSetting = networkHandler.oldEditSetting;
                        networkHandler.oldEditSetting = NetworkHandler.EditSetting.moveSpecificNode;
                        networkHandler.specificMoveNode = -1;
                        isSnapping = false;

                        snap.imaginaryEdgeLine = null;
                        snap.justPointSnapped = -1;
                        snap.dontDeactivate = null;

                        mouseNeverReleased = false;

                        offset = new Vector3(0, 0, 0);
                        networkHandler.ChangedEditSetting.Invoke();
                        if (networkHandler.graph.graph[myNum].adjacentNodes.Count == 0)
                            networkHandler.graph.deleteNode(myNum);
                    }
                }

                //if (!Input.GetMouseButton(0))
                //{
                //    mouseDown = false;
                //    if (networkHandler.editSetting == NetworkHandler.EditSetting.moveSpecificNode && networkHandler.specificMoveNode == myNum)
                //    {
                //        //this is where a node is released after being created OR dragged (see just above)
                //        networkHandler.editSetting = networkHandler.oldEditSetting;
                //        networkHandler.specificMoveNode = -1;
                //        snappingMode = SnappingMode.none;
                //        if (networkHandler.graph.graph[myNum].adjacentNodes.Count == 0)
                //            networkHandler.graph.deleteNode(myNum);
                //    }
                //}
            }
        }

        if (!Input.GetMouseButton(0))
        {
            mouseNeverReleased = false;
        }

    }

    private void collisons()
    {
        if (isInside)
        {
            if (networkHandler.isOutsidePlayerColliderRadius(transform.position))
            {
                isInside = false;
            }
        } else
        {
            if (networkHandler.isInsidePlayerColliderRadius(transform.position))
            {
                isInside = true;

                if (snap.wasAngleActive != null) snap.wasAngleActive[myNum] = true; //only way player can change activation during snap, so add to the backup here
                if (snap.wasLengthActive != null) snap.wasLengthActive[myNum] = true;

                networkHandler.graph.activateNode(myNum, false);
                networkHandler.graph.activateNode(myNum, true);

                foreach (int n in networkHandler.graph.graph[myNum].adjacentNodes)
                {
                    if (networkHandler.graph.graph[n].nodeGameObject.mouseDown && mouseNeverReleased)
                        snap.MakeNewSelfSnap(n);
                }
            }
        }
    }

    private bool isInside = false; //keep track of if this node is touching player

    public bool isTouchingPlayer()
    {
        return networkHandler.isInsidePlayerColliderRadius(transform.position);
    }

    ////Activate this node if player touches
    //void OnTriggerEnter2D(Collider2D col)
    //{
    //    if (col.gameObject.tag == "Player")
    //    {
    //        isTouchingPlayer = true;

    //        if (snap.wasAngleActive != null) snap.wasAngleActive[myNum] = true; //only way player can change activation during snap, so add to the backup here
    //        if (snap.wasLengthActive != null) snap.wasLengthActive[myNum] = true;

    //        networkHandler.graph.activateNode(myNum, false);
    //        networkHandler.graph.activateNode(myNum, true);
    //    }
    //}

    ////Keep track of if player no longer touching
    //void OnTriggerExit2D(Collider2D col)
    //{
    //    if (col.gameObject.tag == "Player")
    //    {
    //        isTouchingPlayer = false;
    //    }
    //}

    public bool angleActivated = false;
    public bool lengthActivated = false;

    public bool isActivated(bool isAngle)
    {
        if (isAngle)
            return angleActivated;
        else
            return lengthActivated;
    }

    //Called for event: if this node is activated, gets the new angles
    public void NodeActivated(int id, bool isAngle)
    {
        if (myNum == id)
        {
            if (isAngle)
            {
                angleActivated = true;
                Debug.Log("MADE ACTIVE");
                //if (anglesDisplay != null)
                //{
                //    foreach (Value value in anglesDisplay)
                //    {
                //        Destroy(value.gameObject);
                //    }
                //}
                //anglesDisplay = AngleCalculator.GetAngles(networkHandler.graph, networkHandler.graph.graph[myNum], networkHandler.valueObject);

                //create distance value display

                List<Value> newDisplay = AngleCalculator.GetAngles(networkHandler.graph, networkHandler.graph.graph[myNum], networkHandler.valueObject);
                foreach (Value v in newDisplay)
                {
                    v.transform.SetParent(null); //set to null so wont get deleted later, then placed back
                }
                if (anglesDisplay == null || !Value.AreValuesTheSame(anglesDisplay, newDisplay))
                {
                    if (anglesDisplay != null)
                    {
                        foreach (Value value in anglesDisplay)
                        {
                            Destroy(value.gameObject);
                        }
                        anglesDisplay = null;
                    }
                    while (transform.childCount > 0)
                    {
                        DestroyImmediate(transform.GetChild(0).gameObject);
                    }
                    anglesDisplay = newDisplay;
                    foreach (Value v in newDisplay)
                    {
                        v.transform.SetParent(this.transform);
                    }
                }
                else
                {
                    foreach (Value value in newDisplay)
                    {
                        Destroy(value.gameObject);
                    }
                    newDisplay = null;
                }

            } else
            {
                lengthActivated = true;
            }

            nodeLine.SetColor();
        }
    }

    //Called for event: if this node is deactivated, deletes the angles
    public void NodeDeactivated(int id, bool isAngle)
    {
        if (myNum == id && networkHandler.isOutsidePlayerColliderRadius(transform.position))
        {
            if (snap.imaginaryEdgeLine == null || (snap.imaginaryEdgeLine.Item1 != myNum && snap.imaginaryEdgeLine.Item2 != myNum)) //dont deactivate if are the invisible line snapping
            {
                if (snap.dontDeactivate == null || !snap.dontDeactivate.Contains(myNum))
                {
                    if (isAngle)
                    {

                        angleActivated = false;
                        //spriteRenderer.sprite = nodeSprites[0];

                        //destroy angle values display
                        if (anglesDisplay != null)
                        {
                            //destroy angle values display
                            foreach (Value value in anglesDisplay)
                            {
                                if (value != null)
                                    Destroy(value.gameObject);
                            }
                        }
                        while (transform.childCount > 0)
                        {
                            DestroyImmediate(transform.GetChild(0).gameObject);
                        }
                        anglesDisplay = null;
                    }
                    else
                    {
                        lengthActivated = false;
                    }

                    nodeLine.SetColor();
                }

            }
        }
    }

    private void OnDestroy()
    {
        if (anglesDisplay != null)
        {
            //destroy angle values display
            foreach (Value value in anglesDisplay)
            {
                if (value != null)
                    Destroy(value.gameObject);
            }
            anglesDisplay = null;
        }
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }

}
