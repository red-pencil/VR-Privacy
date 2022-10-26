using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class aniControl : MonoBehaviour
{

    PlayableDirector ani;
    

    // Start is called before the first frame update
    void Start()
    {
        ani = GetComponent<PlayableDirector>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            Debug.Log((ani.state == PlayState.Playing));
            Debug.Log((ani.state == PlayState.Paused));

            if (ani.state == PlayState.Playing)
            {
                ani.Pause();
            } else if (ani.state == PlayState.Paused)
            {
                ani.Play();
            }
        }
    }



}
