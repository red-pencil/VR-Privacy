using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEnter : MonoBehaviour
{
    private bool playerInBounds;
    public Transform player;//主角
    public GameObject cat;//cat
    public float speed = 0.1f;//移动的阻尼，值越小，移动越平缓
    Animator anim;
    void Awake()
    {
        anim = cat.GetComponent<Animator>();
        Debug.Log("enter3");
    }
    void Update()
    {
        if (playerInBounds)
        {
            //effects to apply go here.
            Debug.Log("inbound");
            if (Vector3.Distance(player.position, cat.transform.position) > 1f)
            {
                PetSmothFlow();
                //to do。。播放移动动画
                anim.Play("Base Layer.new");
            }
            //to do。。播放站立动画
            //anim.Play("Base Layer.idle");
            //控制宠物的朝向
            cat.transform.LookAt(player.position);
        }
    }
    //控制宠物的位置平滑移动
    void PetSmothFlow()
    {
        cat.transform.position = Vector3.Lerp(cat.transform.position, player.position, Time.deltaTime * speed);
    }


    void OnTriggerStay(Collider other)
    {
        if (other.gameObject.name == "player")
        {
            playerInBounds = true;
            Debug.Log("enter");
        }
        Debug.Log(other.gameObject.name);
        Debug.Log("enter2");
    }
}
