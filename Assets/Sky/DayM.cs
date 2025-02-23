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
    public float DayTime
    {
        get {  return m_DayTime; }
    }
    // Start is called before the first frame update
    void Start()
    {
        
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


        m_SunLight.transform.position = sunPos;
        m_SunLight.transform.LookAt(Vector3.zero);
        m_SunLight.gameObject.SetActive(m_SunLight.transform.position.y > 0);
        m_MoonLight.gameObject.SetActive(!m_SunLight.gameObject.activeSelf);
        m_MoonLight.transform.position = moonPos;
        m_MoonLight.transform.LookAt(Vector3.zero);

        m_SkyboxMat.SetVector("_SunPos", sunPos);
        m_SkyboxMat.SetVector("_MoonPos", moonPos);


    }
    public void AddTime(float time)
    {
        m_DayTime += time;
        m_DayTime -= Mathf.FloorToInt(m_DayTime);
        m_SkyboxMat.SetFloat("_DayTime", m_DayTime);
    }
}
