using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{

    [SerializeField] float m_Speed;
    [SerializeField] float m_RotSpeed;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.m_InputMode == GameManager.InputMode.Character)
        {
            Vector3 moveDir = Vector3.zero;
            if (Input.GetKey(KeyCode.W))
            {
                moveDir += Camera.main.transform.forward;
            }
            if (Input.GetKey(KeyCode.A))
            {
                moveDir -= Camera.main.transform.right;
            }
            if (Input.GetKey(KeyCode.S))
            {
                moveDir -= Camera.main.transform.forward;
            }
            if (Input.GetKey(KeyCode.D))
            {
                moveDir += Camera.main.transform.right;
            }
            moveDir.y = 0;

            

            if (Input.GetKey(KeyCode.Space))
            {

            }

            if (moveDir != Vector3.zero)
            {
                //È¸Àü
                float camRotY = Camera.main.transform.eulerAngles.y;
                float curRotY = transform.eulerAngles.y;
                if (camRotY != curRotY)
                {
                    float angleDis = Util.GetAngleDis(curRotY, camRotY);
                    float curRotSpeed = m_RotSpeed * Time.deltaTime;
                    if (Mathf.Abs(angleDis) < curRotSpeed)
                    {
                        transform.eulerAngles = new Vector3(0, camRotY, 0);
                    }
                    else
                    {
                        float dir = angleDis > 0 ? 1 : -1;
                        transform.eulerAngles = new Vector3(0, curRotY + curRotSpeed * dir, 0);
                    }
                }
                moveDir.Normalize();
                transform.position += moveDir * Time.deltaTime * m_Speed;
            }
        }
    }
}
