using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Question : MonoBehaviour
{
    public bool freeMode;

    public float correctAmount;
    public Value.Unit correctUnit;
    public int[] solutionEquationIds;

    CapsuleCollider2D capsuleCollider;
    TextMesh textMesh;

    public UnityEvent levelWon;

    private void Awake()
    {
        levelWon = new UnityEvent();
    }

    // Start is called before the first frame update
    void Start()
    {
        textMesh = GetComponent<TextMesh>();

        capsuleCollider = GetComponent<CapsuleCollider2D>();

        capsuleCollider.size = new Vector2(Equation.GetTextMeshWidth(textMesh) * 10, Equation.GetTextMeshHeight(textMesh) * 10);

        if (freeMode)
        {
            GetComponent<MeshRenderer>().enabled = false;
            capsuleCollider.enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Value")
        {
            Value value = collision.GetComponent<Value>();

            if (value.unit == correctUnit)
            {
                if (AngleCalculator.RoughlyEqual(value.value, correctAmount, 0.002f))
                {
                    textMesh.text = Value.roundValueForDisplay(value.value, value.unit);
                    levelWon.Invoke();
                    value.mouseDown = false;
                    //value.returnToOriginalPos(); //still need to deal with value other than just deleting it
                    Destroy(value.gameObject);
                }
            }
        }
    }
}
