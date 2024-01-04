using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

//An rough test of an equation with values that can be dragged on to, and produces output
public class Equation : MonoBehaviour
{
    public enum Pieces { word, angle, length, either};

    //public static Value.Unit? convert(Equation.Pieces p)
    //{
    //    if (p == Equation.Pieces.angle) return Value.Unit.Degrees;
    //    if (p == Equation.Pieces.length) return Value.Unit.Meters;
    //    return null;
    //}

    public EquationPiece equationPieceObject;

    public GameControl gameControl;

    public NetworkHandler networkHandler;

    public EquationPiece[] equationPieces;

    public string equationString = "sin(|a|)=|opp|/|adj";
    public Pieces[] equationTypes = {Pieces.word, Pieces.angle, Pieces.word, Pieces.length, Pieces.word, Pieces.length, Pieces.word};

    private int valuesNeeded = 0;

    public int equationId = 0;

    public int highlightA = 0;
    public int highlightB = 0;

    public bool isSolution;

    //public void Hint()
    //{
    //    if (isSolution)
    //    {
    //        StartCoroutine(moveMask());
    //    }
    //}
    //IEnumerator moveMask()
    //{
    //    GameObject mask = transform.Find("highlight mask").gameObject;

    //    while (mask.transform.localPosition.x < 5.725f)
    //    {
    //        yield return null;
    //        mask.transform.localPosition = new Vector3(mask.transform.localPosition.x + Time.deltaTime, mask.transform.localPosition.y, mask.transform.localPosition.z);
    //    }

    //    Destroy(mask);
    //}

    public void ResetIfEquationsClosed()
    {
        if (networkHandler.editSetting != NetworkHandler.EditSetting.book)
            resetEquation();
    }

    public void initialize(string equationString, Pieces[] equationTypes, int equationId)
    {
        this.equationString = equationString;
        this.equationTypes = equationTypes;
        this.equationId = equationId;

        gameControl = GameObject.Find("Control").GetComponent<GameControl>();
        networkHandler = GameObject.Find("Control").GetComponent<NetworkHandler>();

        string[] split = equationString.Split('|');
        equationPieces = new EquationPiece[split.Length];
        for (int i = 0; i < split.Length; i++)
        {
            EquationPiece newPiece = Instantiate(equationPieceObject);
            newPiece.transform.SetParent(transform);
            newPiece.Initialize(split[i], equationTypes[i]);
            if (equationTypes[i] != Pieces.word) valuesNeeded++;
            equationPieces[i] = newPiece;
        }
        setPiecePos();

        Question question = GameObject.Find("Question").GetComponent<Question>();
        isSolution = question.solutionEquationIds.Contains(equationId);
        if (! isSolution)
        {
            Destroy(transform.Find("highlight").gameObject);
            Destroy(transform.Find("highlight mask").gameObject);
        }
    }

    public void checkIfComplete()
    {
        List<float> known = new List<float>();
        foreach (EquationPiece p in equationPieces)
        {
            if (p.pieceType != Pieces.word && p.amount != null)
            {
                known.Add(p.amount.Value);
            }
        }

        //if (equationId == 10) //made not possible to solve for a or b here by disallowing input in equation piece
        //{
        //    bool onlyBLeft = true;
        //    foreach (EquationPiece p in equationPieces)
        //    {
        //        if (p.pieceType != Pieces.word)
        //        {
        //            if (p.baseText != "b" && p.amount == null)
        //                onlyBLeft = false;
        //        }
        //    }
        //    if (onlyBLeft)
        //    {
        //        solveEquation("b","b", known);
        //        return;
        //    }

        //    bool onlyALeft = true;
        //    foreach (EquationPiece p in equationPieces)
        //    {
        //        if (p.pieceType != Pieces.word)
        //        {
        //            if (p.baseText != "a" && p.amount == null)
        //                onlyALeft = false;
        //        }
        //    }
        //    if (onlyALeft)
        //    {
        //        solveEquation("a","a", known);
        //        return;
        //    }
        //}

        int valuesEntered = 0;
        EquationPiece unknownLetter = null;
        Value.Unit? unitToUse = null;
        int unknownIndex = 0;
        foreach (EquationPiece p in equationPieces)
        {
            if (p.pieceType != Pieces.word && p.amount == null) {
                unknownLetter = p;
                unknownIndex = valuesEntered;
            }
            if (p.pieceType != Pieces.word && p.amount != null)
            {
                valuesEntered++;
                unitToUse = p.unitRepresented;
            }
        }
        if (valuesEntered == valuesNeeded - 1)
        {
            initiateEquationSolve(unknownIndex, unknownLetter, known, unitToUse);
        }
    }

