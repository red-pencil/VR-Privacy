using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class doorSlide : MonoBehaviour
{
    public bool doorMove = false;
    public bool doorOpen = false;
    public GameObject door;
    public float timeCount;
    public float timeSpeed = 1;
    public float moveDistance = 5;

    Vector3 position0;
    bool doorOpen0 = false;

    // Start is called before the first frame update
    void Start()
    {
        timeCount = 0;
        position0 = new Vector3 (door.transform.position.x, door.transform.position.y, door.transform.position.z);
    }

    // Update is called once per frame
    void Update()
    {
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
                door.transform.position = new Vector3 (Mathf.Lerp(door.transform.position.x, position0.x + moveDistance, timeSpeed * timeCount),
                                        door.transform.position.y,
                                        door.transform.position.z);
                timeCount = timeCount + Time.deltaTime;
            } else {
                door.transform.position = new Vector3 (Mathf.Lerp(door.transform.position.x, position0.x, timeSpeed * timeCount),
                                        door.transform.position.y,
                                        door.transform.position.z);
                timeCount = timeCount + Time.deltaTime;
            }

        } else {
            timeCount = 0;
            door.transform.position = position0;
        }

        
    }
}
