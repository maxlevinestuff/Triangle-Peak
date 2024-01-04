using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Writeout : MonoBehaviour
{
    string fullText;
    int advanceChar = 0;
    TextMesh textMesh;

    // Start is called before the first frame update
    void Start()
    {
        textMesh = GetComponent<TextMesh>();
        fullText = textMesh.text;
        textMesh.text = "";

        StartCoroutine(Write());
    }

    IEnumerator Write()
    {
        while (advanceChar < fullText.Length)
        {
            yield return new WaitForSeconds(.05f);
            advanceChar++;
            textMesh.text = fullText.Substring(0, advanceChar);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
