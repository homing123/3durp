using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthofFieldTestScene : MonoBehaviour
{
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
