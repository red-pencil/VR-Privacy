using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class doorTrigger : MonoBehaviour
{
    public GameObject door;
    public doorRotate doorScript;

    void Start()
    {
        doorScript = door.GetComponent<doorRotate>();
        doorScript.doorMove = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other) {

    if (other.name == "handTouchTrigger")
    {
        doorScript.doorOpen = true;
        StartCoroutine(doorClose(3));
    }

    }


    IEnumerator doorClose(float waitTime = 3.0f) {

        yield return new WaitForSeconds(waitTime);
        doorScript.doorOpen = false;
    }

}
