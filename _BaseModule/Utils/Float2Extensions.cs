using Unity.Mathematics;
using UnityEngine;

namespace Belzont.Utils
{
    public static class Float2Extensions
    {
        public static float GetAngleToPoint(this float2 from, float2 to)
        {
            float ca = to.x - from.x;
            float co = -to.y + from.y;
            //LogUtils.DoLog($"ca = {ca},co = {co};");
            return co == 0 ?
                    ca < 0 ? 270
                    : 90
                : co < 0 ? 360 - (((Mathf.Atan(ca / co) * Mathf.Rad2Deg) + 360) % 360 % 360)
                : 360 - ((Mathf.Atan(ca / co) * Mathf.Rad2Deg + 180 + 360) % 360);
        }
        public static float2 DegreeToFloat2(float degree) => RadianToFloat2(degree * Mathf.Deg2Rad);
        public static float2 RadianToFloat2(float radian) => new(math.cos(radian), math.sin(radian));
        public static float ToDegrees(this float2 inputAngle)
        {
            var angle = math.atan2(inputAngle.y, inputAngle.x);
            if (angle < 0)
            {
                angle += math.PI * 2;
            }
            return angle / math.PI * 180f;
        }

        public static float2 PointAt(this float2 start, float angleDegrees, float distance) => start + (distance * DegreeToFloat2(angleDegrees));
    }
}
