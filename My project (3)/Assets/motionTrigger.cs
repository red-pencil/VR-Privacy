using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class motionTrigger : MonoBehaviour
{


    public Vector3 rotateAxis = new Vector3 (0.0f, 0.0f, 0.0f);
    public GameObject rotateObjectOne;
    public int rotateAngleOne;
    public GameObject rotateObjectTwo;
    public int rotateAngleTwo;
    public float doorAngleDisplayOne;
    public float doorAngleDisplayTwo;


    public Transform from;
    public Transform to;
    private float timeCount = 0.0f;


    // Start is called before the first frame update
    void Start()
    {
        from = rotateObjectOne.transform;
        to = rotateObjectOne.transform;
       
    }

    // Update is called once per frame
    void Update()
    {
        doorAngleDisplayOne = rotateObjectOne.transform.rotation.eulerAngles.y;
        doorAngleDisplayOne = (doorAngleDisplayOne <= 180)? doorAngleDisplayOne: doorAngleDisplayOne - 360;
        doorAngleDisplayTwo = rotateObjectTwo.transform.rotation.eulerAngles.y;
        doorAngleDisplayTwo = (doorAngleDisplayTwo <= 180)? doorAngleDisplayTwo: doorAngleDisplayTwo - 360;


        //transform.rotation = Quaternion.Slerp(from.rotation, to.rotation, timeCount);
        //timeCount = timeCount + Time.deltaTime;

        //Debug.Log(doorAngleDisplayOne);
        //Debug.Log(doorAngleDisplayTwo);
    }

    void OnTriggerEnter(Collider other)
    {
        string playerName = other.name;
        
        

        Debug.Log("Enter!");

        switch (playerName)
        {
            case "player":
                if (doorAngleDisplayOne != rotateAngleOne)
                {
                //rotateObjectOne.transform.Rotate(Vector3.up, rotateAngleOne, Space.Self);
                from.rotation = Quaternion.AngleAxis(0, Vector3.up);
                to.rotation = Quaternion.AngleAxis(rotateAngleOne, Vector3.up);
                }
                
                if ((doorAngleDisplayTwo < rotateAngleTwo)&&(doorAngleDisplayTwo >= 0.0f))
                {
                rotateObjectTwo.transform.Rotate(Vector3.up, rotateAngleTwo, Space.Self);
                }

                break;

        }
        
    }

    private IEnumerator OnTriggerExit(Collider other)
    {
        string playerName = other.name;
        if (playerName == "player")
        {
        Debug.Log("Exit!");
        yield return new WaitForSeconds(3);
        
        rotateObjectOne.transform.Rotate(Vector3.up, -1*rotateAngleOne, Space.Self);
        rotateObjectTwo.transform.Rotate(Vector3.up, -1*rotateAngleTwo, Space.Self);
        }
    }
}
