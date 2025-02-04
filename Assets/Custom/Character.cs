using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum E_RunType
{
    slow,
    normal,
    fast,
    ninja
}
public enum E_Anim
{
    Idle,
    run
}
public class Character : MonoBehaviour
{

    [SerializeField] float m_Speed;
    [SerializeField] float m_RotSpeed;
    [SerializeField] float m_BendingRadius;
    [SerializeField] Animator m_Anim;
    [SerializeField] E_RunType m_RunType;

    E_Anim m_CurAnim = E_Anim.Idle;

    const string AnimTrigger_Idle = "Idle";
    const string AnimTrigger_NormalRun = "NormalRun";
    const string AnimTrigger_SlowRun = "SlowRun";
    const string AnimTrigger_FastRun = "FastRun";
    const string AnimTrigger_NinjaRun = "NinjaRun";

    // Start is called before the first frame update
    void Start()
    {

        GrassBendingM.Ins?.AddBending(transform, m_BendingRadius);
        
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
                    float angleDis = HMUtil.GetAngleDis(curRotY, camRotY);
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

                ChangeAnim(E_Anim.run);
            }
            else
            {
                ChangeAnim(E_Anim.Idle);
            }
        }
    }

    void ChangeAnim(E_Anim anim)
    {
        if(anim != m_CurAnim)
        {
            switch(anim)
            {
                case E_Anim.Idle:
                    m_Anim.SetTrigger(AnimTrigger_Idle);
                    break;
                case E_Anim.run:
                    switch(m_RunType)
                    {
                        case E_RunType.slow:
                            m_Anim.SetTrigger(AnimTrigger_SlowRun);
                            break;
                        case E_RunType.normal:
                            m_Anim.SetTrigger(AnimTrigger_NormalRun);
                            break;
                        case E_RunType.fast:
                            m_Anim.SetTrigger(AnimTrigger_FastRun);
                            break;
                        case E_RunType.ninja:
                            m_Anim.SetTrigger(AnimTrigger_NinjaRun);
                            break;
                    }
                    break;
            }
            m_CurAnim = anim;
        }
    }

}
