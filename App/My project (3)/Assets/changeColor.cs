using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class changeColor : MonoBehaviour
{

    public Light targetLight;
    public Color targetColor;
    public float timeDelay;
    public float timeCount;

    void Start()
    {
        
    }


    void Update()
    {
        timeCount = timeCount + Time.deltaTime;
        if (timeCount>timeDelay)
        {
            targetLight.color = targetColor;
        }
    }
}
