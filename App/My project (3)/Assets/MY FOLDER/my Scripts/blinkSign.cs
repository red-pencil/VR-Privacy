using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class blinkSign : MonoBehaviour
{

    public GameObject targetSign;

    public float timeSpeed;
    static float t = 0.0f;
    public float timeDelay;
    public float timeCount;

    // Start is called before the first frame update
    void Start()
    {
        targetSign.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

        if (targetSign.activeSelf)
        {
            float scale;
            scale = Mathf.Lerp(0, 1, t);
            targetSign.transform.localScale = new Vector3 (scale, scale, scale);
        }

        t += timeSpeed * Time.deltaTime;

        if (t>3)
        { t = 0; }
    }

    private void OnTriggerEnter(Collider other) {

        targetSign.SetActive(true);

    }

    private void OnTriggerExit(Collider other) {

        targetSign.SetActive(false);

    }
}