    public void initiateEquationSolve(int unknownIndex, EquationPiece unknownLetter, List<float> known, Value.Unit? unitToUse)
    {
        Tuple<float, Value.Unit> solution = EquationSolver(equationId, unknownIndex, known, unitToUse);

        //switch (equationId)
        //{
        //    case 0:
        //        switch (unknownIndex)
        //        {
        //            case 0:
        //        }
        //}

        //Debug.LogWarning("index: " + unknownIndex);
        //Debug.LogWarning("letter: " + unknownLetter);

        //string result = "List contents: ";
        //foreach (var item in known)
        //{
        //    result += item.ToString("F20") + ", ";
        //}
        //Debug.LogWarning(result);

        Value display = Instantiate(gameControl.valueObject);
        display.Initialize(solution.Item1, solution.Item2, transform.position + Vector3.left, null, true);
        display.SetOnPaper(transform.parent);
        resetEquation();
    }

    public void resetEquation()
    {
        foreach (EquationPiece p in equationPieces)
        {
            p.unitRepresented = null;
            p.amount = null;
            p.textMesh.text = p.baseText;
        }
        setPiecePos();
     }

    public static Tuple<float, Value.Unit> EquationSolver(int equationId, int unknownIndex, List<float> known, Value.Unit? unitToUse)
    {
        switch(equationId)
        {
            case 0: //addition
                switch(unknownIndex)
                {
                    case 0: return new Tuple<float, Value.Unit>(
                        known[1] - known[0],
                        unitToUse.Value);
                    case 1:
                        return new Tuple<float, Value.Unit>(
                    known[1] - known[0],
                    unitToUse.Value);
                    case 2:
                        return new Tuple<float, Value.Unit>(
                    known[0] + known[1],
                    unitToUse.Value);
                }
                break;
            case 1: //subtraction
                switch(unknownIndex)
                {
                    case 0:
                        return new Tuple<float, Value.Unit>(
                    known[1] + known[0],
                    unitToUse.Value);
                    case 1:
                        return new Tuple<float, Value.Unit>(
                    known[0] - known[1],
                    unitToUse.Value);
                    case 2:
                        return new Tuple<float, Value.Unit>(
                    known[0] - known[1],
                    unitToUse.Value);
                }
                break;
            case 2: //multiplication
                switch (unknownIndex)
                {
                    case 0:
                        return new Tuple<float, Value.Unit>(
                    known[1] / known[0],
                    unitToUse.Value);
                    case 1:
                        return new Tuple<float, Value.Unit>(
                    known[1] / known[0],
                    unitToUse.Value);
                    case 2:
                        return new Tuple<float, Value.Unit>(
                    known[0] * known[1],
                    unitToUse.Value);
                }
                break;
            case 3: //division
                switch (unknownIndex)
                {
                    case 0:
                        return new Tuple<float, Value.Unit>(
                    known[1] * known[0],
                    unitToUse.Value);
                    case 1:
                        return new Tuple<float, Value.Unit>(
                    known[1] / known[0],
                    unitToUse.Value);
                    case 2:
                        return new Tuple<float, Value.Unit>(
                    known[0] / known[1],
                    unitToUse.Value);
                }
                break;
            case 4: //pythagorean theorem
                switch (unknownIndex)
                {
                    case 0:
                        return new Tuple<float, Value.Unit>(
                    (Mathf.Sqrt((known[1] * known[1]) - (known[0] * known[0]))),
                    Value.Unit.Meters);
                    case 1:
                        return new Tuple<float, Value.Unit>(
                    (Mathf.Sqrt((known[1] * known[1]) - (known[0] * known[0]))),
                    Value.Unit.Meters);
                    case 2:
                        return new Tuple<float, Value.Unit>(
                    (Mathf.Sqrt((known[0] * known[0]) + (known[1] * known[1]))),
                    Value.Unit.Meters);
                }
                break;
            case 5: //sum of angles rule
                switch (unknownIndex)
                {
                    case 0:
                        return new Tuple<float, Value.Unit>(
                    180 - known[0] - known[1],
                    Value.Unit.Degrees);
                    case 1:
                        return new Tuple<float, Value.Unit>(
                    180 - known[0] - known[1],
                    Value.Unit.Degrees);
                    case 2:
                        return new Tuple<float, Value.Unit>(
                    180 - known[0] - known[1],
                    Value.Unit.Degrees);
                }
                break;
            case 6: //SOH
                switch (unknownIndex)
                {
                    case 0:
                        return new Tuple<float, Value.Unit>(
                    Mathf.Asin(known[0] / known[1]) * Mathf.Rad2Deg,
                    Value.Unit.Degrees);
                    case 1:
                        return new Tuple<float, Value.Unit>(
                    known[1] * Mathf.Sin(known[0] * Mathf.Deg2Rad),
                    Value.Unit.Meters);
                    case 2:
                        return new Tuple<float, Value.Unit>(
                    known[1] / Mathf.Sin(known[0] * Mathf.Deg2Rad),
                    Value.Unit.Meters);
                }
                break;
            case 7: //CAH
                switch (unknownIndex)
                {
                    case 0:
                        return new Tuple<float, Value.Unit>(
                    Mathf.Acos(known[0] / known[1]) * Mathf.Rad2Deg,
                    Value.Unit.Degrees);
                    case 1:
                        return new Tuple<float, Value.Unit>(
                    known[1] * Mathf.Cos(known[0] * Mathf.Deg2Rad),
                    Value.Unit.Meters);
                    case 2:
                        return new Tuple<float, Value.Unit>(
                    known[1] / Mathf.Cos(known[0] * Mathf.Deg2Rad),
                    Value.Unit.Meters);
                }
                break;
            case 8: //TOA
                switch (unknownIndex)
                {
                    case 0:
                        return new Tuple<float, Value.Unit>(
                    Mathf.Atan(known[0] / known[1]) * Mathf.Rad2Deg,
                    Value.Unit.Degrees);
                    case 1:
                        return new Tuple<float, Value.Unit>(
                    known[1] * Mathf.Tan(known[0] * Mathf.Deg2Rad),
                    Value.Unit.Meters);
                    case 2:
                        return new Tuple<float, Value.Unit>(
                    known[1] / Mathf.Tan(known[0] * Mathf.Deg2Rad),
                    Value.Unit.Meters);
                }
                break;
            case 9: //law of sines
                switch (unknownIndex)
                {
                    case 0:
                        return new Tuple<float, Value.Unit>(
                    Mathf.Asin(known[0] * Mathf.Sin(known[1] * Mathf.Deg2Rad) / known[2]) * Mathf.Rad2Deg,
                    Value.Unit.Degrees);
                    case 1:
                        return new Tuple<float, Value.Unit>(
                    Mathf.Sin(known[0] * Mathf.Deg2Rad) / (Mathf.Sin(known[1] * Mathf.Deg2Rad) / known[2]),
                    Value.Unit.Meters);
                    case 2:
                        return new Tuple<float, Value.Unit>(
                    Mathf.Asin(known[2] * Mathf.Sin(known[0] * Mathf.Deg2Rad) / known[1]) * Mathf.Rad2Deg,
                    Value.Unit.Degrees);
                    case 3:
                        return new Tuple<float, Value.Unit>(
                    Mathf.Sin(known[2] * Mathf.Deg2Rad) / (Mathf.Sin(known[0] * Mathf.Deg2Rad) / known[1]),
                    Value.Unit.Meters);
                }
                break;
            case 10: //law of cosines
                switch (unknownIndex)
                {
                    case 0:
                        return new Tuple<float, Value.Unit>(
                    Mathf.Sqrt((known[0] * known[0]) + (known[1] * known[1]) - 2 * known[1] * known[1] * Mathf.Cos(known[4] * Mathf.Deg2Rad)),
                    Value.Unit.Meters);
                    case 5:
                        return new Tuple<float, Value.Unit>(
                    Mathf.Acos(((known[1] * known[1]) + (known[2] * known[2]) - (known[0] * known[0])) / (2 * known[1] * known[2])) * Mathf.Rad2Deg,
                    Value.Unit.Degrees);
                }
                break;
        }

        return new Tuple<float, Value.Unit>(10, Value.Unit.Degrees);
    }

