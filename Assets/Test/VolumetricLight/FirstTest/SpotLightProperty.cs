using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpotLightProperty : MonoBehaviour
{
    Light m_Light;
    public Material m_Mat;

    float m_LastFar;
    float m_LastNear;
    float m_LastFov;

    private void Awake()
    {
        m_Light = GetComponent<Light>();
        m_LastFar = m_Light.range;
        m_LastFov = m_Light.spotAngle;
        m_LastNear = m_Light.shadowNearPlane;
        m_Mat.SetFloat("_SpotlightNear", m_Light.shadowNearPlane);
        m_Mat.SetFloat("_SpotlightFar", m_Light.range);
        m_Mat.SetFloat("_SpotlightFov", m_Light.spotAngle);
    }

    private void Update()
    {
        if (m_LastFar != m_Light.range || m_LastFov != m_Light.spotAngle || m_LastNear != m_Light.shadowNearPlane)
        {
            m_Mat.SetFloat("_SpotlightNear", m_Light.shadowNearPlane);
            m_Mat.SetFloat("_SpotlightFar", m_Light.range);
            m_Mat.SetFloat("_SpotlightFov", m_Light.spotAngle);
            m_LastFar = m_Light.range;
            m_LastFov = m_Light.spotAngle;
            m_LastNear = m_Light.shadowNearPlane;
        }
    }
}
