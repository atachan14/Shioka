Shader "Shioka/TrainingDefenseFill"
{
 Properties { _Mid("Mid",Color)=(1,1,1,.2) _High("High",Color)=(1,1,1,.2) _Low("Low",Color)=(1,1,1,.2) }
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-1" "RenderType"="Transparent" }
 Pass {
 ZWrite Off ZTest Always Cull Off Blend SrcAlpha OneMinusSrcAlpha
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
 struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
 CBUFFER_START(UnityPerMaterial)
 half4 _Mid; half4 _High; half4 _Low;
 CBUFFER_END
 Varyings Vert(Attributes v){Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=v.uv;return o;}
 half4 Frag(Varyings v):SV_Target {
 float y=saturate(v.uv.y);
 if(y>=2.0/3.0)return lerp(_High,_Mid,smoothstep(0,1,(y-2.0/3.0)*3));
 if(y>=1.0/3.0)return _High;
 return lerp(_Low,_High,smoothstep(0,1,y*3));
 }
 ENDHLSL
 }
 }
}
