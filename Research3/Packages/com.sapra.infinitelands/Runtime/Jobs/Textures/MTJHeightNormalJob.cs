using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using Unity.Collections.LowLevel.Unsafe;

namespace sapra.InfiniteLands
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    internal struct MTJHeightJob : IJobFor
    {
        [ReadOnly] NativeArray<Vertex4> vertices;

        [NativeDisableContainerSafetyRestriction]
        NativeArray<Color> normalColor;


        int maxTextureResolution;

        public void Execute(int i)
        {
            Vertex4 v = vertices[i];
            float4 a = new float4(v.v0.position.y, v.v1.position.y, v.v2.position.y, v.v3.position.y);
            float3x4 n = new float3x4(v.v0.normal, v.v1.normal, v.v2.normal, v.v3.normal);

            float4x3 tn = transpose(n);
            float4x4 basics = float4x4(tn.c0, tn.c1, tn.c2, a);
            basics = transpose(basics);

            int maxcount = min(maxTextureResolution - i * 4, 4);
            for (int x = 0; x < maxcount; x++)
            {
                normalColor[i * 4 + x] = JobExtensions.toColor(basics[x]);
            }
        }

        public static JobHandle ScheduleParallel(NativeArray<Vertex4> vertices, NativeArray<Color> textureArray,
            int resolution, JobHandle dependency)
        {
            return new MTJHeightJob()
            {
                normalColor = textureArray,
                vertices = vertices,
                maxTextureResolution = (resolution + 1) * (resolution + 1)
            }.ScheduleParallel(vertices.Length, resolution, dependency);
        }
    }
    
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    internal struct MTJHeightNormalizedJob : IJobFor
    {
        [ReadOnly] NativeArray<Vertex4> vertices;

        [NativeDisableContainerSafetyRestriction]
        NativeArray<Color> normalColor;

        float2 minMaxValue;

        int maxTextureResolution;

        public void Execute(int i)
        {
            Vertex4 v = vertices[i];
            if (minMaxValue.y - minMaxValue.x != 0)
            {
                float4 a = JobExtensions.invLerp(minMaxValue.x, minMaxValue.y, new float4(v.v0.position.y, v.v1.position.y, v.v2.position.y, v.v3.position.y));
                float3x4 n = new float3x4(v.v0.normal, v.v1.normal, v.v2.normal, v.v3.normal);

                float4x3 tn = transpose(n);
                float4x4 basics = float4x4(tn.c0, tn.c1, tn.c2, a);
                basics = transpose(basics);

                int maxcount = min(maxTextureResolution - i * 4, 4);
                for (int x = 0; x < maxcount; x++)
                {
                    normalColor[i * 4 + x] = JobExtensions.toColor(basics[x]);
                }
            }
        }

        public static JobHandle ScheduleParallel(NativeArray<Vertex4> vertices, NativeArray<Color> textureArray,
            float2 minMaxValue,
            int resolution, JobHandle dependency)
        {
            return new MTJHeightNormalizedJob()
            {
                normalColor = textureArray,
                vertices = vertices,
                minMaxValue = minMaxValue,
                maxTextureResolution = (resolution + 1) * (resolution + 1)
            }.ScheduleParallel(vertices.Length, resolution, dependency);
        }
    }
}