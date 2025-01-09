

struct DomainOut
{
    float3 posWorld : TEXCOORD1;
    float2 uv : TEXCOORD0;
    float4 posCS : SV_POSITION;

};

[domain("tri")]
DomainOut ds(PatchConstOutput patchConst, float3 uvw : SV_DomainLocation, const OutputPatch<HullOut, 3> patch)
{
    DomainOut o;
    o.posWorld = patch[0].posWorld * uvw.x + patch[1].posWorld * uvw.y + patch[2].posWorld * uvw.z;
    o.posCS = patch[0].posCS * uvw.x + patch[1].posCS * uvw.y + patch[2].posCS * uvw.z;
    o.uv = patch[0].uv * uvw.x + patch[1].uv * uvw.y + patch[2].uv * uvw.z;
    return o;
}


// 유니티에서 쿼드 테셀레이션은 지원하지 않는다고 하는거 같은데...
//[domain("quad")]
//DomainOut ds(PatchConstOutput patchConst, float2 uv : SV_DomainLocation, const OutputPatch<HullOut, 4> patch)
//{
//    DomainOut o;
//    float3 v1 = lerp(patch[0].posWorld, patch[1].posWorld, uv.x);
//    float3 v2 = lerp(patch[2].posWorld, patch[3].posWorld, uv.x);
//    float3 p = lerp(v1, v2, uv.y);
//
//    o.posWorld = p;
//
//    float4 cs1 = lerp(patch[0].posCS, patch[1].posCS, uv.x);
//    float4 cs2 = lerp(patch[2].posCS, patch[3].posCS, uv.x);
//    float4 posCS = lerp(cs1, cs2, uv.y);
//    o.posCS = posCS;
//
//    float2 uv1 = lerp(patch[0].uv, patch[1].uv, uv.x);
//    float2 uv2 = lerp(patch[2].uv, patch[3].uv, uv.x);
//    float2 texcoord = lerp(uv1, uv2, uv.y);
//    o.uv = texcoord;
//
//    return o;
//}