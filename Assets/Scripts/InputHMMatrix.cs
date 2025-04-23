using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class InputHMMatrix : MonoBehaviour
{

    float4x4 GetProjectionMat10(float near, float far, float rcp_tan, float aspect) //view.z 안뒤집어도 됨
    {
        float FminusN_RCP = 1 / (far - near);
        return new float4x4(1 * rcp_tan / aspect, 0, 0, 0,
            0, rcp_tan, 0, 0,
            0, 0, -near * FminusN_RCP, near * far * FminusN_RCP,
            0, 0, 1, 0);
    }
    float4x4 GetInvProjectionMat10(float4x4 projmat)
    {
        //c#의 float4x4 는 column이므로 인덱싱주의
        float ARCP = 1 / projmat[0][0];
        float BRCP = 1 / projmat[1][1];
        float C = projmat[2][2];
        float DRCP = 1 / projmat[3][2];
        float ERCP = 1 / projmat[2][3];

        return new float4x4(ARCP, 0, 0, 0,
            0, BRCP, 0, 0,
            0, 0, 0, ERCP,
            0, 0, DRCP, -C * ERCP * DRCP);
    }

    private void Start()
    {
        Update();
    }
    private void Update()
    {
        float camNear = Camera.main.nearClipPlane;
        float camFar = Camera.main.farClipPlane;
        float fov = Camera.main.fieldOfView;
        float tan = Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f);
        float aspect = Screen.width / (float)Screen.height;

        float4x4 projMat = GetProjectionMat10(camNear, camFar, 1 / tan, aspect);
        float4x4 invPorjMat = GetInvProjectionMat10(projMat);

        Shader.SetGlobalFloat("_CamNear", camNear);
        Shader.SetGlobalFloat("_CamFar", camFar);
        Shader.SetGlobalMatrix("_ProjMat10", projMat);
        Shader.SetGlobalMatrix("_InvProjMat10", invPorjMat);
        //Debug.Log($"fov : {fov}, tan : {tan}, CamNear: {camNear}, CamFar : {camFar}, ProjMat : {projMat}, invProjMat : {invPorjMat}");
    }
}
