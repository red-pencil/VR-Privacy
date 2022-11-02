using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class changeText : MonoBehaviour
{
    public GameObject icon;
    public bool iconOnly = false;
    public Text targetText;
    public string[] textGroup;
    public bool displayOn;

    private IEnumerator coroutine;

    public float timeInterval;
    public float timeCount;

    
    void Start()
    {
        //Start the coroutine we define below named ExampleCoroutine.
        //StartCoroutine(ShowIconText(1));
       
        timeCount = timeInterval*(textGroup.Length + 3);
        Debug.Log("Start Func Time = " + Time.time + "!" + timeCount);
    }

    void FixedUpdate()
    {
        if (displayOn)
        {
            StartCoroutine(ShowIconText(timeInterval));
            displayOn = false;
        }

        //StartCoroutine(ShowIconText(2));

        if (!iconOnly)
        {
            timeCount = timeCount + Time.deltaTime;
            if (timeCount > timeInterval*(textGroup.Length + 3))
            {
                timeCount = 0;
                StartCoroutine(ShowIconText(timeInterval));
            }

        } else {
            targetText.text = "";
        }
        

    }

    IEnumerator ShowIconText(float waitTime = 2.0f)
    {   

        Debug.Log("Time Interval = " + waitTime);
        icon.SetActive(true);
        targetText.text = "";

        Debug.Log("Icon End Time = " + Time.time + "!" + timeCount);
        yield return new WaitForSeconds(waitTime);
        Debug.Log("Text Start Time = " + Time.time + "!" + timeCount);

        icon.SetActive(false);
        
        for (int i=0; i<textGroup.Length; i++)
        {
            targetText.text = textGroup[i];
            yield return new WaitForSeconds(waitTime);
        }

        Debug.Log("Text End Time = " + Time.time + "!" + timeCount);
        yield return new WaitForSeconds(waitTime);
        Debug.Log("Icon Start Time = " + Time.time + "!" + timeCount);

        icon.SetActive(true);
        targetText.text = "";


    }

}
