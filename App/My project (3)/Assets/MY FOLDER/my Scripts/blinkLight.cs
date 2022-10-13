using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class blinkLight : MonoBehaviour
{

    public Light targetLight;

    public Color targetColor;
    public Color originColor;


    // Start is called before the first frame update
    void Start()
    {
        originColor = targetLight.color;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter(Collider other) {

        targetLight.color = targetColor;

    }

    private void OnTriggerExit(Collider other) {

        targetLight.color = originColor;

    }
}
