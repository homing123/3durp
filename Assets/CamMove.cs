using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class CamMove : MonoBehaviour
{
    //CharacterMoveMode
    [SerializeField] Character m_Character;
    [SerializeField] float m_Height;
    [SerializeField] float m_Distance;
    [SerializeField] float m_RotX;

    //CameraMoveMode
    [SerializeField] float m_Speed;


    [SerializeField] float m_MouseSensitivity; //마우스감도
    float m_CurMouseX;
    float m_CurMouseY;

    // Start is called before the first frame update
    void Start()
    {
        transform.rotation = Quaternion.Euler(m_RotX, m_Character.transform.eulerAngles.y, 0);
    }

    // Update is called once per frame
    void Update()
    {
        CharacterMoveMode();
    }

    void CharacterMoveMode()
    {
        if(m_Character == null)
        {
            return;
        }
        float rotY = transform.eulerAngles.y;
        rotY += Input.GetAxisRaw("Mouse X") * m_MouseSensitivity * Time.deltaTime;

        //m_CurMouseY -= Input.GetAxisRaw("Mouse Y") * m_MouseSensitivity * Time.deltaTime;

        //m_CurMouseY = Mathf.Clamp(m_CurMouseY, -90f, 90f); //Clamp를 통해 최소값 최대값을 넘지 않도록함

        Vector3 chaPos = m_Character.transform.position;
        Vector3 camPos;
        camPos.y = chaPos.y + m_Height;
        Vector2 xz = new Vector2(Mathf.Sin(Mathf.Deg2Rad * (rotY)), Mathf.Cos(Mathf.Deg2Rad * (rotY))) * m_Distance;
        camPos.x = -xz.x;
        camPos.z = -xz.y;
        transform.position = camPos + chaPos;
        transform.rotation = Quaternion.Euler(m_RotX, rotY, 0);



    }
    void CamerMoveMode()
    {
        if (GameManager.m_InputMode == GameManager.InputMode.Camera)
        {
            Vector3 moveDir = Vector3.zero;
            if (Input.GetKey(KeyCode.W))
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
}
