using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightAnimation : MonoBehaviour
{
    Light[] m_Lights;
    LightAnimInfo[] m_AnimInfo;
    [SerializeField] float m_LightHeightAdd;
    public struct LightAnimInfo
    {
        public float yWaveHeight;
        public float yWaveSpeed;
        public float yWaveSeed;
        public float roundSpeed;
        public float rangeWaveSpeed;
        public float rangeWaveSeed;
        public float rangeWaveAmplitude;

        public bool colorWave;
        public float colorWaveSpeed;
        public float colorWaveRSeed;
        public float colorWaveGSeed;
        public float colorWaveBSeed;
    }

    private void Start()
    {
        m_Lights = FindObjectsOfType<Light>();
        m_AnimInfo = new LightAnimInfo[m_Lights.Length];
        for (int i=0;i<m_Lights.Length;i++)
        {
            m_AnimInfo[i] = new LightAnimInfo();
            int random = Random.Range(0, 2) == 0 ? -1 : 1;
            m_AnimInfo[i].roundSpeed =  Random.Range(0.003f, 0.01f) * random;

            m_AnimInfo[i].yWaveHeight = Random.Range(1.5f, 5f);
            m_AnimInfo[i].yWaveSpeed= Random.Range(0.002f, 0.006f);
            m_AnimInfo[i].yWaveSeed = Random.Range(0, 3.14f);

            m_AnimInfo[i].rangeWaveSpeed = Random.Range(0.02f, 0.06f);
            m_AnimInfo[i].rangeWaveSeed = Random.Range(0, 3.14f);
            m_AnimInfo[i].rangeWaveAmplitude = Random.Range(0.0f, 2.5f);

            m_AnimInfo[i].colorWave = Random.Range(0, 5) < 2;
            m_AnimInfo[i].colorWaveSpeed = Random.Range(0.1f, 0.5f);
            m_AnimInfo[i].colorWaveRSeed = Random.Range(0, 3.14f);
            m_AnimInfo[i].colorWaveGSeed = Random.Range(0, 3.14f);
            m_AnimInfo[i].colorWaveBSeed = Random.Range(0, 3.14f);

        }
    }
    private void Update()
    {
        Vector3 camPos = Camera.main.transform.position;
        camPos.y += m_LightHeightAdd;
        for (int i=0;i< m_Lights.Length;i++)
        {
            Vector3 lightPos = RoundAnim(ref camPos, m_Lights[i].transform.position, ref m_AnimInfo[i]);
            float yWave = Wave(m_AnimInfo[i].yWaveSpeed, m_AnimInfo[i].yWaveSeed, m_AnimInfo[i].yWaveHeight);
            float rangeWave = Wave(m_AnimInfo[i].yWaveSpeed, m_AnimInfo[i].yWaveSeed, m_AnimInfo[i].yWaveHeight);
            rangeWave = 0;
            if (m_AnimInfo[i].colorWave)
            {
                float rWave = Wave(m_AnimInfo[i].colorWaveSpeed, m_AnimInfo[i].colorWaveRSeed, 1);
                float gWave = Wave(m_AnimInfo[i].colorWaveSpeed, m_AnimInfo[i].colorWaveGSeed, 1);
                float bWave = Wave(m_AnimInfo[i].colorWaveSpeed, m_AnimInfo[i].colorWaveBSeed, 1);
                Color color = m_Lights[i].color;
                color.r += rWave;
                color.g += gWave;
                color.b += bWave;
            }

            lightPos.y += yWave;
            m_Lights[i].transform.position = lightPos;
            m_Lights[i].range += rangeWave;
        }
    }

    Vector3 RoundAnim(ref Vector3 camPos, Vector3 curPos, ref LightAnimInfo info)
    {
        Vector2 cam2CurXZ = (curPos - camPos).Vt2XZ();
        float cos = Mathf.Cos(info.roundSpeed * Mathf.Rad2Deg * Time.deltaTime);
        float sin = Mathf.Sin(info.roundSpeed * Mathf.Rad2Deg * Time.deltaTime);
        Vector2 rotatePos = Vector2.zero;
        rotatePos.x = cos * cam2CurXZ.x - sin * cam2CurXZ.y;
        rotatePos.y = sin * cam2CurXZ.x + cos * cam2CurXZ.y;
        return camPos + new Vector3(rotatePos.x, 0, rotatePos.y);
    }
    float Wave(float speed, float seed, float amplitude)
    {
        return Mathf.Cos(speed * Mathf.Rad2Deg * Time.deltaTime + seed) * amplitude;
    }
}
