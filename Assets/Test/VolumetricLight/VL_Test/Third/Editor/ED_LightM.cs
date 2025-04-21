using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LightM))]
public class ED_LightM : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        LightM lightm = (LightM)target;
        if (GUILayout.Button("GenerateLight"))
        {
            lightm.AddLight();
        }
    }
}
