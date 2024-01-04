using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquationHelpText : MonoBehaviour
{
    public TextMesh textMesh;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        GetComponent<Renderer>().sortingLayerName = "EquationSheet";
        GetComponent<Renderer>().sortingOrder = 200;
    }

    public void initialize(string text)
    {
        textMesh = GetComponent<TextMesh>();
        meshRenderer = GetComponent<MeshRenderer>();

        textMesh.text = text.Replace("\\n", "\n");
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
