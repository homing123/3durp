using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightM : MonoBehaviour
{
    public void AddLight()
    {
        GameObject obj = new GameObject();
        obj.name = "Light";
        Light light = obj.AddComponent<Light>();
        VoxelLight.Ins.AddLight(light);
    }

}
