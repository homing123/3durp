using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScanEffectM : MonoBehaviour
{
    public static ScanEffectM Ins;
    [SerializeField] public Color m_Color;
    [SerializeField][Min(0)] public float m_Range;
    [SerializeField][Min(0)] public float m_TotalTime;
    [SerializeField][Min(0)] public float m_LineWidth;
    [SerializeField] public Color m_GridColor;
    [HideInInspector] public float m_CurTime;

    private void Awake()
    {
        Ins = this;
    }

    private void Update()
    {
        if(m_CurTime < m_TotalTime)
        {
            m_CurTime += Time.deltaTime;
        }
    }
    public bool IsActive()
    {
        return m_CurTime < m_TotalTime;
    }
    public void Scan()
    {
        m_CurTime = 0;
    }

}
