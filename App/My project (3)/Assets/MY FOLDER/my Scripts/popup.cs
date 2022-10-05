using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class popup : MonoBehaviour
{
    public bool inView;
    public float targetTimeSpeed;
    public GameObject targetBoard;

    static float t = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        targetBoard.transform.localScale = new Vector3 (0.01f, 0.01f, 0.01f);
        inView = false;

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Input.GetKeyDown(KeyCode.M)) 
        {
            if (!inView)
            {
                UIAppear(targetBoard, targetTimeSpeed);
            } else
            {
                UIDisappear(targetBoard, targetTimeSpeed);
            }

            inView = !inView;
        
        }

        
    }

    void UIAppear(GameObject board, float timeSpeed)
    {
        for (t=0; t<=1; t += timeSpeed * Time.deltaTime)
        {
            float scale;
            scale = Mathf.Lerp(0, 1, t);
            board.transform.localScale = new Vector3 (scale, scale, scale);
        }
        
    }

    void UIDisappear(GameObject board, float timeSpeed)
    {
        for (t=0; t<=1; t += timeSpeed * Time.deltaTime)
        {
            float scale;
            scale = Mathf.Lerp(1, 0, t);
            board.transform.localScale = new Vector3 (scale, scale, scale);
        }
        
    }
}
