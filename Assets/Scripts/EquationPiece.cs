using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquationPiece : MonoBehaviour
{
    public TextMesh textMesh;
    private MeshRenderer meshRenderer;
    public CapsuleCollider2D capsuleCollider;

    public Equation equation;

    public string baseText;
    public Equation.Pieces pieceType;
    public Value.Unit? unitRepresented = null;
    public float? amount = null;

    public bool newLine = false;

    public bool isRightType(Value.Unit u, EquationPiece p)
    {
        if (equation.equationId == 10)
        {
            int countSpotsOtherThanCurrentOrBOrA = 0;
            foreach (EquationPiece e in p.equation.equationPieces)
            {
                if (p != e && e.amount == null && e.baseText != "b" && e.baseText != "a" && e.pieceType != Equation.Pieces.word) countSpotsOtherThanCurrentOrBOrA++;
            }
            Debug.LogWarning("spots counted: " + countSpotsOtherThanCurrentOrBOrA);
            if (countSpotsOtherThanCurrentOrBOrA <= 0) return false;
        }

        if (u == Value.Unit.Degrees && p.pieceType == Equation.Pieces.angle) return true;
        if (u == Value.Unit.Meters && p.pieceType == Equation.Pieces.length) return true;

        if (p.pieceType == Equation.Pieces.either)
        {
            bool foundPieceNot = false;
            foreach (EquationPiece e in equation.equationPieces)
            {
                if (e != p)
                {
                    if (!(e.unitRepresented == null || u == e.unitRepresented))
                    {
                        foundPieceNot = true;
                        break;
                    }
                }
            }
            return !foundPieceNot;
        }

        return false;
    }

    public void Initialize(string baseText, Equation.Pieces pieceType)
    {
        GetComponent<Renderer>().sortingLayerName = "EquationSheet";

        equation = transform.parent.GetComponent<Equation>();
        textMesh = GetComponent<TextMesh>();
        meshRenderer = GetComponent<MeshRenderer>();
        textMesh.text = "tanθ = opp/adj";
        capsuleCollider = GetComponent<CapsuleCollider2D>();

        //if (equation.equationId == 10)
        //    transform.localScale = new Vector3(.11f, .11f, .11f);

        if (baseText.Contains("\\n"))
        {
            baseText = baseText.Replace("\\n", "");
            newLine = true;
        }

        this.baseText = baseText;
        this.pieceType = pieceType;
        textMesh.text = baseText;

        textMesh.text = textMesh.text.Replace("\\n", "\n");

        if (pieceType == Equation.Pieces.word)
        {
            Component.Destroy(capsuleCollider);
        }
    }

    public void updateHighlights()
    {
        if (equation.gameControl.isNearest(this))
        {
            if (isRightType(equation.gameControl.currentDragged.unit, this))
                textMesh.color = Color.green;
            else
                textMesh.color = Color.red;
        } else
        {
            textMesh.color = Color.black;

            EquationPiece closest = equation.gameControl.getNearestPieceToValue();
            if (equation.highlightB > 0 && equation.equationId == 10 && closest.baseText == "b" && baseText == "b" && isRightType(equation.gameControl.currentDragged.unit, closest))
                textMesh.color = Color.green;
            if (equation.highlightA > 0 && equation.equationId == 10 && closest.baseText == "a" && baseText == "a" && isRightType(equation.gameControl.currentDragged.unit, closest))
                textMesh.color = Color.green;
        }
    }

    public void receiveValue(Value value)
    {
        if (isRightType(value.unit, this))
        {
            textMesh.text = Value.roundValueForDisplay(value.value, value.unit);
            unitRepresented = value.unit;
            amount = value.value;
            equation.setPiecePos();
            equation.checkIfComplete();
        }
    }

    //If the mouse released a value onto this object, then incorporate that value
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (pieceType != Equation.Pieces.word && col.gameObject.tag == "Value" && col.transform.parent == equation.transform.parent)
        {
            if (equation.equationId == 10)
            {
                if (baseText == "b") equation.highlightB += 1;
                if (baseText == "a") equation.highlightA += 1;
            }
            equation.gameControl.piecesOverlapping.Add(this);
            equation.gameControl.currentDragged = col.gameObject.GetComponent<Value>();
            equation.updateHighlights();
            //incorporate value
        }
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (pieceType != Equation.Pieces.word && col.gameObject.tag == "Value")
        {
            if (equation.gameControl.piecesOverlapping.Contains(this))
            {
                if (equation.equationId == 10)
                {
                    if (baseText == "b") equation.highlightB -= 1;
                    if (baseText == "a") equation.highlightA -= 1;
                }
                equation.gameControl.piecesOverlapping.Remove(this);
                equation.updateHighlights();
                //incorporate value
            }
        }
    }
}
