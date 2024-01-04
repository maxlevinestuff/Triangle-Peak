using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.SceneManagement;

public class GameControl : MonoBehaviour
{
    public List<EquationPiece> piecesOverlapping = new List<EquationPiece>();
    public Value currentDragged;

    public float foregroundAlpha = 0f;

    public Value valueObject;

    public NetworkHandler networkHandler;

    public SpriteRenderer foregroundSpriteRenderer;
    public SpriteRenderer backgroundSpriteRenderer;

    CameraControl cameraControl;

    [SerializeField] public LayerMask inputLayerMask;

    int totalHints;
    int[] hintsLeft = new int[] { 0, 0 };
    int hintsLeftCount()
    {
        return (1 - hintsLeft[0]) + (1 - hintsLeft[1]);
    }

    public void Hint()
    {
        if (hintsLeft[0] == 0)
        {
            hintsLeft[0] = 1;

            networkHandler.SetEditSetting("book");

            StartCoroutine(showHighlights());

        } else if (hintsLeft[1] == 0)
        {
            hintsLeft[1] = 1;

            GameObject.Find("TraceLines").GetComponent<TraceLines>().AddHint();
            networkHandler.SetEditSetting("book", false);
            cameraControl.ChangedEditSetting(2);
        }

        GameObject.Find("HintButton").transform.Find("Text").GetComponent<Text>().text = "Hint " + (totalHints - hintsLeftCount() + 1);

        if (hintsLeftCount() == 0)
        {
            Destroy(GameObject.Find("HintButton").gameObject);
        }
    }

    public void X()
    {
        SceneManager.LoadScene("Title");
    }

    Question question;
    TraceLines traceLines;

    void DealWithHintButton()
    {
        if (question.solutionEquationIds.Count() == 0)
        {
            hintsLeft[0] = 1;
        }
        if (traceLines == null || (traceLines.hintNodes.Count() == 0 && traceLines.hintEdges.Count() == 0))
        {
            hintsLeft[1] = 1;
        }
        totalHints = hintsLeftCount();

        GameObject.Find("HintButton").transform.Find("Text").GetComponent<Text>().text = "Hint " + (totalHints - hintsLeftCount() + 1);

        if (hintsLeftCount() == 0)
            Destroy(GameObject.Find("HintButton").gameObject);
    }

    public bool bookInPlace()
    {
        return networkHandler.editSetting == NetworkHandler.EditSetting.book && Vector3.Distance(shouldGo.Item1,
            new Vector3(equationSheet.transform.position.x, equationSheet.transform.position.y, 9)) <= 1f;
    }

    IEnumerator showHighlights()
    {
        yield return new WaitForSeconds(0.09f);

        while (!bookInPlace())
            yield return null;

        Equation[] equations = GameObject.FindObjectsOfType<Equation>();
        System.Array.Reverse(equations);

        foreach (Equation e in equations)
        {
            if (e.isSolution)
            {
                GameObject mask = e.transform.Find("highlight mask").gameObject;

                while (mask.transform.localPosition.x <= 5.725f)
                {
                    yield return null;
                    mask.transform.localPosition = new Vector3(mask.transform.localPosition.x + Time.deltaTime * 12f, mask.transform.localPosition.y, mask.transform.localPosition.z);
                }

                Destroy(mask);
            }
        }
    }

    private void Awake()
    {
        Camera.main.eventMask = inputLayerMask; //set which layers interact with mouse, see inspector window for selected layers

        networkHandler = GameObject.Find("Control").GetComponent<NetworkHandler>();
        cameraControl = GameObject.Find("Main Camera").GetComponent<CameraControl>();
    }

    // Start is called before the first frame update
    void Start()
    {
        question = GameObject.Find("Question").GetComponent<Question>();
        try
        {
            traceLines = GameObject.Find("TraceLines").GetComponent<TraceLines>();
        }
        catch (NullReferenceException n) { }

        equationSheet = GameObject.Find("EquationSheet");
        knownSheet = GameObject.Find("KnownSheet");

        DealWithHintButton();

        StartCoroutine(positionSheets());

        equationSheet.transform.position = shouldGo.Item1;
        knownSheet.transform.position = shouldGo.Item2;
        equationSheet.transform.localEulerAngles = shouldGo.Item3;
        knownSheet.transform.localEulerAngles = shouldGo.Item4;
        repositionZ();

        //foregroundSpriteRenderer = GameObject.Find("graphForeground").GetComponent<SpriteRenderer>();
        //backgroundSpriteRenderer = GameObject.Find("graphBackground").GetComponent<SpriteRenderer>();

        //StartCoroutine(shift());
    }

    GameObject equationSheet;
    GameObject knownSheet;

    Tuple<Vector3, Vector3, Vector3, Vector3> shouldGo;

