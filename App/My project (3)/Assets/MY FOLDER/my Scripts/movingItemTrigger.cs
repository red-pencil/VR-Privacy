using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class movingItemTrigger : MonoBehaviour
{

    public GameObject trigger;
    public float timeSpeed;
    public float timeCount;

    float originalHeight;
   


    // Start is called before the first frame update
    void Start()
    {
        originalHeight = gameObject.transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {

        if (trigger.GetComponent<triggerDetector>().triggerOn)
        {
            //gameObject.SetActive(true);
            //for (float timeCount = 0; timeCount * timeSpeed <1; timeCount = timeCount + Time.deltaTime)
            
            gameObject.transform.position = new Vector3 (gameObject.transform.position.x, Mathf.Lerp(10f, originalHeight, timeCount * timeSpeed), gameObject.transform.position.z);
            timeCount = timeCount + Time.deltaTime;
            
        } else
        {
            //gameObject.SetActive(false);
            gameObject.transform.position = new Vector3 (gameObject.transform.position.x, 10f, gameObject.transform.position.z);
            timeCount = 0;

        }

        
    }

}
