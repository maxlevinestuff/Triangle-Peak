using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquationSpawner : MonoBehaviour
{
    public GameObject equationObject;
    public GameObject equationHelpTextObject;

    public List<int> excludeIds;

    [System.Serializable]
    public struct EquationInfo
    {
        public string helpText;

        public string equationString;
        public Equation.Pieces[] equationTypes;
        public int equationId;
    }

    [SerializeField]
    public EquationInfo[] equationInfo;

    // Start is called before the first frame update
    void Awake()
    {
        Vector3 currentPos = transform.position;

        foreach (EquationInfo ei in equationInfo)
        {
            if (! excludeIds.Contains(ei.equationId))
            {
                GameObject newEquationObject = Instantiate(equationObject);
                newEquationObject.transform.SetParent(transform.parent);
                Equation newEquation = newEquationObject.GetComponent<Equation>();
                newEquation.initialize(ei.equationString, ei.equationTypes, ei.equationId);
                newEquationObject.transform.position = currentPos;
                currentPos -= new Vector3(0, Equation.GetTextMeshHeight(newEquation.equationPieces[0].textMesh), 0);

                if (ei.equationId == 10)
                    currentPos -= new Vector3(0, Equation.GetTextMeshHeight(newEquation.equationPieces[0].textMesh), 0);

                currentPos += new Vector3(0, .18f, 0);

                GameObject newEquationHelpTextObject = Instantiate(equationHelpTextObject);
                newEquationHelpTextObject.transform.SetParent(transform.parent);
                EquationHelpText newEquationHelpText = newEquationHelpTextObject.GetComponent<EquationHelpText>();
                newEquationHelpText.initialize(ei.helpText);
                newEquationHelpTextObject.transform.position = currentPos;
                currentPos -= new Vector3(0, Equation.GetTextMeshHeight(newEquationHelpText.textMesh), 0);

                currentPos -= new Vector3(0, .33f, 0);
            }
        }
    }

    private void Start()
    {
        //transform.parent.transform.localScale = new Vector3(.8f, .8f, .8f);

        Destroy(this.gameObject);
    }
}
