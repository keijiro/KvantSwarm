//
// GPGPU kernels for Swarm
//
Shader "Hidden/Kvant/Swarm/Kernel"
{
    Properties
    {
        _PositionTex ("-", 2D) = ""{}
        _VelocityTex ("-", 2D) = ""{}
    }

    CGINCLUDE

    #include "UnityCG.cginc"
    #include "ClassicNoise3D.cginc"

    sampler2D _PositionTex;
    sampler2D _VelocityTex;
    float4 _PositionTex_TexelSize;
    float4 _VelocityTex_TexelSize;

    float3 _AttractPos;
    float3 _Acceleration;
    float3 _NoiseParams; // (frequency, amplitude, animation)
    float _RandomSeed;

    // Pseudo random number generator
    float nrand(float2 uv, float salt)
    {
        uv += float2(salt, _RandomSeed);
        return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
    }

    // Position dependant velocity field
    float3 position_velocity(float3 p, float2 uv)
    {
        p = (p + _Time.y * _NoiseParams.z) * _NoiseParams.x;
        float nx = cnoise(p + float3(138.2, uv.x, uv.y));
        float ny = cnoise(p + float3(uv.y, 138.2, uv.x));
        float nz = cnoise(p + float3(uv.x, uv.y, 138.2));
        return float3(nx, ny, nz) * _NoiseParams.y;
    }

    // Pass 0: position initialization
    float4 frag_init_position(v2f_img i) : SV_Target 
    {
        return (float4)0;
    }

    // Pass 1: velocity initialization
    float4 frag_init_velocity(v2f_img i) : SV_Target 
    {
        return (float4)0;
    }

    // Pass 2: position update
    float4 frag_update_position(v2f_img i) : SV_Target 
    {
        if (i.uv.x < _PositionTex_TexelSize.x)
        {
            float dt = unity_DeltaTime.x;

            float3 p = tex2D(_PositionTex, i.uv).xyz;
            float3 v = tex2D(_VelocityTex, i.uv).xyz;

            //p.xyz += (v + position_velocity(p.xyz, i.uv)) * dt;
            p.xyz += v * dt;

            return float4(p, 1);
        }
        else
        {
            float2 duv = float2(_PositionTex_TexelSize.x, 0);
            return tex2D(_PositionTex, i.uv - duv) + float4(-2.0/60, 0, 0, 0);
        }
    }

    float3 attract_point(float2 uv)
    {
        return _AttractPos + float3(nrand(uv, 3), nrand(uv, 4), nrand(uv, 5)) * 0.1;
    }

    // Pass 3: velocity update
    float4 frag_update_velocity(v2f_img i) : SV_Target 
    {
        if (i.uv.x < _VelocityTex_TexelSize.x)
        {
            float dt = unity_DeltaTime.x;

            float3 p = tex2D(_PositionTex, i.uv).xyz;
            float3 v = tex2D(_VelocityTex, i.uv).xyz;

            float a = lerp(_Acceleration.x, _Acceleration.y, nrand(i.uv, 9));
            float3 ac = (attract_point(i.uv) - p + position_velocity(p, i.uv)) * a;
            v = v * (1.0 - _Acceleration.z) + ac * dt;

            return float4(v, 0);
        }
        else
        {
            float2 duv = float2(_VelocityTex_TexelSize.x, 0);
            return tex2D(_VelocityTex, i.uv - duv);
        }
    }

    ENDCG

    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment frag_init_position
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment frag_init_velocity
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment frag_update_position
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment frag_update_velocity
            ENDCG
        }
    }
}
