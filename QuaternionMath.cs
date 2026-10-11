using UnityEngine;

namespace QuaternionDemo
{
    /// <summary>
    /// 四元数的基础数学工具。
    /// 这里故意手动实现常用公式，方便在课程演示中说明原理。
    /// </summary>
    public static class QuaternionMath
    {
        /// <summary>
        /// 由“旋转轴 + 旋转角度”构造单位四元数。
        /// q = [axis * sin(theta / 2), cos(theta / 2)]
        /// </summary>
        public static Quaternion FromAxisAngle(Vector3 axis, float angleDegrees)
        {
            if (axis.sqrMagnitude < 0.000001f)
            {
                axis = Vector3.up;
            }

            axis.Normalize();

            float halfAngleRadians = angleDegrees * Mathf.Deg2Rad * 0.5f;
            float sinHalfAngle = Mathf.Sin(halfAngleRadians);
            float cosHalfAngle = Mathf.Cos(halfAngleRadians);

            Quaternion result = new Quaternion(
                axis.x * sinHalfAngle,
                axis.y * sinHalfAngle,
                axis.z * sinHalfAngle,
                cosHalfAngle);

            return Normalize(result);
        }

        /// <summary>
        /// 手动计算四元数乘法 lhs * rhs。
        /// 在 Unity 中，lhs * rhs 表示先执行 rhs，再执行 lhs。
        /// </summary>
        public static Quaternion Multiply(Quaternion lhs, Quaternion rhs)
        {
            Quaternion result = new Quaternion(
                lhs.w * rhs.x + lhs.x * rhs.w + lhs.y * rhs.z - lhs.z * rhs.y,
                lhs.w * rhs.y - lhs.x * rhs.z + lhs.y * rhs.w + lhs.z * rhs.x,
                lhs.w * rhs.z + lhs.x * rhs.y - lhs.y * rhs.x + lhs.z * rhs.w,
                lhs.w * rhs.w - lhs.x * rhs.x - lhs.y * rhs.y - lhs.z * rhs.z);

            return Normalize(result);
        }

        /// <summary>
        /// 使用 q * p * q^-1 旋转三维向量。
        /// 下面使用的是该公式展开后的等价形式。
        /// </summary>
        public static Vector3 RotateVector(Quaternion rotation, Vector3 vector)
        {
            rotation = Normalize(rotation);

            Vector3 u = new Vector3(rotation.x, rotation.y, rotation.z);
            float scalar = rotation.w;

            return 2f * Vector3.Dot(u, vector) * u
                   + (scalar * scalar - Vector3.Dot(u, u)) * vector
                   + 2f * scalar * Vector3.Cross(u, vector);
        }

        /// <summary>
        /// 对四元数进行归一化，使其长度为 1。
        /// 单位四元数才能表示纯旋转。
        /// </summary>
        public static Quaternion Normalize(Quaternion quaternion)
        {
            float length = Mathf.Sqrt(
                quaternion.x * quaternion.x
                + quaternion.y * quaternion.y
                + quaternion.z * quaternion.z
                + quaternion.w * quaternion.w);

            if (length < 0.000001f)
            {
                return Quaternion.identity;
            }

            float inverseLength = 1f / length;
            return new Quaternion(
                quaternion.x * inverseLength,
                quaternion.y * inverseLength,
                quaternion.z * inverseLength,
                quaternion.w * inverseLength);
        }

        /// <summary>
        /// 返回四元数的模长。单位四元数的模长应接近 1。
        /// </summary>
        public static float Magnitude(Quaternion quaternion)
        {
            return Mathf.Sqrt(
                quaternion.x * quaternion.x
                + quaternion.y * quaternion.y
                + quaternion.z * quaternion.z
                + quaternion.w * quaternion.w);
        }

        /// <summary>
        /// 判断两个四元数是否表示相同旋转。
        /// q 和 -q 虽然数值不同，但表示同一个空间旋转。
        /// </summary>
        public static bool RepresentsSameRotation(
            Quaternion first,
            Quaternion second,
            float tolerance = 0.0001f)
        {
            first = Normalize(first);
            second = Normalize(second);

            float absoluteDot = Mathf.Abs(Quaternion.Dot(first, second));
            return 1f - absoluteDot <= tolerance;
        }
    }
}
