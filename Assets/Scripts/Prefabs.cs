using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Prefabs : MonoBehaviour
{
    public static Prefabs Ins;
    private void Awake()
    {
        Ins = this;
    }

    public GameObject G_Ground;
    public GameObject G_RenderTextureObject;

    public Material M_Grass;

}
