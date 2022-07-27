using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class outdoorTrigger : MonoBehaviour
{

    public Text targetText;
    public string indicator;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerStay(Collider other)
    {
        string playerName = other.name;
        //Debug.Log(playerName);

        switch (playerName)
        {
            case "DummyPlayer":
                targetText.text = indicator;
                break;


        }

    }

    private IEnumerator OnTriggerExit(Collider other)
    {
        yield return new WaitForSeconds(3);
        targetText.text = null;
    }

        
}
