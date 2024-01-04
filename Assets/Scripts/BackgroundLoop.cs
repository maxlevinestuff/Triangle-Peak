using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundLoop : MonoBehaviour
{
    public GameObject[] levels;
    private Camera mainCamera;
    private Vector2 screenBounds;
    public float choke;
    public float scrollSpeed;

    private Dictionary<GameObject, List<List<GameObject>>> pieces = new Dictionary<GameObject, List<List<GameObject>>>();

    void Start()
    {
        mainCamera = gameObject.GetComponent<Camera>();
        screenBounds = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, mainCamera.transform.position.z));
        foreach (GameObject obj in levels)
        {
            loadChildObjects(obj);
        }
    }
    void loadChildObjects(GameObject obj)
    {
        float objectWidthX = obj.GetComponent<SpriteRenderer>().bounds.size.x - choke;
        float objectWidthY = obj.GetComponent<SpriteRenderer>().bounds.size.y - choke;

        int childsNeededX = (int)Mathf.Ceil(screenBounds.x * 2 / objectWidthX) + 1;
        int childsNeededY = (int)Mathf.Ceil(screenBounds.x * 2 / objectWidthY) + 1;

        List<List<GameObject>> piece = new List<List<GameObject>>();

        GameObject clone = Instantiate(obj) as GameObject;
        for (int y = 0; y <= childsNeededX; y++)
        {
            List<GameObject> l = new List<GameObject>();
            for (int x = 0; x < childsNeededY; x++)
            {
                GameObject c = Instantiate(clone) as GameObject;
                c.transform.SetParent(obj.transform);
                c.transform.position = new Vector3(objectWidthX * x, objectWidthY * y, obj.transform.position.z);
                l.Add(c);
            }
            piece.Add(l);
        }
        pieces[obj] = piece;
        Destroy(clone);
        Destroy(obj.GetComponent<SpriteRenderer>());
    }
    void repositionChildObjects(GameObject obj)
    {
        Vector3 lowerLeftPos = pieces[obj][0][0].transform.position;
        Vector3 upperRightPos = pieces[obj][pieces[obj].Count-1][pieces[obj][0].Count-1].transform.position;

        float halfObjectWidthX = pieces[obj][0][0].GetComponent<SpriteRenderer>().bounds.extents.x - choke;
        float halfObjectWidthY = pieces[obj][0][0].GetComponent<SpriteRenderer>().bounds.extents.y - choke;

        if (transform.position.x + screenBounds.x > upperRightPos.x + halfObjectWidthX)
        {
            foreach (List<GameObject> l in pieces[obj])
            {
                GameObject first = l[0];
                l.RemoveAt(0);
                l.Add(first);
                first.transform.position = new Vector3(upperRightPos.x + halfObjectWidthX * 2, first.transform.position.y, first.transform.position.z);
            }
        }
        if (transform.position.x - screenBounds.x < lowerLeftPos.x - halfObjectWidthX)
        {
            foreach (List<GameObject> l in pieces[obj])
            {
                GameObject last = l[l.Count - 1];
                l.RemoveAt(l.Count - 1);
                l.Insert(0, last);
                last.transform.position = new Vector3(lowerLeftPos.x - halfObjectWidthX * 2, last.transform.position.y, last.transform.position.z);
            }
        }

        if (transform.position.y + screenBounds.y > upperRightPos.y + halfObjectWidthY)
        {
            List<GameObject> first = pieces[obj][0];
            pieces[obj].RemoveAt(0);
            pieces[obj].Add(first);
            foreach (GameObject g in first)
                g.transform.position = new Vector3(g.transform.position.x, upperRightPos.y + halfObjectWidthY * 2, g.transform.position.z);
        }
        if (transform.position.y - screenBounds.y < lowerLeftPos.y - halfObjectWidthY)
        {
            List<GameObject> last = pieces[obj][pieces[obj].Count - 1];
            pieces[obj].RemoveAt(pieces[obj].Count - 1);
            pieces[obj].Insert(0, last);
            foreach (GameObject g in last)
                g.transform.position = new Vector3(g.transform.position.x, upperRightPos.y - halfObjectWidthY * 2, g.transform.position.z);
        }

    }
    void Update()
    {

        Vector3 velocity = Vector3.zero;
        Vector3 desiredPosition = transform.position + new Vector3(scrollSpeed, 0, 0);
        Vector3 smoothPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, 0.3f);
        transform.position = smoothPosition;

    }
    void LateUpdate()
    {
        foreach (GameObject obj in levels)
        {
            repositionChildObjects(obj);
        }
    }
}