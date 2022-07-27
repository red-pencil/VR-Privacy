using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Locomotion : MonoBehaviour
{
    
    public float speed = 10.0f;
    public float rotationSpeed = 100.0f;

    float horizontalSpeed = 2.0f;
    float verticalSpeed = 2.0f;

    float smooth = 5.0f;
    float tiltAngle = 60.0f;

    public GameObject camera;

    Rigidbody m_Rigidbody;
    public float m_Thrust = 200f;

    // Start is called before the first frame update
    void Start()
    {
         //Fetch the Rigidbody from the GameObject with this script attached
        m_Rigidbody = GetComponent<Rigidbody>();

        
    }

    // Update is called once per frame
    void Update()
    {


        float translation = Input.GetAxis("Vertical") * speed;
        float rotation = Input.GetAxis("Horizontal") * rotationSpeed;

        // Make it move 10 meters per second instead of 10 meters per frame...
        translation *= Time.deltaTime;
        rotation *= Time.deltaTime;

        // Move translation along the object's z-axis
        transform.Translate(0, 0, translation);

        // Rotate around our y-axis
        transform.Rotate(0, rotation, 0);
        
        

        // Get the mouse delta. This is not in the range -1...1
        //float h = horizontalSpeed * Input.GetAxis("Mouse X");
        float h = 0.0f;
        float v = -1 *verticalSpeed * Input.GetAxis("Mouse Y");

        float tiltAroundZ = Input.GetAxis("Mouse Y") * tiltAngle;
        float tiltAroundX = Input.GetAxis("Mouse X") * tiltAngle;

        Quaternion target = Quaternion.Euler(tiltAroundX, 0, tiltAroundZ);

        camera.transform.Rotate(v, h, 0);

        Debug.Log("X:"+Input.GetAxis("Mouse X"));
        Debug.Log("Y:"+Input.GetAxis("Mouse Y"));
        Debug.Log(camera.transform.rotation.eulerAngles);

        if (Input.GetKeyDown("space"))
        {
            //Apply a force to this Rigidbody in direction of this GameObjects up axis
            m_Rigidbody.AddForce(transform.up * m_Thrust);
        }


        //if (Input.Keypress(Keycode.Space))
        //{
        //    camera.transform.rotation.SetLookRotation(vector3.forward, vector3.up)
        //}
    }
}
