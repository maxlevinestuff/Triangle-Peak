using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public enum CameraMode { player, book, focusBook, focusPlayer}

    public CameraMode cameraMode = CameraMode.player;

    NetworkHandler networkHandler;

    PlayerCharacter playerCharacter;
    Question question;
    Camera cam;
    TraceLines traceLines;
    Transform flag;

    public Vector2 focusPointBook(int hint) //determines where the camera should focus when book opens, also when book opens via hint 2
    {
        Vector2 result;

        int count = 0;
        if (hint == 1)
        {
            Vector2 sum = new Vector2(0, 0);
            if (!question.freeMode)
            {
                sum += (Vector2)question.transform.position;
                count++;
            }

            result = sum / count;
        } else if (hint == 2)
        {
            TraceNode[] traceNodes = GameObject.FindObjectsOfType<TraceNode>();
            Vector2 sum = new Vector2(0, 0);
            foreach (TraceNode traceNode in traceNodes)
            {
                if (traceNode.isHint)
                {
                    sum += (Vector2)traceNode.transform.position;
                    count++;
                }
            }
            foreach (DrawLineBetweenTwoObjects line in traceLines.hintEdgeObjects)
            {
                sum += line.middlePos();
                count++;
            }
            result = sum / count;
        }
        else
        {
            Vector2 sum = new Vector2(0, 0); //maybe get rid of this and just use player camera below, see how works in levels
            TraceNode[] traceNodes = GameObject.FindObjectsOfType<TraceNode>();
            foreach (TraceNode traceNode in traceNodes)
            {
                sum += (Vector2)traceNode.transform.position;
                count++;
            }
            NodeObject[] nodeObjects = GameObject.FindObjectsOfType<NodeObject>();
            foreach (NodeObject nodeObject in nodeObjects)
            {
                sum += (Vector2)nodeObject.transform.position;
                count++;
            }
            Value[] values = GameObject.FindObjectsOfType<Value>();
            foreach (Value value in values)
            {
                if (value.transform.parent == null)
                {
                    sum += (Vector2)value.transform.position;
                    count++;
                }
            }
            if (!question.freeMode && question.GetComponent<MeshRenderer>().enabled)
            {
                sum += (Vector2)question.transform.position;
                count++;
            }

            result = sum / count;
        }

        if (count == 0 || question.freeMode) //just do this as simpler, maybe for everywhere
        {
            result = playerCamera();
        }

        //Vector2 viewToWorld = (Vector2)cam.ViewportToWorldPoint(new Vector3(.25f, 0f, cam.nearClipPlane)); //shift over to make room for the sheets
        //result += new Vector2(-viewToWorld.y, 0);
        result += new Vector2(3f, -.5f);

        return result;
    }
    Vector2 focus;

    float edges = 2f;
    public Vector2 playerCamera()
    {
        if (question.freeMode)
            return playerCharacter.transform.position;

        //Vector3 average then get min/max distance so player is always on

        Vector2 camPos = (Vector2)transform.position;

        Vector2 average = (((Vector2)playerCharacter.transform.position * 2) + (Vector2)question.transform.position + (Vector2)flag.position) / 4; //player character weighted twice to give more room, could adjust this

        float height = 2f * cam.orthographicSize;
        float width = height * cam.aspect;

        if (average.x + (width / 2) < (playerCharacter.transform.position.x + 2f))
            average = new Vector2((playerCharacter.transform.position.x + 2f) - (width / 2), average.y);

        if (average.x - (width / 2) > (playerCharacter.transform.position.x - 2f))
            average = new Vector2((playerCharacter.transform.position.x - 2f) + (width / 2), average.y);

        if (average.y + (height / 2) < (playerCharacter.transform.position.y + 2f))
            average = new Vector2(average.x, (playerCharacter.transform.position.y + 2f) - (height / 2));

        if (average.y - (height / 2) > (playerCharacter.transform.position.y - 2f))
            average = new Vector2(average.x, (playerCharacter.transform.position.y - 2f) + (height / 2));

        return average;
    }

    // Start is called before the first frame update
    void Start()
    {
        networkHandler = GameObject.Find("Control").GetComponent<NetworkHandler>();
        playerCharacter = GameObject.Find("PlayerCharacter").GetComponent<PlayerCharacter>();
        question = GameObject.Find("Question").GetComponent<Question>();
        cam = GetComponent<Camera>();
        traceLines = GameObject.Find("TraceLines").GetComponent<TraceLines>();
        if (! question.freeMode)
            flag = GameObject.Find("Flag").transform;
    }

    public void ChangedEditSetting(int hint)
    {
        if (cameraMode != CameraMode.focusBook && cameraMode != CameraMode.focusPlayer)
        {
            if (networkHandler.editSetting == NetworkHandler.EditSetting.book)
            {
                focus = focusPointBook(hint);

                if (new Vector2(transform.position.x, transform.position.y) != focus)
                {
                    lerpPercent = 0f;
                    cameraMode = CameraMode.focusBook;
                }
                else
                {
                    lerpPercent = 0f;
                    cameraMode = CameraMode.book;
                }
            }
            else
            {
                if (new Vector2(transform.position.x, transform.position.y) != playerCamera())
                {
                    lerpPercent = 0f;
                    cameraMode = CameraMode.focusPlayer;
                }
                else
                {
                    lerpPercent = 0f;
                    cameraMode = CameraMode.player;
                }
            }
        } else
        {
            if (cameraMode == CameraMode.focusBook && networkHandler.editSetting != NetworkHandler.EditSetting.book)
            {
                if (new Vector2(transform.position.x, transform.position.y) != playerCamera())
                {
                    lerpPercent = 0f;
                    cameraMode = CameraMode.focusPlayer;
                }
                else
                {
                    lerpPercent = 0f;
                    cameraMode = CameraMode.player;
                }
            } else if (cameraMode == CameraMode.focusPlayer && networkHandler.editSetting == NetworkHandler.EditSetting.book)
            {
                focus = focusPointBook(hint);

                if (new Vector2(transform.position.x, transform.position.y) != focus)
                {
                    lerpPercent = 0f;
                    cameraMode = CameraMode.focusBook;
                }
                else
                {
                    lerpPercent = 0f;
                    cameraMode = CameraMode.book;
                }
            } else
            {
                if (cameraMode == CameraMode.focusBook && networkHandler.editSetting == NetworkHandler.EditSetting.book)
                {
                    lerpPercent = 0f;
                    focus = focusPointBook(hint);
                }
            }
        }
    }

    float lerpPercent = 0f;

    // Update is called once per frame
    void LateUpdate()
    {
        lerpPercent += Time.deltaTime * 10f;
        if (lerpPercent > 100f) lerpPercent = 100f;

        switch(cameraMode)
        {
            case CameraMode.player:
                transform.position = playerCamera();
                break;
            case CameraMode.book:
                transform.position += new Vector3(Input.GetAxisRaw("Horizontal") / 10, Input.GetAxisRaw("Vertical") / 10, 0);
                break;
            case CameraMode.focusBook:
                transform.position = Vector2.Lerp(transform.position, focus, lerpPercent); //need to use lerp correctly by incrementing to 100%
                if (new Vector2(transform.position.x, transform.position.y) == focus)
                    cameraMode = CameraMode.book;
                break;
            case CameraMode.focusPlayer:
                transform.position = Vector2.Lerp(transform.position, playerCamera(), lerpPercent);
                if (new Vector2(transform.position.x, transform.position.y) == playerCamera())
                    cameraMode = CameraMode.player;
                break;
        }
        transform.position = ConstrainZ(transform.position);
        
    }

    static Vector3 ConstrainZ(Vector3 v)
    {
        return new Vector3(v.x, v.y, -10);
    }
}
