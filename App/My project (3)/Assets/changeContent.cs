using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class changeContent : MonoBehaviour
{
    public GameObject icon;
    public Text targetText;
    public string[] textGroup;
    public bool displayOn;

    public float timeCount;

    public int count=0;
    public int textLen;

    // Start is called before the first frame update
    void Start()
    {
        icon.SetActive(true);
        targetText.text = "";
        textLen = textGroup.Length;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        
        if (Mathf.Abs(timeCount - (2.0f * (count + 1))) < 0.001f)
        {
            icon.SetActive(false);

            targetText.text = textGroup[count];

            Debug.Log("Time = " + timeCount);
            Debug.Log("Count = " + count);

            count = count + 1;
        }

        timeCount = timeCount + Time.deltaTime;

        if (count >= textLen)
        {
            count = 0;
            timeCount = 0;
        }
        
    }
}
