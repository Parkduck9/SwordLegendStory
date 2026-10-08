using System.Collections.Generic;
using UnityEngine;
using SwordPrototype.Enemy;

namespace SwordPrototype.World
{
    /// <summary>S6: 적 공격 범위(예고와 같은 AttackShape)나 적 몸통과 겹친 소품을 부순다. 플레이어 공격은 호출하지 않는다.</summary>
    public static class PropBreaker
    {
        public static int BreakInShape(AttackShape shape, Vector3 origin, Vector3 forward)
            => BreakInShape(DestructibleProp.Active, shape, origin, forward);

        public static int BreakInShape(IReadOnlyList<DestructibleProp> props, AttackShape shape, Vector3 origin, Vector3 forward)
        {
            int count = 0;
            // 부수면 목록에서 빠지므로 복사본으로 순회
            foreach (DestructibleProp p in new List<DestructibleProp>(props))
            {
                if (p == null || p.Broken) continue;
                if (!shape.Contains(origin, forward, p.transform.position, p.Radius)) continue;
                p.Break();
                count++;
            }
            return count;
        }

        public static int BreakTouching(Vector3 position, float bodyRadius)
            => BreakInShape(DestructibleProp.Active, AttackShape.Circle(bodyRadius), position, Vector3.forward);

        /// <summary>투사체가 소품에 막혔는지.</summary>
        public static bool Blocked(Vector3 point)
        {
            foreach (DestructibleProp p in DestructibleProp.Active)
                if (p != null && p.Blocks(point)) return true;
            return false;
        }
    }
}
