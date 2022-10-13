using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class movingItem : MonoBehaviour
{

    public Transform followTarget;

    public float timeDelay = 0.0f;
    public float timeCount = 0.0f;
    public float timeSpeed=0.1f;
    public float timeDelaySet = 0.0f;
    public Transform from;
    public Transform to;

    public Vector3 tempIntial;
    public Vector3 tempEnd;
    public float targetAngle = 80f;

    // Start is called before the first frame update
    void Start()
    {
        timeDelay = 0;
        timeCount = 0;

        to = followTarget;
        from = transform;
        tempIntial = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, transform.localEulerAngles.z);
        tempEnd = new Vector3(followTarget.localEulerAngles.x, followTarget.localEulerAngles.y, followTarget.localEulerAngles.z);
    }

    // Update is called once per frame
    void Update()
    {
        //transform.localEulerAngles = tempEnd;
        //transform.rotation = Quaternion.Slerp(transform.rotation, followTarget.rotation, timeCount * timeSpeed);
        //transform.localEulerAngles = new Vector3(0, Mathf.LerpAngle(transform.localEulerAngles.y, targetAngle, timeCount * timeSpeed), 0);

        
        //Debug.Log("from:" + Quaternion.Euler(tempIntial.x, tempIntial.y, tempIntial.z));
        
        //Debug.Log("to:" + followTarget.rotation);
        if (timeDelay >= timeDelaySet)
        {
            timeCount = timeCount + Time.deltaTime;
            //transform.rotation = followTarget.rotation;
            //tempEnd = new Vector3(followTarget.localEulerAngles.x, followTarget.localEulerAngles.y, followTarget.localEulerAngles.z);
        //} else
        //{
            transform.rotation = Quaternion.Slerp(transform.rotation, followTarget.rotation, timeCount * timeSpeed);
            transform.position = new Vector3(Mathf.Lerp(transform.position.x, followTarget.position.x, timeCount * timeSpeed),
                                            transform.position.y,
                                            Mathf.Lerp(transform.position.z, followTarget.position.z, timeCount * timeSpeed));
        }

        if ((transform.rotation == followTarget.rotation) 
        && (transform.position.x == followTarget.position.x) 
        && (transform.position.z == followTarget.position.z))
        {
            timeDelay = 0;
            timeCount = 0;
        } else
        {
            timeDelay = timeDelay + Time.deltaTime;
        }

    }
}   