    Tuple<Vector3, Vector3, Vector3, Vector3> whereSheetsShouldGo(NetworkHandler.EditSetting setting)
    {
        Camera cam = Camera.main;
        //float height = 2f * cam.orthographicSize;
        //float width = height * cam.aspect;


        //float width = cam.aspect * 2f * cam.orthographicSize;
        //float height = 2f * cam.orthographicSize;

        Vector3 equationExtents = equationSheet.GetComponent<SpriteRenderer>().bounds.extents;
        Vector3 knownExtents = knownSheet.GetComponent<SpriteRenderer>().bounds.extents;

        Vector3 equationPos;
        Vector3 knownPos;

        Vector3 equationRot;
        Vector3 knownRot;

        if (setting == NetworkHandler.EditSetting.book)
        {
            equationPos = cam.ViewportToWorldPoint(new Vector3(1, .5f, cam.nearClipPlane));
            equationPos = new Vector3(equationPos.x - equationExtents.x + 2, equationPos.y - 0.79f, 9);

            equationRot = new Vector3(0, 0, 2);

            knownPos = cam.ViewportToWorldPoint(new Vector3(0f, 0f, cam.nearClipPlane));
            knownPos = new Vector3(knownPos.x + knownExtents.x + 1f, knownPos.y + knownExtents.y - 3.8f, 9);

            knownRot = new Vector3(0, 0, 2);
        } else
        {
            equationPos = cam.ViewportToWorldPoint(new Vector3(1, .5f, cam.nearClipPlane));
            equationPos = new Vector3(equationPos.x + equationExtents.x, equationPos.y - 0.79f, 9);

            equationRot = new Vector3(0, 0, 10);

            knownPos = cam.ViewportToWorldPoint(new Vector3(0f, 0f, cam.nearClipPlane));
            knownPos = new Vector3(knownPos.x + knownExtents.x, knownPos.y - knownExtents.y, 9);

            knownRot = new Vector3(0, 0, 1);
        }
        return new Tuple<Vector3, Vector3, Vector3, Vector3>(equationPos, knownPos, equationRot, knownRot);
    }

    IEnumerator positionSheets()
    {
        while (true)
        {
            shouldGo = whereSheetsShouldGo(networkHandler.editSetting);
            equationSheet.transform.position = Vector3.Lerp(equationSheet.transform.position, shouldGo.Item1, Time.deltaTime * 8f);
            knownSheet.transform.position = Vector3.Lerp(knownSheet.transform.position, shouldGo.Item2, Time.deltaTime * 8f);
            equationSheet.transform.localEulerAngles = Vector3.Lerp(equationSheet.transform.localEulerAngles, shouldGo.Item3, Time.deltaTime * 8f);
            knownSheet.transform.localEulerAngles = Vector3.Lerp(knownSheet.transform.localEulerAngles, shouldGo.Item4, Time.deltaTime * 8f);
            repositionZ();
            yield return null;
        }
    }

    void repositionZ()
    {

        equationSheet.transform.localPosition = new Vector3(equationSheet.transform.localPosition.x, equationSheet.transform.localPosition.y, 9);
        knownSheet.transform.localPosition = new Vector3(knownSheet.transform.localPosition.x, knownSheet.transform.localPosition.y, 9);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
            foregroundAlpha = 0.5f;
        else
            foregroundAlpha = 0f;
    }

    public EquationPiece getNearestPieceToValue()
    {
        float closestDistance = Mathf.Infinity;
        EquationPiece nearest = null;
        foreach (EquationPiece equationPiece in piecesOverlapping)
        {
            float current = Vector3.Distance(equationPiece.transform.position, currentDragged.realLocation.position);
            if (current < closestDistance)
            {
                closestDistance = current;
                nearest = equationPiece;
            }
        }
        return nearest;
    }
    public bool isNearest(EquationPiece equationPiece)
    {
        return getNearestPieceToValue() == equationPiece;
    }

    public void valueReleased()
    {
        EquationPiece nearest = getNearestPieceToValue();
        if (nearest != null)
        {
            nearest.receiveValue(currentDragged);

            if (nearest.equation.equationId == 10) //put value into other slot for law of cosines
            {
                foreach (string c in new List<string> {"a","b"})
                {
                    if (nearest.baseText == c)
                    {
                        foreach (EquationPiece p in nearest.equation.equationPieces)
                        {
                            if (p != nearest && p.baseText == c)
                            {
                                p.receiveValue(currentDragged);
                                break;
                            }
                        }
                    }
                }
            }
            currentDragged.returnToOriginalPos();
        }
    }

    //IEnumerator shift()
    //{
    //    while (true)
    //    {
    //        yield return null;

    //        float backgroundAlpha = 1f - foregroundAlpha;

    //        Color tmp = foregroundSpriteRenderer.color;
    //        tmp.a = Mathf.MoveTowards(tmp.a, foregroundAlpha, 0.1f);
    //        foregroundSpriteRenderer.color = tmp;

    //        tmp = backgroundSpriteRenderer.color;
    //        tmp.a = Mathf.MoveTowards(tmp.a, backgroundAlpha, 0.1f);
    //        backgroundSpriteRenderer.color = tmp;
    //    }
    //}
}
