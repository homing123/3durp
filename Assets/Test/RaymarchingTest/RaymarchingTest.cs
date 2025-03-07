using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaymarchingTest : MonoBehaviour
{
    [SerializeField] Material m_Mat;
    [SerializeField] int m_Samples;
    private void Awake()
    {
        m_Mat = GetComponent<MeshRenderer>().material;
    }
    private void Update()
    {
        m_Mat.SetVector("_Position", transform.position);
        m_Mat.SetVector("_Scale", transform.localScale);
        m_Mat.SetInteger("_Samples", m_Samples);
    }
}
