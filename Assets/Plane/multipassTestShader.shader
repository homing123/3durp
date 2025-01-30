Shader "My/multipassTestShader"
{
    Properties
    {
    }

    SubShader
    {
        Tags
        {
            // SRP introduced a new "RenderPipeline" tag in Subshader. This allows you to create shaders
            // that can match multiple render pipelines. If a RenderPipeline tag is not set it will match
            // any render pipeline. In case you want your SubShader to only run in URP, set the tag to
            // "UniversalPipeline"

            // here "UniversalPipeline" tag is required, because we only want this shader to run in URP.
            // If Universal render pipeline is not set in the graphics settings, this SubShader will fail.

            // One can add a SubShader below or fallback to Standard built-in to make this
            // material works with both Universal Render Pipeline and Builtin-RP

            // the tag value is "UniversalPipeline", not "UniversalRenderPipeline", be careful!
            "RenderPipeline" = "UniversalPipeline"

            // explicit SubShader tag to avoid confusion
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }
        Pass
        {   
            Name "Red"
            Tags{"LightMode" = "UniversalForwardOnly"}

            ZTest Always
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            struct vertexInput
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct vertexOutput
            {
                float4 pos : POSITION;
                float4 color : COLOR;
            };

            vertexOutput vert(vertexInput v)
            {
                vertexOutput o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(vertexOutput i) : SV_Target
            {
                return fixed4(1, 0, 0, 1);
            }
            ENDCG
        }

        Pass
        {            
            Name "Green"
               Tags{"LightMode" = "SRPDefaultUnlit"}
            ZTest Always
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            struct vertexInput
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct vertexOutput
            {
                float4 pos : POSITION;
                float4 color : COLOR;
            };

            vertexOutput vert(vertexInput v)
            {
                vertexOutput o;

                if (v.vertex.x > 0.1)
                {
                    o.color = float4(0, 1, 0, 1);
                }
                else
                {
                    o.color = float4(0, 0, 0, 0);
                }

                o.pos = UnityObjectToClipPos(v.vertex);

                return o;
            }

            fixed4 frag(vertexOutput i) : SV_Target
            {
                clip(i.color.g - 0.5);
                return i.color;
            }

            ENDCG
        }
    }
}