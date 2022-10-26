using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class sceneState : MonoBehaviour
{
    public Material light, dark;

    public bool switchOn = false;
    
    void Start()
    {
        
    }


    void Update()
    {
        if (switchOn)
        {
            gameObject.GetComponent<Renderer>().material = light;
        } else {
            gameObject.GetComponent<Renderer>().material = dark;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        switchOn = !switchOn;
    }


}
