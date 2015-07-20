//
// Line shader for Swarm
//
Shader "Hidden/Kvant/Swarm/Line"
{
    Properties
    {
        _PositionTex ("-", 2D) = ""{}
        [HDR] _Color1 ("-", Color) = (1, 1, 1, 1)
        [HDR] _Color2 ("-", Color) = (1, 1, 1, 1)
    }

    CGINCLUDE

    #pragma multi_compile_fog

    #include "UnityCG.cginc"

    struct appdata
    {
        float4 position : POSITION;
        float2 texcoord : TEXCOORD0;
    };

    struct v2f
    {
        float4 position : SV_POSITION;
        half4 color : COLOR;
        UNITY_FOG_COORDS(0)
    };

    sampler2D _PositionTex;
    float4 _PositionTex_TexelSize;

    half4 _Color1;
    half4 _Color2;
    half _GradExp;

    // Pseudo random number generator
    float nrand(float2 uv, float salt)
    {
        uv += float2(salt, 0);
        return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
    }

    v2f vert(appdata v)
    {
        v2f o;

        float2 uv = v.texcoord.xy + _PositionTex_TexelSize.xy / 2;

        float4 p = v.position + tex2Dlod(_PositionTex, float4(uv, 0, 0));
        o.position = mul(UNITY_MATRIX_MVP, p);

        float4 c = lerp(_Color1, _Color2, nrand(uv.yy, 10));
        c.a *= pow(1.0 - uv.x, _GradExp);
        o.color = c;

        UNITY_TRANSFER_FOG(o, o.position);
        return o;
    }

    half4 frag(v2f i) : SV_Target
    {
        fixed4 c = i.color;
        UNITY_APPLY_FOG(i.fogCoord, c);
        return c;
    }

    ENDCG

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    } 
}