    public float setPiecePos()
    {
        Vector3 total = transform.position;
        float totalScaleInverse = 1f / totalScale(equationPieces[0].transform).x;
        for (int i = 0; i < equationPieces.Length; i++)
        {
            if (equationPieces[i].newLine)
            {
                total = new Vector3(transform.position.x + .5f, total.y - .5f, total.y);
            }
            equationPieces[i].transform.position = total;
            float textMeshWidth = GetTextMeshWidth(equationPieces[i].textMesh);
            float textMeshHeight = GetTextMeshHeight(equationPieces[i].textMesh);
            if (equationPieces[i].GetComponent<CapsuleCollider2D>() != null)
            {
                equationPieces[i].capsuleCollider.offset = new Vector2(textMeshWidth * totalScaleInverse / 2f, 0f); //inverse must be applied as capsule doesnt use local scaling?
                equationPieces[i].capsuleCollider.size = new Vector2(textMeshWidth * totalScaleInverse, textMeshHeight * totalScaleInverse); //inverse must be applied as capsule doesnt use local scaling?
            }
            total += transform.right * textMeshWidth;

        }
        return total.x;
    }

    // Start is called before the first frame update
    void Start()
    {
        networkHandler.ChangedEditSetting.AddListener(ResetIfEquationsClosed);
    }

