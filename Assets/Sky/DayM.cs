using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class DayM : MonoBehaviour
{
    public static DayM Ins;
    [SerializeField][Range(0,1)] float m_TimeSpeed;
    [SerializeField][Range(0,1)] float m_DayTime;

    [SerializeField] Material m_SkyboxMat;
    [SerializeField] Light m_SunLight;
    [SerializeField] Light m_MoonLight;

    [SerializeField] float m_SunriseHeight;
    [SerializeField] float m_SunriseMulValue;
    public float DayTime
    {
        get {  return m_DayTime; }
    }
    // Start is called before the first frame update
    Color m_SunLightOriginColor;
    float m_SunLightOriginIntensity;
    float m_MoonLightOriginIntensity;
    void Start()
    {
        m_SunLightOriginColor = m_SunLight.color;
        m_SunLightOriginIntensity = m_SunLight.intensity;
        m_MoonLightOriginIntensity = m_MoonLight.intensity;
    }

    // Update is called once per frame
    void Update()
    {
        AddTime(m_TimeSpeed * Time.deltaTime);

        float timeToRad = m_DayTime * Mathf.PI * 2;
        float sunRotAxisX = 45 * Mathf.Deg2Rad;
        Vector3 sunPos = new Vector3(Mathf.Cos(timeToRad), Mathf.Sin(timeToRad), 0) * 2;
        float cosAxisX = Mathf.Cos(sunRotAxisX);
        float sinAxisX = Mathf.Sin(sunRotAxisX);
        Matrix4x4 sunAxisXRotMat = Matrix4x4.identity;
        sunAxisXRotMat.m11 = cosAxisX;
        sunAxisXRotMat.m12 = -sinAxisX;
        sunAxisXRotMat.m21 = sinAxisX;
        sunAxisXRotMat.m22 = cosAxisX;
        sunPos = sunAxisXRotMat * sunPos;

        timeToRad += Mathf.PI;
        float moonRotAxisX = 45 * Mathf.Deg2Rad;
        Vector3 moonPos = new Vector3(Mathf.Cos(timeToRad), Mathf.Sin(timeToRad), 0) * 2;
        float moonCosAxisX = Mathf.Cos(moonRotAxisX);
        float moonSinAxisX = Mathf.Sin(moonRotAxisX);
        Matrix4x4 moonAxisXRotMat = Matrix4x4.identity;
        moonAxisXRotMat.m11 = moonCosAxisX;
        moonAxisXRotMat.m12 = -moonSinAxisX;
        moonAxisXRotMat.m21 = moonSinAxisX;
        moonAxisXRotMat.m22 = moonCosAxisX;
        moonPos = moonAxisXRotMat * moonPos;
        //float sunriseIntensity = 1 - Mathf.Clamp01(Mathf.Abs(m_SunriseHeight - sunPos.y) * m_SunriseMulValue); 
        float sunriseIntensity = Mathf.Clamp01((0.5f - Mathf.Abs(sunPos.y)) / 0.2f); 
        if(m_DayTime > 0.9f || m_DayTime < 0.4f)
        {
            sunriseIntensity *= 0.5f;
        }
        if (sunPos.y < 0.05f)
        {
            m_SunLight.transform.position = new Vector3(sunPos.x, 0.05f, sunPos.z);
            m_SunLight.transform.LookAt(Vector3.zero);
            //0.05 부터 -0.3까지 줄어든다.
            //Color curColor = (sunPos.y - (-0.3f)) / (0.05f - (-0.3f)) * m_SunLightOriginColor;
            float intensity = (sunPos.y - (-0.3f)) / (0.05f - (-0.3f)) * m_SunLightOriginIntensity;
            m_SunLight.intensity = intensity;
        }
        else
        {
            m_SunLight.transform.position = sunPos;
            m_SunLight.transform.LookAt(Vector3.zero);
            m_SunLight.intensity = m_SunLightOriginIntensity;
        }
        if(moonPos.y < 0.05f)
        {
            m_MoonLight.transform.position = new Vector3(moonPos.x, 0.05f, moonPos.z);
            m_MoonLight.transform.LookAt(Vector3.zero);
            //0.05 부터 -0.3까지 줄어든다.
            //Color curColor = (moonPos.y - (-0.3f)) / (0.05f - (-0.3f)) * m_SunLightOriginColor;
            float intensity = (moonPos.y - (-0.3f)) / (0.05f - (-0.3f)) * m_MoonLightOriginIntensity;
            m_MoonLight.intensity = intensity;
        }
        else
        {
            m_MoonLight.transform.position = moonPos;
            m_MoonLight.transform.LookAt(Vector3.zero);
            m_MoonLight.intensity = m_MoonLightOriginIntensity;
        }


        float nightIntensity = Mathf.Clamp01(-sunPos.y * 1.25f + 0.5f); // -0.4~0.4 => 1~0
        m_SkyboxMat.SetVector("_SunPos", sunPos);
        m_SkyboxMat.SetVector("_MoonPos", moonPos);
        m_SkyboxMat.SetFloat("_NightIntensity", nightIntensity);
        m_SkyboxMat.SetFloat("_SunriseIntensity", sunriseIntensity);
    }
    public void AddTime(float time)
    {
        m_DayTime += time;
        m_DayTime -= Mathf.FloorToInt(m_DayTime);
        m_SkyboxMat.SetFloat("_DayTime", m_DayTime);
    }
}
