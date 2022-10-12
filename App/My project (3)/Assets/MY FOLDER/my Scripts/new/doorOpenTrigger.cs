using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class doorOpenTrigger : MonoBehaviour
{
    public GameObject target;
    public GameObject poster;
    public GameObject door1, door2;
    public doorRotate doorScript1, doorScript2;


    void Start()
    {
        //doorScript1 = door1.GetComponent<doorRotate>;
        //doorScript2 = door2.GetComponent<doorRotate>;
        poster.SetActive(false);
    }


    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.name == target.name)
        {
            poster.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other) 
    {
        if (other.name == target.name)
        {
            poster.SetActive(false);
        }
    }
}
