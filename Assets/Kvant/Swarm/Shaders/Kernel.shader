//
// GPGPU kernels for Swarm
//
// Position buffer format:
// .xyz = particle position
// .w   = random number
//
// Velocity buffer format:
// .xyz = particle velocity
// .w   = 0
//
Shader "Hidden/Kvant/Swarm/Kernel"
{
    Properties
    {
        _PositionTex ("-", 2D) = ""{}
        _VelocityTex ("-", 2D) = ""{}
    }

    CGINCLUDE

    #pragma multi_compile _ ENABLE_SWIRL

    #include "UnityCG.cginc"
    #include "SimplexNoiseGrad3D.cginc"

    sampler2D _PositionTex;
    sampler2D _VelocityTex;
    float4 _PositionTex_TexelSize;
    float4 _VelocityTex_TexelSize;

    float3 _Acceleration;   // min, max, drag
    float4 _Attractor;      // x, y, z, spread
    float3 _Flow;
    float4 _NoiseParams; // (frequency, amplitude, motion, variance)
    float3 _NoiseOffset;
    float2 _SwirlParams; // (strength, density)
    float _RandomSeed;
    float2 _TimeParams; // (current, delta)

    // Pseudo random number generator
    float nrand(float2 uv, float salt)
    {
        uv += float2(salt, _RandomSeed);
        return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
    }

    // Position dependant force field
    float3 position_force(float3 p, float2 uv)
    {
        p = (p + _NoiseOffset) * _NoiseParams.x + _TimeParams.x * _NoiseParams.z;
        float3 uvc = float3(uv, 7.919) * _NoiseParams.w;
        float3 n1 = snoise_grad(p + uvc.xyz);
        float3 n2 = snoise_grad(p + uvc.yzx);
        return cross(n1, n2) * _NoiseParams.y * 0.1;
        // FIXME: remove 0.1, that's very magic#
    }

    // Attractor position
    float3 attract_point(float2 uv)
    {
        float3 r = float3(nrand(uv, 0), nrand(uv, 1), nrand(uv, 2));
        return _Attractor.xyz + (r - (float3)0.5) * _Attractor.w;
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

        // Use the flow vector or add swirl vector.
        float3 flow = _Flow;
#if ENABLE_SWIRL
        flow += position_force(p.xyz * _SwirlParams.y, i.uv) * _SwirlParams.x;
#endif
        // Add the velocity (u=0) or the flow vector (u>0).
        float u_0 = i.uv.x < _PositionTex_TexelSize.x;
        p.xyz += lerp(flow, v, u_0) * _TimeParams.y;

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

        // Drag
        v *= _Acceleration.z;

        // Acceleration
        v += acs * acf * _TimeParams.y;

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
