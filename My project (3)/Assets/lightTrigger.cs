using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lightTrigger : MonoBehaviour
{

    public lightRotateEffect[] lightSwitch;
    //public lightRotateEffect lightSwitch;
    public string triggerName;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        triggerName = other.name;
        if (triggerName == "player" && other.GetComponentsInChildren<lightRotateEffect>().Length != 0)
        {
            
            lightSwitch = other.GetComponentsInChildren<lightRotateEffect>();
            Debug.Log("Pass the value" + lightSwitch);
            
            
            for (int i=0; i < lightSwitch.Length; i++)
            {
               lightSwitch[i].enabled = !lightSwitch[i].enabled;
               
               //GameObject rb = lightSwitch[i].gameObject;
               //if (rb.activeInHierarchy)
               //{
               //rb.SetActive(false);
               //} else
               //{
               //rb.SetActive(true);
               //}
            }

        }
        


    }
}
