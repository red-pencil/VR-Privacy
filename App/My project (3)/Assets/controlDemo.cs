using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class controlDemo : MonoBehaviour
{

    public float horizontalAxis;
    public float verticalAxis;
    public float mouseAxisX;
    public float mouseAxisY;
    private Rigidbody rb;
    public Vector3 mousePos;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Debug.Log("Screen Width : " + Screen.width);
        Debug.Log("Screen Height : " + Screen.height);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        horizontalAxis = Input.GetAxis("Horizontal");
        verticalAxis = Input.GetAxis("Vertical");
        mouseAxisX = Input.GetAxis("Mouse X");
        mouseAxisY = Input.GetAxis("Mouse Y");
        mousePos = Input.mousePosition;

    }
}
