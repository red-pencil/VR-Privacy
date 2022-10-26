using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class sceneSwtich : MonoBehaviour
{
    public GameObject scene;
    public bool sceneOn;

    void Start()
    {
        scene.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        sceneOn = GetComponent<sceneState>().switchOn;

        if (sceneOn)
        {
            scene.SetActive(true);
        } else {
            scene.SetActive(false);
        }


    }
}
