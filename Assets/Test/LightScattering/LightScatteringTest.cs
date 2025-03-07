
using UnityEngine;
public class LightScatteringTest : MonoBehaviour
{
    private void Awake()
    {
        Camera.main.depthTextureMode = DepthTextureMode.DepthNormals;
    }
}