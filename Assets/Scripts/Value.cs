using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//The values (lengths and angles) that appear when measuring things and can be dragged around in book mode
public class Value : MonoBehaviour
{
    //Types of units
    public enum Unit { Meters, Degrees }

    public static string roundValueForDisplay(float v, Unit u)
    {
        return (Mathf.Round(v * 100f) / 100f).ToString() + GetSymbol(u);
    }

    //Get the display string for a given unit type
    public static string GetSymbol(Unit unit)
    {
        switch (unit)
        {
            case Unit.Meters: return "m";
            case Unit.Degrees: return "°";
        }
        return "";
    }

    public Unit unit;
    public float value;
    public Vector3? originalPos;
    public Transform originalParent;

    public Vector3 realOriginalPos;

    public bool noOriginalPos;

    private TextMesh textMesh;
    private Renderer renderer;

    private NetworkHandler networkHandler;

    private CapsuleCollider2D capsuleCollider;
    public Transform realLocation;

    private DrawLineBetweenTwoObjects drawLine;

    private GameControl gameControl;

    //Set up the new value
    public void Initialize(float value, Unit unit, Vector3 position, Transform parent, bool noOriginalPos = false)
    {

        renderer = GetComponent<Renderer>();

        realLocation = transform.Find("RealLocation");

        this.noOriginalPos = noOriginalPos;

        this.value = value;
        this.unit = unit;

        transform.position = new Vector3(position.x, position.y, -1); //place slightly above

        drawLine = gameObject.GetComponent<DrawLineBetweenTwoObjects>();
        if (!noOriginalPos)
        {
            originalPos = transform.position;
            drawLine.Initialize(originalPos.Value, transform, 0, 0);
        }
        else
            Destroy(drawLine);

        realOriginalPos = transform.position;

        textMesh = GetComponent<TextMesh>();

        textMesh.text = roundValueForDisplay(value, unit);

        networkHandler = GameObject.Find("Control").GetComponent<NetworkHandler>();

        gameControl = GameObject.Find("Control").GetComponent<GameControl>();

        capsuleCollider = GetComponent<CapsuleCollider2D>();
        capsuleCollider.size = new Vector2(Equation.GetTextMeshWidth(textMesh) * 10, Equation.GetTextMeshHeight(textMesh) * 10);

        networkHandler.ChangedEditSetting.AddListener(ChangedEditSetting);
        ChangedEditSetting();

        originalParent = parent;
        transform.SetParent(parent);
    }

    //IEnumerator typeOut(string toDisplay)
    //{
    //    int progress = 0;
    //    while (toDisplay.Length != textMesh.text.Length)
    //    {
    //        progress++;
    //        textMesh.text = toDisplay.Substring(0, progress);
    //        yield return new WaitForSeconds(0.2f);
    //    }
    //}

    private void Update()
    {

        updateOnPaper();
    }

    //bool temp = false;
    //Vector3 tempPos;
    //Transform tempParent;
    bool tempOnPaper = false;
    string tempSortingLayerName = "Default";

    //Returns the value to its original calculation point
    public void returnToOriginalPos()
    {
        if (originalPos.HasValue)
        {
            transform.position = originalPos.Value;

            transform.SetParent(originalParent);
            fullyOnPaper = tempOnPaper;
            onPaper = tempOnPaper;
            currentlyPlacedOnPaper = tempOnPaper;
            if (!noOriginalPos)
                drawLine._renderer.sortingLayerName = tempSortingLayerName;
            renderer.sortingLayerName = tempSortingLayerName;
        }
        else
            Destroy(this.gameObject);
    }

    private Vector3 screenPoint;
    private Vector3 offset;

    public static bool InsideCol(Collider2D mycol, Collider2D other)
    {
        return (other.bounds.Contains(mycol.bounds.min)
             && other.bounds.Contains(mycol.bounds.max));
    }

    public void SetOnPaper(Transform paperT) //should only be called when sure it will end up fully on the paper
    {
        paperTransform = paperT;
        fullyOnPaper = true;
        onPaper = true;

        currentlyPlacedOnPaper = true;
        transform.SetParent(paperT);
        if (!noOriginalPos)
            drawLine._renderer.sortingLayerName = "EquationSheet";
        renderer.sortingLayerName = "EquationSheet";
    }

