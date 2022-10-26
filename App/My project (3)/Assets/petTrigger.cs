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
            ani.Pause();
            StartCoroutine(aniResume(3));
        }

    }

    IEnumerator aniResume(float waitTime = 3.0f) {

        yield return new WaitForSeconds(waitTime);
        ani.Resume();
    }
}
