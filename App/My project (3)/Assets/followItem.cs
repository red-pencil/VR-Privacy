using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class followItem : MonoBehaviour
{

    public enum myEnum // custom enumeration
    {
    not_save,
    save_target_rotation,
    save_self_rotation
    };

    public myEnum selectMethod;

    public Transform followTarget;
    public float timeCount = 0.0f;
    public float timeSpeed = 0.1f;
    public Quaternion savedTargetRotation;
    public Quaternion savedSelfRotation;

    public float w, x, y, z;


    // Start is called before the first frame update
    void Start()
    {

        savedTargetRotation = new Quaternion (followTarget.rotation.x, followTarget.rotation.y, followTarget.rotation.z, followTarget.rotation.w);
        savedSelfRotation = new Quaternion (transform.rotation.x, transform.rotation.y, transform.rotation.z, transform.rotation.w);
    }

    // Update is called once per frame
    void FixedUpdate()
    {   
        switch (selectMethod)
        {
            case myEnum.not_save:

                Debug.Log(followTarget.rotation.eulerAngles.y - transform.rotation.eulerAngles.y);

                transform.rotation = Quaternion.Slerp(transform.rotation, followTarget.rotation, timeCount * timeSpeed);
                
                timeCount = timeCount + Time.deltaTime;

                if (Mathf.Abs(followTarget.rotation.eulerAngles.y - transform.rotation.eulerAngles.y) <0.001f)
                {
                    timeCount = 0;
                }

                break;


            
            case myEnum.save_target_rotation:

                if (savedTargetRotation != followTarget.rotation)
                {
                    savedTargetRotation = new Quaternion (followTarget.rotation.x, followTarget.rotation.y, followTarget.rotation.z, followTarget.rotation.w);
                    timeCount = 0;
                }

                transform.rotation = Quaternion.Slerp(transform.rotation, savedTargetRotation, timeCount * timeSpeed);

                timeCount = timeCount + Time.deltaTime;

                break;



            case myEnum.save_self_rotation:

                if (savedSelfRotation != transform.rotation)
                {
                    savedSelfRotation = new Quaternion (transform.rotation.x, transform.rotation.y, transform.rotation.z, transform.rotation.w);
                    timeCount = 0;
                }

                transform.rotation = Quaternion.Slerp(savedSelfRotation, followTarget.rotation, timeCount * timeSpeed * 100f);

                timeCount = timeCount + Time.deltaTime;

                break;



        }
        
        
    }




}