    public bool mouseDown = false;
    private bool onPaper = false;
    private bool fullyOnPaper = false;
    private Transform paperTransform;
    private bool currentlyPlacedOnPaper = false;

    void updateOnPaper()
    {
        bool shouldGoOnPaper = false;
        if (fullyOnPaper) shouldGoOnPaper = true;
        if (mouseDown && onPaper) shouldGoOnPaper = true;

        if (shouldGoOnPaper && !currentlyPlacedOnPaper)
        {
            currentlyPlacedOnPaper = true;
            transform.SetParent(paperTransform);
            if (!noOriginalPos)
                drawLine._renderer.sortingLayerName = "EquationSheet";
            renderer.sortingLayerName = "EquationSheet";
        }
        else if (!shouldGoOnPaper && currentlyPlacedOnPaper)
        {
            currentlyPlacedOnPaper = false;
            transform.SetParent(null);
            if (!noOriginalPos)
                drawLine._renderer.sortingLayerName = "Default";
            renderer.sortingLayerName = "Default";
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (mouseDown)
        {
            if (collision.tag == "Sheet")
            {
                onPaper = true;
                paperTransform = collision.transform;
                if (InsideCol(capsuleCollider, collision))
                {
                    fullyOnPaper = true;
                }
                else
                {
                    fullyOnPaper = false;
                }
            }
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (mouseDown)
        {
            if (collision.tag == "Sheet")
            {
                fullyOnPaper = false;
                onPaper = false;
            }
        }
    }

    //Move the value on mouse drag
    void OnMouseDown()
    {
        if (networkHandler.editSetting == NetworkHandler.EditSetting.book)
        {
            offset = gameObject.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
            mouseDown = true;

            if (noOriginalPos)
            {
                originalPos = transform.position;
                originalParent = transform.parent;
                tempOnPaper = currentlyPlacedOnPaper;
                tempSortingLayerName = renderer.sortingLayerName;
            }
        }
    }
    //Move the value on mouse drag
    void OnMouseDrag()
    {
        if (networkHandler.editSetting == NetworkHandler.EditSetting.book && mouseDown)
        {
            Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
            Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;
            transform.position = curPosition;
        }
    }

    private void OnMouseUp()
    {
        if (networkHandler.editSetting == NetworkHandler.EditSetting.book)
        {
            gameControl.valueReleased();
            mouseDown = false;

            if (currentlyPlacedOnPaper && fullyOnPaper)
            {
                if (paperTransform.gameObject.name == "KnownSheet" && (originalParent == null || originalParent.gameObject.name != "KnownSheet"))
                {
                    if (!noOriginalPos)
                        copyAndReturn();
                }
            }
        }
    }

    public void copyAndReturn()
    {
        Value valueCopy = Instantiate(networkHandler.valueObject);
        valueCopy.Initialize(value, unit, transform.position, null, true);
        valueCopy.SetOnPaper(transform.parent);
        returnToOriginalPos();
    }

    private void ChangedEditSetting()
    {
        if (networkHandler.editSetting == NetworkHandler.EditSetting.book)
        {
            //capsuleCollider.enabled = true;
            gameObject.layer = 0; //default layer
        }
        else
        {
            //capsuleCollider.enabled = false;
            gameObject.layer = 2; //ignore raycast
            if (transform.parent != null && transform.parent.gameObject.name == "EquationSheet")
            {
                if (!noOriginalPos)
                    returnToOriginalPos();
                else
                    Destroy(this.gameObject);
            }
        }
    }

    public static bool AreValuesTheSame(List<Value> values1, List<Value> values2)
    {
        if (values1.Count != values2.Count) return false;

        for (int i = 0; i < values1.Count; i++)
        {
            if (!IsValueTheSame(values1[i], values2[i])) return false;
        }

        return true;
    }
    public static bool IsValueTheSame(Value value1, Value value2)
    {
        if (value1 == null && value2 != null) return false;
        if (value1 != null && value2 == null) return false;
        if (value1 == null && value2 == null) return true;
        if (value1.realOriginalPos != value2.realOriginalPos) return false; //important NOT transform.position but ORIGINAL
        if (value1.unit != value2.unit) return false;
        if (value1.value != value2.value) return false;
        return true;
    }

}
