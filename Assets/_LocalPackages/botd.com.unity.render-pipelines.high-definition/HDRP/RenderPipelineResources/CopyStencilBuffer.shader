Shader "Hidden/HDRenderPipeline/CopyStencilBuffer"
{
    Properties
    {
        [HideInInspector] _StencilRef("_StencilRef", Int) = 1
        [HideInInspector] _StencilMask("_StencilMask", Int) = 7
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
    // #pragma enable_d3d11_debug_symbols

    #include "CoreRP/ShaderLibrary/Common.hlsl"
    #include "CoreRP/ShaderLibrary/Packing.hlsl"
    #include "../ShaderVariables.hlsl"

    int _StencilRef;
    RW_TEXTURE2D(float, _HTile); // DXGI_FORMAT_R8_UINT is not supported by Unity

    struct Attributes
    {
        uint vertexID : SV_VertexID;
    };

    struct Varyings
    {
        float4 positionCS : SV_Position;
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        return output;
    }

    #pragma vertex Vert

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            Name "Pass 0 - Copy stencilRef to output"

            Stencil
            {
                ReadMask [_StencilMask]
                Ref  [_StencilRef]
                Comp Equal
                Pass Keep
            }

            Cull   Off
            ZTest  Always
            ZWrite Off
            Blend  Off

            HLSLPROGRAM
            #pragma fragment Frag

            // Force the stencil test before the UAV write.
            [earlydepthstencil]
            float4 Frag(Varyings input) : SV_Target // use SV_StencilRef in D3D 11.3+
            {
                return PackByte(_StencilRef);
            }

            ENDHLSL
        }

        Pass
        {
            Name "Pass 1 - Write 1 if value different from stencilRef to output"

            Stencil
            {
                ReadMask [_StencilMask]
                Ref  [_StencilRef]
                Comp NotEqual
                Pass Keep
            }

            Cull   Off
            ZTest  Always
            ZWrite Off
            Blend  Off

            HLSLPROGRAM
            #pragma fragment Frag

            // Force the stencil test before the UAV write.
            [earlydepthstencil]
            float4 Frag(Varyings input) : SV_Target // use SV_StencilRef in D3D 11.3+
            {
                return float4(1.0, 0.0, 0.0, 0.0); // 1.0 for true as it passes the condition
            }

            ENDHLSL
        }

        Pass
        {
            Name "Pass 2 - Export HTILE for stencilRef to output"

            Stencil
            {
                ReadMask [_StencilMask]
                Ref  [_StencilRef]
                Comp Equal
                Pass Keep
            }

            Cull   Off
            ZTest  Always
            ZWrite Off
            Blend  Off
            ColorMask 0

            HLSLPROGRAM
            #pragma fragment Frag

            // Force the stencil test before the UAV write.
            [earlydepthstencil]
            float4 Frag(Varyings input) : SV_Target // use SV_StencilRef in D3D 11.3+
            {
                uint2 positionNDC = (uint2)input.positionCS.xy;
                // There's no need for atomics as we are always writing the same value.
                // Note: the GCN tile size is 8x8 pixels.
                _HTile[positionNDC / 8] = _StencilRef;

                return float4(0.0, 0.0, 0.0, 0.0);
            }

            ENDHLSL
        }
    }
    Fallback Off
}
