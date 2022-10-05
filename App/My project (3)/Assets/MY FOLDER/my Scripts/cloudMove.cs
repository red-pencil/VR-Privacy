using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cloudMove : MonoBehaviour
{

    public Transform from;
    public Transform to;
    public float rotationSpeed;

    public float maxAngle;
    public float minAngle;

    private float timeCount = 0.0f;

    public Transform target;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(target);

        //transform.localRotation = Quaternion.Slerp(from.rotation, to.rotation, timeCount);
        timeCount = timeCount + Time.deltaTime;

        float rotation;
        int rDirection = 1;
        rotation =100f;

        if ((rotation>maxAngle) || (rotation<minAngle))
        {
        rDirection = -rDirection;
        }

        rotation = rDirection * rotationSpeed * Time.deltaTime;

        Vector3 fromItemToAvatar = new Vector3 (0,0,0);

        //this.transform.LocalRotate(0, rotation, 0);
    }
}
