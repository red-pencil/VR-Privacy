using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class appearTigger : MonoBehaviour
{

    public GameObject targetObject;

    // Start is called before the first frame update
    void Start()
    {
        targetObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerStay(Collider other)
    {
        string playerName = other.name;

        switch (playerName)
        {
            case "player":
                targetObject.SetActive(true);
                targetObject.transform.Rotate(new Vector3 (0, 30, 0) * Time.deltaTime);
            break;

        }
    }

}
