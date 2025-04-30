using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScanEffect_Scene : MonoBehaviour
{

    private void Start()
    {
        Camera.main.depthTextureMode = DepthTextureMode.DepthNormals;
    }
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.E))
        {
            ScanEffectM.Ins.Scan();
        }
    }
}
