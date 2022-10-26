using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class petTrigger : MonoBehaviour
{
    public GameObject aniControl;
    PlayableDirector ani;
    public string triggerName;
    


    void Start()
    {
        ani = aniControl.GetComponent<PlayableDirector>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other) {

        triggerName = other.name;

        if (other.name == "handTouchTrigger")
        {
            ani.Stop();
        }

    }
}
