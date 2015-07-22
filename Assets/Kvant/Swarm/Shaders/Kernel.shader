//
// GPGPU kernels for Swarm
//
// Texture format:
//
// _PositionTex.xyz = position
// _PositionTex.w   = random number
//
// _VelocityTex.xyz = velocity vector
// _VelocityTex.w   = 0
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
    float2 _Acceleration; // (min, max)
    float _Spread;
    float _Damp;
    float4 _NoiseParams; // (frequency, amplitude, animation, variance)
    float _RandomSeed;
    float3 _Flow;
    float _DeltaTime;

    // Pseudo random number generator
    float nrand(float2 uv, float salt)
    {
        uv += float2(salt, _RandomSeed);
        return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
    }

    // Position dependant force field
    float3 position_force(float3 p, float2 uv)
    {
        p = p * _NoiseParams.x + _Time.y * _NoiseParams.z + _RandomSeed;
        float3 uvc = float3(uv, 7.919) * _NoiseParams.w;
        float nx = cnoise(p + uvc.xyz);
        float ny = cnoise(p + uvc.yzx);
        float nz = cnoise(p + uvc.zxy);
        return float3(nx, ny, nz) * _NoiseParams.y;
    }

    // Attractor position
    float3 attract_point(float2 uv)
    {
        float3 r = float3(nrand(uv, 0), nrand(uv, 1), nrand(uv, 2));
        return _AttractPos + (r - (float3)0.5) * _Spread;
    }

    // Pass 0: position initialization
    float4 frag_init_position(v2f_img i) : SV_Target 
    {
        return float4(0, 0, 0, nrand(i.uv.yy, 3));
    }

    // Pass 1: velocity initialization
    float4 frag_init_velocity(v2f_img i) : SV_Target 
    {
        return (float4)0;
    }

    // Pass 2: position update
    float4 frag_update_position(v2f_img i) : SV_Target 
    {
        // Fetch the current position (u=0) or the previous position (u>0).
        float2 uv_prev = float2(_PositionTex_TexelSize.x, 0);
        float4 p = tex2D(_PositionTex, i.uv - uv_prev);

        // Fetch the velocity vector.
        float3 v = tex2D(_VelocityTex, i.uv).xyz;

        // Add the velocity (u=0) or the flow vector (u>0).
        float u_0 = i.uv.x < _PositionTex_TexelSize.x;
        p.xyz += lerp(_Flow, v, u_0) * _DeltaTime;

        return p;
    }

    // Pass 3: velocity update
    float4 frag_update_velocity(v2f_img i) : SV_Target 
    {
        // Only needs the leftmost pixel.
        float2 uv = i.uv * float2(0, 1);

        // Fetch the current position/velocity.
        float3 p = tex2D(_PositionTex, uv).xyz;
        float3 v = tex2D(_VelocityTex, uv).xyz;

        // Acceleration scale factor
        float acs = lerp(_Acceleration.x, _Acceleration.y, nrand(uv, 4));

        // Acceleration force
        float3 acf = attract_point(i.uv) - p + position_force(p, uv);

        // Damping
        v *= (1.0 - _Damp * _DeltaTime);

        // Acceleration
        v += acs * acf * _DeltaTime;

        return float4(v, 0);
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
