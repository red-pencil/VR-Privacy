using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class triggerDetector : MonoBehaviour
{

    public string triggerEnterName, triggerExitName, triggerStayName;
    public bool onlyTriggerOn;
    public GameObject onlyTrigger;
    public bool triggerOn;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        triggerOn = (triggerStayName == "DummyPlayer")? true: false;
    }

    private void OnTriggerEnter(Collider other) {
        triggerEnterName = (onlyTriggerOn && other.name != onlyTrigger.name)? triggerEnterName: other.name;
    }

    private void OnTriggerExit(Collider other) {
        triggerExitName = (onlyTriggerOn && other.name != onlyTrigger.name)? triggerExitName: other.name;
        triggerEnterName = (triggerEnterName == other.name)? null: triggerEnterName; 
        triggerStayName = (triggerStayName == other.name)? null: triggerStayName; 
        
    }

    private void OnTriggerStay(Collider other) {
        triggerStayName = (onlyTriggerOn && other.name != onlyTrigger.name)? triggerStayName: other.name;
    }
}
