using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class doorRotate : MonoBehaviour
{
    public bool doorMove = false;
    public bool doorOpen = false;
    public GameObject door;
    public float timeCount;
    public float timeSpeed = 1;
    public float moveAngle = 90;

    Vector3 position0;
    Vector3 rotation0;
    bool doorOpen0 = false;


    void Start()
    {
        timeCount = 0;
        position0 = new Vector3 (door.transform.position.x, door.transform.position.y, door.transform.position.z);
        rotation0 = new Vector3 (door.transform.eulerAngles.x, door.transform.eulerAngles.y, door.transform.eulerAngles.z);
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            doorMove = true;
            doorOpen = true;
        }
        
        if (doorMove)
        {
            // detect if the status change
            if (doorOpen0 != doorOpen)
            {
                doorOpen0 = doorOpen;
                timeCount = 0;
            }
            if (doorOpen)
            {
                door.transform.rotation = Quaternion.Slerp(door.transform.rotation, 
                                                            Quaternion.Euler(rotation0.x, rotation0.y + moveAngle, rotation0.z),
                                                            timeSpeed * timeCount);
                timeCount = timeCount + Time.deltaTime;
            } else {
                door.transform.rotation = Quaternion.Slerp(door.transform.rotation, 
                                                            Quaternion.Euler(rotation0.x, rotation0.y, rotation0.z),
                                                            timeSpeed * timeCount);
                timeCount = timeCount + Time.deltaTime;
            }

        } else {
            timeCount = 0;
            door.transform.rotation =  Quaternion.Euler(rotation0.x, rotation0.y, rotation0.z);
        }

        
    }
}
