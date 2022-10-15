using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rotateDemo : MonoBehaviour
{

    public Transform A, B, C, D;
    Quaternion q1 = Quaternion.identity;

    public float angleOne, angleTwo;
    public Vector3 axis = Vector3.zero;
    public Vector3 eulerAngles;



    void Start()
    {

    }

    void Update()
    {
        q1.SetFromToRotation(A.position, B.position);
        C.rotation = q1;

        D.rotation = Quaternion.FromToRotation(A.position, B.position) * D.rotation;

        Debug.DrawLine(Vector3.zero, A.position, Color.red);
        Debug.DrawLine(Vector3.zero, B.position, Color.red);
        Debug.DrawLine(C.position, C.position + new Vector3(0,10f,0), Color.blue);
        Debug.DrawLine(C.position, C.TransformPoint(Vector3.up * 5f), Color.green);
        Debug.DrawLine(C.position, C.TransformPoint(Vector3.forward * 5f), Color.red);
        Debug.DrawLine(D.position, D.position + new Vector3(0,10f,0), Color.blue);
        Debug.DrawLine(D.position, D.TransformPoint(Vector3.up * 5f), Color.green);

        

        q1.ToAngleAxis(out angleOne, out axis);
        eulerAngles = q1.eulerAngles;

        
       
    }
}
