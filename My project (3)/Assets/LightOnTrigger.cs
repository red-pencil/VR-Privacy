using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightOnTrigger : MonoBehaviour
{
    public GameObject light1;
    public GameObject light2;

    void OnTriggerStay(Collider other)
    {
        if (other.gameObject.name == "player")
        {
            light1.SetActive(true);
            light2.SetActive(true);
            Debug.Log("LightOn");
        }

    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name == "player")
        {
            light1.SetActive(false);
            light2.SetActive(false);
            Debug.Log("LightOn");
        }
    }
}
