using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rotateDemoTwo : MonoBehaviour
{

    public enum myEnum // custom enumeration
    {
    mathf_lerp_set_angle,
    mathf_lerp_set_min_max,
    quaternion_lerp,
    quaternion_slerp
    };

    public myEnum selectMethod;

    public Transform rotateTarget;

    public float targetAngle;
    public int animationSpeed;

    public float minimum = -90F;
    public float maximum =  90F;
    public float timeSpeed=0.5f;

    static float t = 0.0f;

    float timeCount = 0.0f;
    public Transform from;
    public Transform to;
    

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        switch (selectMethod)
        {
            case myEnum.mathf_lerp_set_angle:
                rotateTarget.localEulerAngles = new Vector3(0, Mathf.LerpAngle(rotateTarget.localEulerAngles.y, targetAngle, t), 0);
                t += timeSpeed * Time.deltaTime;

                break;

            case myEnum.mathf_lerp_set_min_max:
                rotateTarget.localEulerAngles = new Vector3(0, Mathf.LerpAngle(minimum, maximum, t), 0);
                t += timeSpeed * Time.deltaTime;
                if (t > 1.0f)
                {
                    float temp = maximum;
                    maximum = minimum;
                    minimum = temp;
                    t = 0.0f;
                }

                break;

            case myEnum.quaternion_lerp:
                transform.rotation = Quaternion.Lerp(from.rotation, to.rotation, timeCount * timeSpeed);
                timeCount = timeCount + Time.deltaTime;
                if (timeCount*timeSpeed > 1.0f)
                {
                    timeCount = 0;
                }

                break;

             case myEnum.quaternion_slerp:
                transform.rotation = Quaternion.Slerp(from.rotation, to.rotation, timeCount * timeSpeed);
                timeCount = timeCount + Time.deltaTime;
                if (timeCount*timeSpeed > 1.0f)
                {
                    timeCount = 0;
                }

                break;

        }

        

        

        

        //for (int r = animationSpeed; r >0 ; r -= 1)
        //{
        //    rotateTarget.localEulerAngles = new Vector3(0, Mathf.LerpAngle(rotateTarget.localEulerAngles.y, targetAngle, 5f / animationSpeed), 0);
        //}
        
    }
}
