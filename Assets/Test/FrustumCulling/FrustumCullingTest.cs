using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrustumCullingTest : MonoBehaviour
{
    [SerializeField] GameObject m_obj;
    private void Update()
    {
        Vector3 min = m_obj.transform.position - transform.localScale * 0.5f;
        Vector3 max = m_obj.transform.position + transform.localScale * 0.5f;
        bool isOut = Camera.main.FrustumCullingInWorld(min, max);
        Debug.Log(min + " " + max + " " + isOut);
    }
}
