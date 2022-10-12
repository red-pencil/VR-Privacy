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

        StartCoroutine(ShowIconText(2));

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
            yield return new WaitForSeconds(1);
        }

        Debug.Log("Time = " + Time.time);
        yield return new WaitForSeconds(waitTime);
        Debug.Log("Time = " + Time.time);

        icon.SetActive(true);
        targetText.text = "";


    }

}
