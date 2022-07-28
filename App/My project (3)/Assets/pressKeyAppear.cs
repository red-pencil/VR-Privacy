using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pressKeyAppear : MonoBehaviour
{

    public GameObject cellphone;
    public GameObject cellphoneScreen;
    public GameObject cellphoneTrigger;

    // Start is called before the first frame update
    void Start()
    {
        
        cellphoneScreen.SetActive(false);
        cellphone.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            cellphone.SetActive(!cellphone.activeInHierarchy);
        }
    }

    void OnTriggerEnter(Collider other) 
    {
        if (other.name == cellphoneTrigger.name)
        {
            cellphoneScreen.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other) 
    {
        if (other.name == cellphoneTrigger.name)
        {
            cellphoneScreen.SetActive(false);
        }
    }

}
