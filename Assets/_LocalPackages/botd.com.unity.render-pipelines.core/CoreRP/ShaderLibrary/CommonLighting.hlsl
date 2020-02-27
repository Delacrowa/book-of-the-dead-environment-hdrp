#ifndef UNITY_COMMON_LIGHTING_INCLUDED
#define UNITY_COMMON_LIGHTING_INCLUDED

// These clamping function to max of floating point 16 bit are use to prevent INF in code in case of extreme value
TEMPLATE_1_REAL(ClampToFloat16Max, value, return min(value, HALF_MAX))

// Ligthing convention
// Light direction is oriented backward (-Z). i.e in shader code, light direction is -lightData.forward

//-----------------------------------------------------------------------------
// Helper functions
//-----------------------------------------------------------------------------

// Performs the mapping of the vector 'v' centered within the axis-aligned cube
// of dimensions [-1, 1]^3 to a vector centered within the unit sphere.
// The function expects 'v' to be within the cube (possibly unexpected results otherwise).
// Ref: http://mathproofs.blogspot.com/2005/07/mapping-cube-to-sphere.html
real3 MapCubeToSphere(real3 v)
{
    real3 v2 = v * v;
    real2 vr3 = v2.xy * rcp(3.0);
    return v * sqrt((real3)1.0 - 0.5 * v2.yzx - 0.5 * v2.zxy + vr3.yxx * v2.zzy);
}

// Computes the squared magnitude of the vector computed by MapCubeToSphere().
real ComputeCubeToSphereMapSqMagnitude(real3 v)
{
    real3 v2 = v * v;
    // Note: dot(v, v) is often computed before this function is called,
    // so the compiler should optimize and use the precomputed result here.
    return dot(v, v) - v2.x * v2.y - v2.y * v2.z - v2.z * v2.x + v2.x * v2.y * v2.z;
}

// texelArea = 4.0 / (resolution * resolution).
// Ref: http://bpeers.com/blog/?itemid=1017
// This version is less accurate, but much faster than this one:
// http://www.rorydriscoll.com/2012/01/15/cubemap-texel-solid-angle/
real ComputeCubemapTexelSolidAngle(real3 L, real texelArea)
{
    // Stretch 'L' by (1/d) so that it points at a side of a [-1, 1]^2 cube.
    real d = Max3(abs(L.x), abs(L.y), abs(L.z));
    // Since 'L' is a unit vector, we can directly compute its
    // new (inverse) length without dividing 'L' by 'd' first.
    real invDist = d;

    // dw = dA * cosTheta / (dist * dist), cosTheta = 1.0 / dist,
    // where 'dA' is the area of the cube map texel.
    return texelArea * invDist * invDist * invDist;
}

// Only makes sense for Monte-Carlo integration.
// Normalize by dividing by the total weight (or the number of samples) in the end.
// Integrate[6*(u^2+v^2+1)^(-3/2), {u,-1,1},{v,-1,1}] = 4 * Pi
// Ref: "Stupid Spherical Harmonics Tricks", p. 9.
real ComputeCubemapTexelSolidAngle(real2 uv)
{
    float u = uv.x, v = uv.y;
    return pow(1 + u * u + v * v, -1.5);
}

//-----------------------------------------------------------------------------
// Attenuation functions
//-----------------------------------------------------------------------------

// Ref: Moving Frostbite to PBR.

// Non physically based hack to limit light influence to attenuationRadius.
// SmoothInfluenceAttenuation must be use, InfluenceAttenuation is just for optimization with SmoothQuadraticDistanceAttenuation
real InfluenceAttenuation(real distSquare, real rangeAttenuationScale, real rangeAttenuationBias)
{
    // Apply the saturate here as we can have negative number
    real factor = saturate(distSquare * rangeAttenuationScale + rangeAttenuationBias);
    return 1.0 - factor * factor;
}

real SmoothInfluenceAttenuation(real distSquare, real rangeAttenuationScale, real rangeAttenuationBias)
{
    real smoothFactor = InfluenceAttenuation(distSquare, rangeAttenuationScale, rangeAttenuationBias);
    return Sq(smoothFactor);
}

#define PUNCTUAL_LIGHT_THRESHOLD 0.01 // 1cm (in Unity 1 is 1m)

// Return physically based quadratic attenuation + influence limit to reach 0 at attenuationRadius
real SmoothQuadraticDistanceAttenuation(real distSquare, real distRcp, real rangeAttenuationScale, real rangeAttenuationBias)
{
    // Becomes quadratic after the call to Sq().
    real attenuation  = min(distRcp, 1.0 / PUNCTUAL_LIGHT_THRESHOLD);
    attenuation *= InfluenceAttenuation(distSquare, rangeAttenuationScale, rangeAttenuationBias);
    return Sq(attenuation);
}

