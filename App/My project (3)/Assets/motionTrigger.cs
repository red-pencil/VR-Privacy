using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class motionTrigger : MonoBehaviour
{


    public Vector3 rotateAxis = new Vector3 (0.0f, 0.0f, 0.0f);
    public GameObject rotateTargetOne;
    public int rotateAngleOne;
    public GameObject rotateTargetTwo;
    public int rotateAngleTwo;
    public float doorAngleDisplayOne;
    public float doorAngleDisplayTwo;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        doorAngleDisplayOne = rotateTargetOne.transform.rotation.eulerAngles.y;
        doorAngleDisplayOne = (doorAngleDisplayOne <= 180)? doorAngleDisplayOne: doorAngleDisplayOne - 360;
        doorAngleDisplayTwo = rotateTargetTwo.transform.rotation.eulerAngles.y;
        doorAngleDisplayTwo = (doorAngleDisplayTwo <= 180)? doorAngleDisplayTwo: doorAngleDisplayTwo - 360;

        Debug.Log(doorAngleDisplayOne);
        Debug.Log(doorAngleDisplayTwo);
    }

    void OnTriggerEnter(Collider other)
    {
        string playerName = other.name;

        switch (playerName)
        {
            case "DummyPlayer":
                if (doorAngleDisplayOne != rotateAngleOne)
                {
                rotateTargetOne.transform.Rotate(Vector3.up, rotateAngleOne, Space.Self);
                }
                
                if ((doorAngleDisplayTwo < rotateAngleTwo)&&(doorAngleDisplayTwo >= 0.0f))
                {
                rotateTargetTwo.transform.Rotate(Vector3.up, rotateAngleTwo, Space.Self);
                }

                break;

        }
        
    }

    private IEnumerator OnTriggerExit(Collider other)
    {
        string playerName = other.name;
        if (playerName == "DummyPlayer")
        {
        yield return new WaitForSeconds(3);

        rotateTargetOne.transform.Rotate(Vector3.up, -1*rotateAngleOne, Space.Self);
        rotateTargetTwo.transform.Rotate(Vector3.up, -1*rotateAngleTwo, Space.Self);
        }
    }
}
