using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class changeText : MonoBehaviour
{
    public GameObject icon;
    public Text targetText;
    public string[] textGroup;
    public bool displayOn;

    private IEnumerator coroutine;

    
    void Start()
    {
        //Start the coroutine we define below named ExampleCoroutine.
        StartCoroutine(ShowIconText(2));
    }

    void FixedUpdate()
    {
        if (displayOn)
        {
            StartCoroutine(ShowIconText(2));
            displayOn = false;
        }

    }

    IEnumerator ShowIconText(float waitTime)
    {

        icon.SetActive(true);
        targetText.text = "";
        //Print the time of when the function is first called.
        Debug.Log("Started Coroutine at timestamp : " + Time.time);

        //yield on a new YieldInstruction that waits for 5 seconds.
        yield return new WaitForSeconds(waitTime);

        icon.SetActive(false);
        targetText.text = textGroup[0];
        //After we have waited 5 seconds print the time again.
        Debug.Log("Finished Coroutine at timestamp : " + Time.time);

        yield return new WaitForSeconds(waitTime);

        icon.SetActive(true);
        targetText.text = "";
         Debug.Log("Timestamp : " + Time.time);
    }
}
