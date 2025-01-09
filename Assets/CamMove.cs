using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamMove : MonoBehaviour
{
    [SerializeField] float m_Speed;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 moveDir = Vector3.zero;
        if(Input.GetKey(KeyCode.W))
        {
            moveDir += transform.forward;
        }
        if (Input.GetKey(KeyCode.A))
        {
            moveDir -= transform.right;
        }
        if (Input.GetKey(KeyCode.S))
        {
            moveDir -= transform.forward;
        }
        if (Input.GetKey(KeyCode.D))
        {
            moveDir += transform.right;
        }
        if (Input.GetKey(KeyCode.Space))
        {
            moveDir += transform.up;
        }
        if (Input.GetKey(KeyCode.LeftControl))
        {
            moveDir -= transform.up;
        }

        if (moveDir != Vector3.zero)
        {
            moveDir.Normalize();
            transform.position += moveDir * Time.deltaTime * m_Speed;
        }
    }
}
