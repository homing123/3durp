using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VL_TestScene : MonoBehaviour
{
    [Serializable]
    public struct PosRot
    {
        public Vector3 pos;
        public Quaternion rot;
        public PosRot(Vector3 p, Quaternion r)
        {
            pos = p;
            rot = r;
        }
    }
    public enum E_VLKind
    {
        First,
        Second
    }

    [SerializeField] E_VLKind m_Kind;
    public static VL_TestScene Ins;
    [SerializeField] public Light m_Light;
    [SerializeField] List<PosRot> m_Preset;
    [SerializeField] int m_PresetIdx;

    public I_VLSetting GetVLSetting()
    {
        switch(m_Kind)
        {
            case E_VLKind.First:
                return transform.GetComponent<VL_First>();
            case E_VLKind.Second:
                return transform.GetComponent<VL_Second>();
        }
        return null;
    }
    private void Awake()
    {
        Ins = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        Camera.main.depthTextureMode = DepthTextureMode.DepthNormals;
    }
    [ContextMenu("������ ����")]
    public void PresetSave()
    {
        if(m_PresetIdx < 0)
        {
            return;
        }
        if(m_Preset.Count <= m_PresetIdx)
        {
            m_Preset.Add(new PosRot(Camera.main.transform.position, Camera.main.transform.rotation));
        }
        else
        {
            m_Preset[m_PresetIdx] = new PosRot(Camera.main.transform.position, Camera.main.transform.rotation);
        }
    }
    [ContextMenu("������ �ε�")]
    public void PresetLoad()
    {
        if (m_PresetIdx < 0 || m_Preset.Count <= m_PresetIdx)
        {
            return;
        }
        Camera.main.transform.position = m_Preset[m_PresetIdx].pos;
        Camera.main.transform.rotation = m_Preset[m_PresetIdx].rot;
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