    public void updateHighlights()
    {
        foreach (EquationPiece ep in equationPieces)
        {
            ep.updateHighlights();
        }
    }

    //Animates and then creates a value object with the output
    //private IEnumerator produceOutput()
    //{
    //    for (int i = 0; i < 50; i++)
    //    {
    //        float randomRot = Random.Range(-2, 2);
    //        transform.rotation = Quaternion.Euler(0,0,randomRot);
    //        yield return new WaitForSeconds(0.01f);
    //    }
    //    transform.rotation = Quaternion.Euler(0, 0, 0);

    //    float output = length * Mathf.Tan(angle * Mathf.PI / 180);

    //    angle = 0; length = 0;
    //    updateString();

    //    Value valueObject = Instantiate(networkHandler.valueObject);
    //    valueObject.Initialize(output, Value.Unit.Meters, transform.position - new Vector3(0, 0.7f, 0));
    //}

    //Tells if has all data needed to output
    //private bool completed()
    //{
    //    return angle != 0 && length != 0;
    //}

    public static float GetTextMeshWidth(TextMesh textMesh)
    {
        string[] textLines = textMesh.text.Split('\n');
        int widestLineWidth = 0;

        // Iterate through each line of text
        foreach (string textLine in textLines)
        {
            int width = 0;

            // Iterate through each symbol in the current text line
            foreach (char symbol in textLine)
            {
                if (textMesh.font.GetCharacterInfo(symbol, out CharacterInfo charInfo, textMesh.fontSize, textMesh.fontStyle))
                    width += charInfo.advance;
            }

            if (widestLineWidth <= 0 || width > widestLineWidth)
                widestLineWidth = width;
        }

        // Multiplied by 0.1 to make the size of this match the bounds size of meshes (which is 10x larger)
        return widestLineWidth * textMesh.characterSize * Mathf.Abs(totalScale(textMesh.transform).x) * 0.1f;
    }

    public static float GetTextMeshHeight(TextMesh textMesh)
    {
        return (textMesh.fontSize) * 0.1f * textMesh.text.Split('\n').Length * totalScale(textMesh.transform).x;
    }

    static Vector3 totalScale(Transform t)
    {

        Vector3 tempScale = new Vector3(1, 1, 1);
        while (t != null)
        {
            tempScale = new Vector3(tempScale.x * t.localScale.x, tempScale.y * t.localScale.y, tempScale.z * t.localScale.z);
            t = t.parent;
        }
        return tempScale;
    }


}
