using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class Decal
    {
        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, true, 200)]
        public struct DecalSurfaceData
        {
            [SurfaceDataAttributes("Base Color", false, true)]
            public Vector4 baseColor;
            [SurfaceDataAttributes("Normal", true)]
            public Vector4 normalWS;
            [SurfaceDataAttributes("Mask", true)]
            public Vector4 mask;
            [SurfaceDataAttributes("HTileMask")]
            public uint HTileMask;
        };

        [GenerateHLSL(PackingRules.Exact)]
        public enum DBufferMaterial
        {
            // Note: This count doesn't include the velocity buffer. On shader and csharp side the velocity buffer will be added by the framework
            Count = 3
        };

        [GenerateHLSL(PackingRules.Exact)]
        public enum DBufferHTileBit
        {
            Diffuse = 1,
            Normal = 2,
            Mask = 4
        };

        //-----------------------------------------------------------------------------
        // DBuffer management
        //-----------------------------------------------------------------------------

        // should this be combined into common class shared with Lit.cs???
        static public int GetMaterialDBufferCount() { return (int)DBufferMaterial.Count; }

        static RenderTextureFormat[] m_RTFormat = { RenderTextureFormat.ARGB32, RenderTextureFormat.ARGB32, RenderTextureFormat.ARGB32 };
        static bool[] m_sRGBFlags = { true, false, false };

        static public void GetMaterialDBufferDescription(out RenderTextureFormat[] RTFormat, out bool[] sRGBFlags)
        {
            RTFormat = m_RTFormat;
            sRGBFlags = m_sRGBFlags;
        }
    }

    // normal to world only uses 3x3 for actual matrix so some data is packed in the unused space
    // blend:
    // float decalBlend = decalData.normalToWorld[0][3];
    // albedo contribution on/off:
    // float albedoContribution = decalData.normalToWorld[1][3];
    // tiling:
    // float2 uvScale = float2(decalData.normalToWorld[3][0], decalData.normalToWorld[3][1]);
    // float2 uvBias = float2(decalData.normalToWorld[3][2], decalData.normalToWorld[3][3]);
    [GenerateHLSL(PackingRules.Exact, false)]
    public struct DecalData
    {
        public Matrix4x4 worldToDecal;
        public Matrix4x4 normalToWorld;
        public Vector4 diffuseScaleBias;
        public Vector4 normalScaleBias;
        public Vector4 maskScaleBias;
        public Vector4 baseColor;
    };
}
