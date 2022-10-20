using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class triggerDetector : MonoBehaviour
{

    public string triggerEnterName, triggerExitName, triggerStayName;
    public bool targetPlayerOn;
    public GameObject targetPlayer;
    public bool everyPlayer;
    public bool triggerOn;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!everyPlayer) triggerOn = (triggerStayName == targetPlayer.name)? true: false;
    }

    private void OnTriggerEnter(Collider other) {
        if (everyPlayer)
        {
            triggerOn = true;
        }
        else
        {
            triggerEnterName = (targetPlayerOn && other.name != targetPlayer.name)? triggerEnterName: other.name;
        }
    }

    private void OnTriggerExit(Collider other) {
        if (everyPlayer)
        {
            triggerOn = false;
        }
        else
        {
            triggerExitName = (targetPlayerOn && other.name != targetPlayer.name)? triggerExitName: other.name;
            triggerEnterName = (triggerEnterName == other.name)? null: triggerEnterName; 
            triggerStayName = (triggerStayName == other.name)? null: triggerStayName; 
        }
        
    }

    private void OnTriggerStay(Collider other) {
        if (everyPlayer)
        {
            triggerOn = true;
        }
        else
        {
            triggerStayName = (targetPlayerOn && other.name != targetPlayer.name)? triggerStayName: other.name;
        }
    }
}
