using Unity.Mathematics;
using UnityEngine;

namespace Belzont.Utils
{
    public static class FloatNExtensions
    {
        public static float GetAngleXZ(this float3 dir) => math.atan2(dir.z, dir.x) * math.TODEGREES;
        public static float SqrDistance(this float3 a, float3 b)
        {
            float3 vector = a - b;
            return (vector.x * vector.x) + (vector.y * vector.y) + (vector.z * vector.z);
        }

        public static float SqrDistance(this float2 a, float2 b)
        {
            var vector = a - b;
            return (vector.x * vector.x) + (vector.y * vector.y);
        }

        public static float[] ToArray(this float3 f) => new[] { f.x, f.y, f.z };
    }
}
