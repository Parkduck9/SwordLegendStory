using System;
using UnityEngine;

namespace SwordPrototype.Enemy
{
    public enum ShapeKind { Fan, Line, Circle }

    /// <summary>
    /// 적 공격 범위. 바닥 예고 표시(TelegraphView)와 실제 판정이 같은 값을 쓴다.
    /// 모든 계산은 수평면 기준.
    /// </summary>
    [Serializable]
    public struct AttackShape
    {
        public ShapeKind kind;
        public float radius;   // Fan·Circle 반경
        public float angle;    // Fan 전체 각도(도)
        public float width;    // Line 폭
        public float length;   // Line 길이

        public static AttackShape Fan(float radius, float angle) => new AttackShape { kind = ShapeKind.Fan, radius = radius, angle = angle };
        public static AttackShape Line(float width, float length) => new AttackShape { kind = ShapeKind.Line, width = width, length = length };
        public static AttackShape Circle(float radius) => new AttackShape { kind = ShapeKind.Circle, radius = radius };

        /// <summary>targetRadius는 맞는 쪽 몸 반경(플레이어 0.35m).</summary>
        public bool Contains(Vector3 origin, Vector3 forward, Vector3 point, float targetRadius = 0f)
        {
            Vector3 d = Vector3.ProjectOnPlane(point - origin, Vector3.up);
            Vector3 f = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            switch (kind)
            {
                case ShapeKind.Fan:
                    if (d.magnitude > radius + targetRadius) return false;
                    return d.sqrMagnitude < 0.01f || Vector3.Angle(f, d) <= angle * 0.5f;
                case ShapeKind.Line:
                    float along = Vector3.Dot(d, f);
                    float side = Mathf.Abs(Vector3.Dot(d, Vector3.Cross(Vector3.up, f)));
                    return along >= -targetRadius && along <= length + targetRadius && side <= width * 0.5f + targetRadius;
                default:
                    return d.magnitude <= radius + targetRadius;
            }
        }
    }
}
