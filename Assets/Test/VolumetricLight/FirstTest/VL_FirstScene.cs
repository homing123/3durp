using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VL_FirstScene : MonoBehaviour
{
    public static VL_FirstScene Ins;
    [SerializeField] public Light m_Light;
    private void Awake()
    {
        Ins = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        Camera.main.depthTextureMode = DepthTextureMode.DepthNormals;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
