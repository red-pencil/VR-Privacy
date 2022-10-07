using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEnter : MonoBehaviour
{
    private bool playerInBounds;
    public Transform player;//����
    public GameObject cat;//cat
    public float speed = 0.1f;//�ƶ������ᣬֵԽС���ƶ�Խƽ��
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
                //to do���������ƶ�����
                anim.Play("Base Layer.new");
            }
            //to do��������վ������
            //anim.Play("Base Layer.idle");
            //���Ƴ���ĳ���
            cat.transform.LookAt(player.position);
        }
    }
    //���Ƴ����λ��ƽ���ƶ�
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
