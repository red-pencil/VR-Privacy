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
        StartCoroutine(ShowIconText(timeInterval));
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
            if (timeCount > 2*(textGroup.Length + 3))
            {
                timeCount = 0;
                StartCoroutine(ShowIconText(timeInterval));
            }

        }
        

    }

    IEnumerator ShowIconText(float waitTime = 2.0f)
    {
        icon.SetActive(true);
        targetText.text = "";

        Debug.Log("Time = " + Time.time);
        yield return new WaitForSeconds(waitTime);
        Debug.Log("Time = " + Time.time);

        icon.SetActive(false);
        
        for (int i=0; i<textGroup.Length; i++)
        {
            targetText.text = textGroup[i];
            yield return new WaitForSeconds(waitTime);
        }

        Debug.Log("Time = " + Time.time);
        yield return new WaitForSeconds(waitTime);
        Debug.Log("Time = " + Time.time);

        icon.SetActive(true);
        targetText.text = "";


    }

}
