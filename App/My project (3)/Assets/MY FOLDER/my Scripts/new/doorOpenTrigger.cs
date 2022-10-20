using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class doorOpenTrigger : MonoBehaviour
{
    public GameObject targetPlayer;
    public bool everyPlayer = true;
    public GameObject poster1, poster2;
    public GameObject door1, door2;
    public doorRotate doorScript1, doorScript2;


    void Start()
    {
        //doorScript1 = door1.GetComponent<doorRotate>;
        //doorScript2 = door2.GetComponent<doorRotate>;
        poster1.SetActive(false);
        poster2.SetActive(false);
    }


    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        if ( everyPlayer || other.name == targetPlayer.name)
        {
            poster1.SetActive(true);
            poster2.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other) 
    {
        if ( everyPlayer || other.name == targetPlayer.name)
        {
            poster1.SetActive(false);
            poster2.SetActive(false);
        }
    }
}
