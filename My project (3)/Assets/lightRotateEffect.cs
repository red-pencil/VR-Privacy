using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lightRotateEffect : MonoBehaviour
{
    public Vector3 initialTransfom;
    public float speed;

    public enum MyEnum // custom enumeration
    {
    XAxis,
    YAxis,
    ZAxis
    };

    public MyEnum selectAxis;


    // Start is called before the first frame update
    void Start()
    {
        //this.transform.Rotate(90, 0, 0);
        //speed = 100;
        //switch (selectAxis)
        //{
        //    case MyEnum.XAxis:
        //        this.transform.localEulerAngles = initialTransfom;
        //        break;

        //    case MyEnum.YAxis:
        //        //this.transform.Rotate(0, speed*Time.deltaTime, 0);
        //        this.transform.localPosition = initialTransfom;
        //        break;

        //    case MyEnum.ZAxis:
        //        //this.transform.Rotate(0, 0, speed*Time.deltaTime);
        //        break;

        //    default:
        //        break;
        //}

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //Debug.Log(selectAxis);
        switch (selectAxis)
        {
            case MyEnum.XAxis:
                this.transform.Rotate(speed*Time.deltaTime, 0, 0);
                break;

            case MyEnum.YAxis:
                //this.transform.Rotate(0, speed*Time.deltaTime, 0);
                this.transform.localPosition= new Vector3 (40*Mathf.Sin(10*Time.time) * speed + initialTransfom.x, initialTransfom.y, initialTransfom.z);
                break;

            case MyEnum.ZAxis:
                this.transform.Rotate(0, 0, speed*Time.deltaTime);
                break;

            default:
                break;
        }


        
       
    }
}
