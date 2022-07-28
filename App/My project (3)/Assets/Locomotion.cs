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

    public GameObject m_head;

    Rigidbody m_Rigidbody;
    public float m_Thrust = 300f;

    public float ratioH;
    public float ratioV;
    public int rangeH;
    public int rangeV;

    // Start is called before the first frame update
    void Start()
    {
         //Fetch the Rigidbody from the GameObject with this script attached
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Thrust = 300f;

        
        rangeH = 30;
        rangeV = -45;
        
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
        
        

        // Camera gaze direction controlled by mouse position / display size
        // gaze direction typically -45 to +45

        Vector3 mousePos = Input.mousePosition;
        ratioH = 2 * mousePos.x / Screen.width - 1;
        ratioV = 2 * mousePos.y / Screen.height - 1;

        m_head.transform.localEulerAngles = new Vector3( ratioV * rangeV, ratioH * rangeH, 0);
    }
}
