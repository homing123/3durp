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
        m_SunLight.transform.position = sunPos;
        m_SunLight.transform.LookAt(Vector3.zero);

    }
    public void AddTime(float time)
    {
        m_DayTime += time;
        m_DayTime -= Mathf.FloorToInt(m_DayTime);
        m_SkyboxMat.SetFloat("_DayTime", m_DayTime);
    }
}
