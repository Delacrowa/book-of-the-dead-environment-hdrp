using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class ProceduralSkyRenderer : SkyRenderer
    {
        Material m_ProceduralSkyMaterial;
        MaterialPropertyBlock m_PropertyBlock;
        ProceduralSky m_ProceduralSkyParams;

        readonly int _SunSizeParam = Shader.PropertyToID("_SunSize");
        readonly int _SunSizeConvergenceParam = Shader.PropertyToID("_SunSizeConvergence");
        readonly int _AtmoshpereThicknessParam = Shader.PropertyToID("_AtmosphereThickness");
        readonly int _SkyTintParam = Shader.PropertyToID("_SkyTint");
        readonly int _GroundColorParam = Shader.PropertyToID("_GroundColor");
        readonly int _SunColorParam = Shader.PropertyToID("_SunColor");
        readonly int _SunDirectionParam = Shader.PropertyToID("_SunDirection");

        public ProceduralSkyRenderer(ProceduralSky proceduralSkyParams)
        {
            m_ProceduralSkyParams = proceduralSkyParams;
            m_PropertyBlock = new MaterialPropertyBlock();
        }

        public override void Build()
        {
            var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            m_ProceduralSkyMaterial = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.proceduralSky);
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_ProceduralSkyMaterial);
        }

        public override void SetRenderTargets(BuiltinSkyParameters builtinParams)
        {
            if (builtinParams.depthBuffer == BuiltinSkyParameters.nullRT)
            {
                HDUtils.SetRenderTarget(builtinParams.commandBuffer, builtinParams.hdCamera, builtinParams.colorBuffer);
            }
            else
            {
                HDUtils.SetRenderTarget(builtinParams.commandBuffer, builtinParams.hdCamera, builtinParams.colorBuffer, builtinParams.depthBuffer);
            }
        }

        public override void RenderSky(BuiltinSkyParameters builtinParams, bool renderForCubemap)
        {
            CoreUtils.SetKeyword(m_ProceduralSkyMaterial, "_ENABLE_SUN_DISK", m_ProceduralSkyParams.enableSunDisk);

            Color sunColor = Color.white;
            Vector3 sunDirection = Vector3.zero;
            if (builtinParams.sunLight != null)
            {
                sunColor = builtinParams.sunLight.color * builtinParams.sunLight.intensity;
                sunDirection = -builtinParams.sunLight.transform.forward;
            }

            m_ProceduralSkyMaterial.SetVector(HDShaderIDs._SkyParam, new Vector4(GetExposure(m_ProceduralSkyParams, builtinParams.debugSettings), m_ProceduralSkyParams.multiplier, 0.0f, 0.0f));
            m_ProceduralSkyMaterial.SetFloat(_SunSizeParam, m_ProceduralSkyParams.sunSize);
            m_ProceduralSkyMaterial.SetFloat(_SunSizeConvergenceParam, m_ProceduralSkyParams.sunSizeConvergence);
            m_ProceduralSkyMaterial.SetFloat(_AtmoshpereThicknessParam, m_ProceduralSkyParams.atmosphereThickness);
            m_ProceduralSkyMaterial.SetColor(_SkyTintParam, m_ProceduralSkyParams.skyTint);
            m_ProceduralSkyMaterial.SetColor(_GroundColorParam, m_ProceduralSkyParams.groundColor);
            m_ProceduralSkyMaterial.SetColor(_SunColorParam, sunColor);
            m_ProceduralSkyMaterial.SetVector(_SunDirectionParam, sunDirection);

            // This matrix needs to be updated at the draw call frequency.
            m_PropertyBlock.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, builtinParams.pixelCoordToViewDirMatrix);

            CoreUtils.DrawFullScreen(builtinParams.commandBuffer, m_ProceduralSkyMaterial, m_PropertyBlock, renderForCubemap ? 0 : 1);
        }

        public override bool IsValid()
        {
            return true;
        }
    }
}