real SmoothQuadraticDistanceAttenuation(real3 unL, real rangeAttenuationScale, real rangeAttenuationBias)
{
    real distSquare = dot(unL, unL);
    real distRcp = rsqrt(distSquare);
    return SmoothQuadraticDistanceAttenuation(distSquare, distRcp, rangeAttenuationScale, rangeAttenuationBias);
}

real AngleAttenuation(real cosFwd, real lightAngleScale, real lightAngleOffset)
{
    return saturate(cosFwd * lightAngleScale + lightAngleOffset);
}

real SmoothAngleAttenuation(real cosFwd, real lightAngleScale, real lightAngleOffset)
{
    real attenuation = AngleAttenuation(cosFwd, lightAngleScale, lightAngleOffset);
    return Sq(attenuation);
}

real SmoothAngleAttenuation(real3 L, real3 lightFwdDir, real lightAngleScale, real lightAngleOffset)
{
    real cosFwd = dot(-L, lightFwdDir);
    return SmoothAngleAttenuation(cosFwd, lightAngleScale, lightAngleOffset);
}

// Combines SmoothQuadraticDistanceAttenuation() and SmoothAngleAttenuation() in an efficient manner.
// distances = {d, d^2, 1/d, d_proj}, where d_proj = dot(lightToSample, lightData.forward).
real SmoothPunctualLightAttenuation(real4 distances, real rangeAttenuationScale, real rangeAttenuationBias,
                                     real lightAngleScale, real lightAngleOffset)
{
    real distSq   = distances.y;
    real distRcp  = distances.z;
    real distProj = distances.w;
    real cosFwd   = distProj * distRcp;

    real attenuation  = min(distRcp, 1.0 / PUNCTUAL_LIGHT_THRESHOLD);
    attenuation *= InfluenceAttenuation(distSq, rangeAttenuationScale, rangeAttenuationBias);
    attenuation *= AngleAttenuation(cosFwd, lightAngleScale, lightAngleOffset);

    return Sq(attenuation);
}

// Applies SmoothInfluenceAttenuation() after transforming the attenuation ellipsoid into a sphere.
// If r = rsqrt(invSqRadius), then the ellipsoid is defined s.t. r1 = r / invAspectRatio, r2 = r3 = r.
// The transformation is performed along the major axis of the ellipsoid (corresponding to 'r1').
// Both the ellipsoid (e.i. 'axis') and 'unL' should be in the same coordinate system.
// 'unL' should be computed from the center of the ellipsoid.
real EllipsoidalDistanceAttenuation(real3 unL, real rangeAttenuationScale, real rangeAttenuationBias,
                                    real3 axis, real invAspectRatio)
{
    // Project the unnormalized light vector onto the axis.
    real projL = dot(unL, axis);

    // Transform the light vector instead of transforming the ellipsoid.
    real diff = projL - projL * invAspectRatio;
    unL -= diff * axis;

    real sqDist = dot(unL, unL);
    return SmoothInfluenceAttenuation(sqDist, rangeAttenuationScale, rangeAttenuationBias);
}

// Applies SmoothInfluenceAttenuation() using the axis-aligned ellipsoid of the given dimensions.
// Both the ellipsoid and 'unL' should be in the same coordinate system.
// 'unL' should be computed from the center of the ellipsoid.
real EllipsoidalDistanceAttenuation(real3 unL, real3 invHalfDim)
{
    // Transform the light vector so that we can work with
    // with the ellipsoid as if it was a unit sphere.
    unL *= invHalfDim;

    real sqDist = dot(unL, unL);
    return SmoothInfluenceAttenuation(sqDist, 1.0, 0.0);
}

// Applies SmoothInfluenceAttenuation() after mapping the axis-aligned box to a sphere.
// If the diagonal of the box is 'd', invHalfDim = rcp(0.5 * d).
// Both the box and 'unL' should be in the same coordinate system.
// 'unL' should be computed from the center of the box.
real BoxDistanceAttenuation(real3 unL, real3 invHalfDim)
{
    // Transform the light vector so that we can work with
    // with the box as if it was a [-1, 1]^2 cube.
    unL *= invHalfDim;

    // Our algorithm expects the input vector to be within the cube.
    if (Max3(abs(unL.x), abs(unL.y), abs(unL.z)) > 1.0) return 0.0;

    real sqDist = ComputeCubeToSphereMapSqMagnitude(unL);
    return SmoothInfluenceAttenuation(sqDist, 1.0, 0.0);
}

//-----------------------------------------------------------------------------
// IES Helper
//-----------------------------------------------------------------------------

