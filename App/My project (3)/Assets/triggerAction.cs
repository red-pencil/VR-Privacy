using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class triggerAction : MonoBehaviour
{

    public GameObject actuator;
    public GameObject trigger;
    public string scriptName;
    public bool triggerIsOn;

    public enum method // custom enumeration
    {
    gameobject_active,
    script_action
    };

    public method selectMethod;



    // Start is called before the first frame update
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {

        triggerIsOn = trigger.GetComponent<triggerDetector>().triggerOn;


        switch(selectMethod)
        {
            case method.gameobject_active:

            actuator.SetActive(triggerIsOn);
            break;

            case method.script_action:

            //actuator.GetComponent<scriptName>().action;
            break;



        }
        
    }
}
