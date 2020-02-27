using UnityEngine.Rendering;
using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class IBLFilterGGX
    {
        RenderTexture m_GgxIblSampleData;
        int           m_GgxIblMaxSampleCount          = TextureCache.isMobileBuildTarget ? 34 : 89;   // Width
        const int     k_GgxIblMipCountMinusOne        = 6;    // Height (UNITY_SPECCUBE_LOD_STEPS)

        ComputeShader m_ComputeGgxIblSampleDataCS;
        int           m_ComputeGgxIblSampleDataKernel = -1;

        ComputeShader m_BuildProbabilityTablesCS;
        int           m_ConditionalDensitiesKernel    = -1;
        int           m_MarginalRowDensitiesKernel    = -1;

        Material      m_GgxConvolveMaterial; // Convolves a cubemap with GGX

        Matrix4x4[]   m_faceWorldToViewMatrixMatrices     = new Matrix4x4[6];


        RenderPipelineResources m_RenderPipelineResources;
        BufferPyramidProcessor m_BufferPyramidProcessor;
        List<RenderTexture> m_PlanarColorMips = new List<RenderTexture>();

        public IBLFilterGGX(RenderPipelineResources renderPipelineResources, BufferPyramidProcessor processor)
        {
            m_RenderPipelineResources = renderPipelineResources;
            m_BufferPyramidProcessor = processor;
        }

        public bool IsInitialized()
        {
            return m_GgxIblSampleData != null;
        }

        public void Initialize(CommandBuffer cmd)
        {
            if (!m_ComputeGgxIblSampleDataCS)
            {
                m_ComputeGgxIblSampleDataCS     = m_RenderPipelineResources.computeGgxIblSampleData;
                m_ComputeGgxIblSampleDataKernel = m_ComputeGgxIblSampleDataCS.FindKernel("ComputeGgxIblSampleData");
            }

            if (!m_BuildProbabilityTablesCS)
            {
                m_BuildProbabilityTablesCS   = m_RenderPipelineResources.buildProbabilityTables;
                m_ConditionalDensitiesKernel = m_BuildProbabilityTablesCS.FindKernel("ComputeConditionalDensities");
                m_MarginalRowDensitiesKernel = m_BuildProbabilityTablesCS.FindKernel("ComputeMarginalRowDensities");
            }

            if (!m_GgxConvolveMaterial)
            {
                m_GgxConvolveMaterial = CoreUtils.CreateEngineMaterial(m_RenderPipelineResources.GGXConvolve);
            }

            if (!m_GgxIblSampleData)
            {
                m_GgxIblSampleData = new RenderTexture(m_GgxIblMaxSampleCount, k_GgxIblMipCountMinusOne, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                m_GgxIblSampleData.useMipMap = false;
                m_GgxIblSampleData.autoGenerateMips = false;
                m_GgxIblSampleData.enableRandomWrite = true;
                m_GgxIblSampleData.filterMode = FilterMode.Point;
                m_GgxIblSampleData.name = CoreUtils.GetRenderTargetAutoName(m_GgxIblMaxSampleCount, k_GgxIblMipCountMinusOne, 1, RenderTextureFormat.ARGBHalf, "GGXIblSampleData");
                m_GgxIblSampleData.hideFlags = HideFlags.HideAndDontSave;
                m_GgxIblSampleData.Create();

                m_ComputeGgxIblSampleDataCS.SetTexture(m_ComputeGgxIblSampleDataKernel, "output", m_GgxIblSampleData);

                using (new ProfilingSample(cmd, "Compute GGX IBL Sample Data"))
                {
                    cmd.DispatchCompute(m_ComputeGgxIblSampleDataCS, m_ComputeGgxIblSampleDataKernel, 1, 1, 1);
                }
            }

            for (int i = 0; i < 6; ++i)
            {
                var lookAt = Matrix4x4.LookAt(Vector3.zero, CoreUtils.lookAtList[i], CoreUtils.upVectorList[i]);
                m_faceWorldToViewMatrixMatrices[i] = lookAt * Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f)); // Need to scale -1.0 on Z to match what is being done in the camera.wolrdToCameraMatrix API. ...
            }
        }

        public void Cleanup()
        {
            CoreUtils.Destroy(m_GgxConvolveMaterial);
            CoreUtils.Destroy(m_GgxIblSampleData);
            for (var i = 0; i < m_PlanarColorMips.Count; ++i)
                m_PlanarColorMips[i].Release();
            m_PlanarColorMips.Clear();
        }

        void FilterCubemapCommon(CommandBuffer cmd,
            Texture source, RenderTexture target,
            Matrix4x4[] worldToViewMatrices)
        {
            int mipCount = 1 + (int)Mathf.Log(source.width, 2.0f);
            if (mipCount < ((int)EnvConstants.SpecCubeLodStep + 1))
            {
                Debug.LogWarning("RenderCubemapGGXConvolution: Cubemap size is too small for GGX convolution, needs at least " + ((int)EnvConstants.SpecCubeLodStep + 1) + " mip levels");
                return;
            }

            // Copy the first mip
            using (new ProfilingSample(cmd, "Copy Original Mip"))
            {
                for (int f = 0; f < 6; f++)
                {
                    cmd.CopyTexture(source, f, 0, target, f, 0);
                }
            }

            // Solid angle associated with a texel of the cubemap.
            float invOmegaP = (6.0f * source.width * source.width) / (4.0f * Mathf.PI);

            m_GgxConvolveMaterial.SetTexture("_GgxIblSamples", m_GgxIblSampleData);

            var props = new MaterialPropertyBlock();
            props.SetTexture("_MainTex", source);
            props.SetFloat("_InvOmegaP", invOmegaP);

            for (int mip = 1; mip < ((int)EnvConstants.SpecCubeLodStep + 1); ++mip)
            {
                props.SetFloat("_Level", mip);

                using (new ProfilingSample(cmd, "Filter Cubemap Mip {0}", mip))
                {
                    for (int face = 0; face < 6; ++face)
                    {
                        var faceSize = new Vector4(source.width >> mip, source.height >> mip, 1.0f / (source.width >> mip), 1.0f / (source.height >> mip));
                        var transform = HDUtils.ComputePixelCoordToWorldSpaceViewDirectionMatrix(0.5f * Mathf.PI, faceSize, worldToViewMatrices[face], true);

                        props.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, transform);

                        CoreUtils.SetRenderTarget(cmd, target, ClearFlag.None, mip, (CubemapFace)face);
                        CoreUtils.DrawFullScreen(cmd, m_GgxConvolveMaterial, props);
                    }
                }
            }
        }

        // Filters MIP map levels (other than 0) with GGX using BRDF importance sampling.
        public void FilterCubemap(CommandBuffer cmd, Texture source, RenderTexture target)
        {
            m_GgxConvolveMaterial.DisableKeyword("USE_MIS");

            FilterCubemapCommon(cmd, source, target, m_faceWorldToViewMatrixMatrices);
        }

        public void FilterPlanarTexture(CommandBuffer cmd, Texture source, RenderTexture target)
        {
            var lodCount = Mathf.Max(Mathf.FloorToInt(Mathf.Log(Mathf.Min(source.width, source.height), 2f)), 0);

            for (var i = 0; i < lodCount - 0; ++i)
            {
                var width = target.width >> (i + 1);
                var height = target.height >> (i + 1);
                var rtHash = HashRenderTextureProperties(
                        width,
                        height,
                        target.depth,
                        target.format,
                        target.sRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear
                        );

                var lodIsMissing = i >= m_PlanarColorMips.Count;
                RenderTexture rt = null;
                var createRT = lodIsMissing
                    || (rt = m_PlanarColorMips[i]) == null
                    || rtHash != HashRenderTextureProperties(
                        rt.width, rt.height, rt.depth, rt.format, rt.sRGB
                        ? RenderTextureReadWrite.sRGB
                        : RenderTextureReadWrite.Linear
                        );

                if (createRT && rt)
                    rt.Release();
                if (createRT)
                {
                    rt = new RenderTexture(
                            width,
                            height,
                            target.depth,
                            target.format,
                            target.sRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear
                            );
                    rt.enableRandomWrite = true;
                    rt.name = "Planar Convolution Tmp RT";
                    rt.hideFlags = HideFlags.HideAndDontSave;
                    rt.Create();
                }
                if (lodIsMissing)
                    m_PlanarColorMips.Add(rt);
                else if (createRT)
                    m_PlanarColorMips[i] = rt;
            }

            m_BufferPyramidProcessor.RenderColorPyramid(
                new RectInt(0, 0, source.width, source.height),
                cmd,
                source,
                target,
                m_PlanarColorMips,
                lodCount
                );
        }

        // Filters MIP map levels (other than 0) with GGX using multiple importance sampling.
        public void FilterCubemapMIS(CommandBuffer cmd,
            Texture source, RenderTexture target,
            RenderTexture conditionalCdf, RenderTexture marginalRowCdf)
        {
            // Bind the input cubemap.
            m_BuildProbabilityTablesCS.SetTexture(m_ConditionalDensitiesKernel, "envMap", source);

            // Bind the outputs.
            m_BuildProbabilityTablesCS.SetTexture(m_ConditionalDensitiesKernel, "conditionalDensities", conditionalCdf);
            m_BuildProbabilityTablesCS.SetTexture(m_ConditionalDensitiesKernel, "marginalRowDensities", marginalRowCdf);
            m_BuildProbabilityTablesCS.SetTexture(m_MarginalRowDensitiesKernel, "marginalRowDensities", marginalRowCdf);

            int numRows = conditionalCdf.height;

            using (new ProfilingSample(cmd, "Build Probability Tables"))
            {
                cmd.DispatchCompute(m_BuildProbabilityTablesCS, m_ConditionalDensitiesKernel, numRows, 1, 1);
                cmd.DispatchCompute(m_BuildProbabilityTablesCS, m_MarginalRowDensitiesKernel, 1, 1, 1);
            }

            m_GgxConvolveMaterial.EnableKeyword("USE_MIS");
            m_GgxConvolveMaterial.SetTexture("_ConditionalDensities", conditionalCdf);
            m_GgxConvolveMaterial.SetTexture("_MarginalRowDensities", marginalRowCdf);

            FilterCubemapCommon(cmd, source, target, m_faceWorldToViewMatrixMatrices);
        }

        int HashRenderTextureProperties(
            int width,
            int height,
            int depth,
            RenderTextureFormat format,
            RenderTextureReadWrite sRGB)
        {
            return width.GetHashCode()
                ^ height.GetHashCode()
                ^ depth.GetHashCode()
                ^ format.GetHashCode()
                ^ sRGB.GetHashCode();
        }
    }
}
