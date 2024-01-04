using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Flag : MonoBehaviour
{
    public bool levelBeatToFadeIn;

    Renderer renderer;
    Collider2D collider;

    NetworkHandler networkHandler;

    // Start is called before the first frame update
    void Start()
    {
        networkHandler = GameObject.Find("Control").GetComponent<NetworkHandler>();

        if (levelBeatToFadeIn)
        {
            renderer = GetComponent<Renderer>();
            SetAlphaZero();
            collider = GetComponent<Collider2D>();
            collider.enabled = false;
            GameObject.Find("Question").GetComponent<Question>().levelWon.AddListener(LevelWon);
        }
    }

    void LevelWon()
    {
        GameObject.Find("Question").GetComponent<Question>().levelWon.RemoveAllListeners();
        StartCoroutine(FadeIn(.5f));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator FadeIn(float aTime)
    {
        //if (networkHandler.editSetting == NetworkHandler.EditSetting.book)
        //    networkHandler.SetEditSettingFromButton("add");

        yield return new WaitForSeconds(.09f);

        float alpha = renderer.material.color.a;
        Vector3 endPos = transform.localPosition;
        Vector3 startPos = transform.localPosition + new Vector3(0, 0.5f, 0);
        for (float t = 0.0f; t < 1.0f; t += Time.deltaTime / aTime)
        {
            Color newColor = new Color(1, 1, 1, Mathf.Lerp(alpha, 1, t));
            renderer.material.color = newColor;

            transform.localPosition = Vector3.Lerp(startPos, endPos, t);

            yield return null;
        }

        collider.enabled = true;
    }

    void SetAlphaZero()
    {
        Color newColor = new Color(1, 1, 1, 0);
        renderer.material.color = newColor;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            if (SceneManager.GetActiveScene().buildIndex == 11)
                SceneManager.LoadScene("Title");
            else
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }
}