real2 GetIESTextureCoordinate(real3x3 lightToWord, real3 L)
{
    // IES need to be sample in light space
    real3 dir = mul(lightToWord, -L); // Using matrix on left side do a transpose

    // convert to spherical coordinate
    real2 sphericalCoord; // .x is theta, .y is phi
    // Texture is encoded with cos(phi), scale from -1..1 to 0..1
    sphericalCoord.y = (dir.z * 0.5) + 0.5;
    real theta = atan2(dir.y, dir.x);
    sphericalCoord.x = theta * INV_TWO_PI;

    return sphericalCoord;
}

//-----------------------------------------------------------------------------
// Lighting functions
//-----------------------------------------------------------------------------

// Ref: Horizon Occlusion for Normal Mapped Reflections: http://marmosetco.tumblr.com/post/81245981087
real GetHorizonOcclusion(real3 V, real3 normalWS, real3 vertexNormal, real horizonFade)
{
    real3 R = reflect(-V, normalWS);
    real specularOcclusion = saturate(1.0 + horizonFade * dot(R, vertexNormal));
    // smooth it
    return specularOcclusion * specularOcclusion;
}

// Ref: Moving Frostbite to PBR - Gotanda siggraph 2011
// Return specular occlusion based on ambient occlusion (usually get from SSAO) and view/roughness info
real GetSpecularOcclusionFromAmbientOcclusion(real NdotV, real ambientOcclusion, real roughness)
{
    return saturate(PositivePow(NdotV + ambientOcclusion, exp2(-16.0 * roughness - 1.0)) - 1.0 + ambientOcclusion);
}

//forest-begin: lightmap occlusion
float3 SampleBakedGI(float3 positionWS, float3 normalWS, float2 uvStaticLightmap, float2 uvDynamicLightmap, float skyOcclusion, float grassOcclusion, float treeOcclusion);

float4 _LightmapOcclusionLuminanceMode;
float4 _LightmapOcclusionScalePowerReflStrengthSpecStrength;

float GetSpecularOcclusionFromLightmapLuminance(float3 V, float3 normalWS, float2 uvStaticLightmap, float2 uvDynamicLightmap)
{
#if !defined(LIGHTMAP_ON) && !defined(DYNAMICLIGHTMAP_ON)
    return 1.0;
#endif

	float3 lookupVector = _LightmapOcclusionLuminanceMode.w == 0 ? normalWS : reflect(-V, normalWS);;
	float3 bakedGI = SampleBakedGI(float3(0, 0, 0), lookupVector, uvStaticLightmap, uvDynamicLightmap, 1.f, 1.f, 1.f);
	float lightmapOcclusion = dot(bakedGI, _LightmapOcclusionLuminanceMode.rgb);
	lightmapOcclusion = pow(saturate(lightmapOcclusion * _LightmapOcclusionScalePowerReflStrengthSpecStrength.x), _LightmapOcclusionScalePowerReflStrengthSpecStrength.y);
	return lerp(1.f, lightmapOcclusion, _LightmapOcclusionScalePowerReflStrengthSpecStrength.z);
}
//forest-end

// ref: Practical Realtime Strategies for Accurate Indirect Occlusion
// Update ambient occlusion to colored ambient occlusion based on statitics of how light is bouncing in an object and with the albedo of the object
real3 GTAOMultiBounce(real visibility, real3 albedo)
{
    real3 a =  2.0404 * albedo - 0.3324;
    real3 b = -4.7951 * albedo + 0.6417;
    real3 c =  2.7552 * albedo + 0.6903;

    real x = visibility;
    return max(x, ((x * a + b) * x + c) * x);
}

// Based on Oat and Sander's 2008 technique
// Area/solidAngle of intersection of two cone
real SphericalCapIntersectionSolidArea(real cosC1, real cosC2, real cosB)
{
    real r1 = FastACos(cosC1);
    real r2 = FastACos(cosC2);
    real rd = FastACos(cosB);
    real area = 0.0;

    if (rd <= max(r1, r2) - min(r1, r2))
    {
        // One cap is completely inside the other
        area = TWO_PI - TWO_PI * max(cosC1, cosC2);
    }
    else if (rd >= r1 + r2)
    {
        // No intersection exists
        area = 0.0;
    }
    else
    {
        real diff = abs(r1 - r2);
        real den = r1 + r2 - diff;
        real x = 1.0 - saturate((rd - diff) / den);
        area = smoothstep(0.0, 1.0, x);
        area *= TWO_PI - TWO_PI * max(cosC1, cosC2);
    }

    return area;
}

