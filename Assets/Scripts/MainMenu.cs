using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    Transform triangleImage;

    public void FreePlay()
    {
        SceneManager.LoadScene("FreeMode");
    }

    public void StartGame()
    {
        SceneManager.LoadScene("Level1");
    }

    // Start is called before the first frame update
    void Start()
    {
        triangleImage = GameObject.Find("Triangle").transform;
    }

    // Update is called once per frame
    void Update()
    {
        triangleImage.localEulerAngles += new Vector3(0, 0, 10 * Time.deltaTime);
    }
}
