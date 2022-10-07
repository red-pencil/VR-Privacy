using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
public class CellPhoneTigger : MonoBehaviour
{
    [SerializeField]
    XRBaseController controller;

    public GameObject mobile;
    bool stay = false;
    void OnTriggerStay(Collider other)
    {
        stay = true; 
    }

    IEnumerator Start()
    {
        if (stay)
        {
            StartCoroutine("DoSomething", 2.0f);
            yield return new WaitForSeconds(1);
            StopCoroutine("DoSomething");
        }
        
    }
    IEnumerator DoSomething(float someParameter)
    {
        while (stay)
        {
            SendHaptics();
            mobile.SetActive(true);
            yield return null;
        }
    }

    void SendHaptics()
    {
        if (controller != null)
            controller.SendHapticImpulse(0.7f, 0.1f);
    }

    void OnTriggerExit(Collider other)
    {
        stay = false;
        if (other.gameObject.name == "player")
        {
            mobile.SetActive(false);
        }
    }
}