// Ref: Steve McAuley - Energy-Conserving Wrapped Diffuse
real ComputeWrappedDiffuseLighting(real NdotL, real w)
{
    return saturate((NdotL + w) / ((1 + w) * (1 + w)));
}

//-----------------------------------------------------------------------------
// Helper functions
//-----------------------------------------------------------------------------

// Ref: "Crafting a Next-Gen Material Pipeline for The Order: 1886".
float ClampNdotV(float NdotV)
{
    return max(NdotV, 0.0001);
}

// return usual BSDF angle
void GetBSDFAngle(float3 V, float3 L, float NdotL, float unclampNdotV, out float LdotV, out float NdotH, out float LdotH, out float clampNdotV, out float invLenLV)
{
    // Optimized math. Ref: PBR Diffuse Lighting for GGX + Smith Microsurfaces (slide 114).
    LdotV = dot(L, V);
    invLenLV = rsqrt(max(2.0 * LdotV + 2.0, FLT_EPS));    // invLenLV = rcp(length(L + V)), clamp to avoid rsqrt(0) = NaN
    NdotH = saturate((NdotL + unclampNdotV) * invLenLV);        // Do not clamp NdotV here
    LdotH = saturate(invLenLV * LdotV + invLenLV);
    clampNdotV = ClampNdotV(unclampNdotV);
}

// Inputs:    normalized normal and view vectors.
// Outputs:   front-facing normal, and the new non-negative value of the cosine of the view angle.
// Important: call Orthonormalize() on the tangent and recompute the bitangent afterwards.
real3 GetViewReflectedNormal(real3 N, real3 V, out real NdotV)
{
    // Fragments of front-facing geometry can have back-facing normals due to interpolation,
    // normal mapping and decals. This can cause visible artifacts from both direct (negative or
    // extremely high values) and indirect (incorrect lookup direction) lighting.
    // There are several ways to avoid this problem. To list a few:
    //
    // 1. Setting { NdotV = max(<N,V>, SMALL_VALUE) }. This effectively removes normal mapping
    // from the affected fragments, making the surface appear flat.
    //
    // 2. Setting { NdotV = abs(<N,V>) }. This effectively reverses the convexity of the surface.
    // It also reduces light leaking from non-shadow-casting lights. Note that 'NdotV' can still
    // be 0 in this case.
    //
    // It's important to understand that simply changing the value of the cosine is insufficient.
    // For one, it does not solve the incorrect lookup direction problem, since the normal itself
    // is not modified. There is a more insidious issue, however. 'NdotV' is a constituent element
    // of the mathematical system describing the relationships between different vectors - and
    // not just normal and view vectors, but also light vectors, half vectors, tangent vectors, etc.
    // Changing only one angle (or its cosine) leaves the system in an inconsistent state, where
    // certain relationships can take on different values depending on whether 'NdotV' is used
    // in the calculation or not. Therefore, it is important to change the normal (or another
    // vector) in order to leave the system in a consistent state.
    //
    // We choose to follow the conceptual approach (2) by reflecting the normal around the
    // (<N,V> = 0) boundary if necessary, as it allows us to preserve some normal mapping details.

    NdotV = dot(N, V);

    // N = (NdotV >= 0.0) ? N : (N - 2.0 * NdotV * V);
    N += (2.0 * saturate(-NdotV)) * V;
    NdotV = abs(NdotV);

    return N;
}

// Generates an orthonormal (row-major) basis from a unit vector. TODO: make it column-major.
// The resulting rotation matrix has the determinant of +1.
// Ref: 'ortho_basis_pixar_r2' from http://marc-b-reynolds.github.io/quaternions/2016/07/06/Orthonormal.html
real3x3 GetLocalFrame(real3 localZ)
{
    real x  = localZ.x;
    real y  = localZ.y;
    real z  = localZ.z;
    real sz = FastSign(z);
    real a  = 1 / (sz + z);
    real ya = y * a;
    real b  = x * ya;
    real c  = x * sz;

    real3 localX = real3(c * x * a - 1, sz * b, c);
    real3 localY = real3(b, y * ya - sz, y);

    // Note: due to the quaternion formulation, the generated frame is rotated by 180 degrees,
    // s.t. if localZ = {0, 0, 1}, then localX = {-1, 0, 0} and localY = {0, -1, 0}.
    return real3x3(localX, localY, localZ);
}

// Generates an orthonormal (row-major) basis from a unit vector. TODO: make it column-major.
// The resulting rotation matrix has the determinant of +1.
real3x3 GetLocalFrame(real3 localZ, real3 localX)
{
    real3 localY = cross(localZ, localX);

    return real3x3(localX, localY, localZ);
}

#endif // UNITY_COMMON_LIGHTING_INCLUDED
