using UnityEngine;
using SwordPrototype.Battle;

namespace SwordPrototype.Presentation
{
    /// <summary>
    /// R6: 판정 검 자세(8방향)를 실제 검술 겨눔 자세로 보여 주는 표. 보이는 것만 담당(판정 무관).
    /// 묶음: ↓↙↘ 하단·와키가마에(낮게, 몸 옆·뒤) / ↑↖↗ 상단·팔상(세워 들기) / ←→ 중단(칼끝을 적에게).
    /// 손 위치는 키 1.8m 캐릭터 루트(발) 기준 미터, 칼 방향은 캐릭터 기준(x 오른쪽, y 위, z 앞). 값을 고치면 바로 바뀐다.
    /// </summary>
    public static class Kamae
    {
        public enum Family { Low, High, Middle }

        public struct Pose
        {
            public Vector3 hand;
            public Vector3 blade;
            public Family family;
            public Pose(Vector3 hand, Vector3 blade, Family family) { this.hand = hand; this.blade = blade.normalized; this.family = family; }
        }

        // AttackDirection 순서: Up, UpRight, Right, DownRight, Down, DownLeft, Left, UpLeft
        private static readonly Pose[] Poses =
        {
            new Pose(new Vector3(0.10f, 1.72f, 0.18f), new Vector3(0f, 0.6f, -0.8f), Family.High),          // ↑ 상단: 이마 앞 위, 칼끝 뒤로
            new Pose(new Vector3(0.26f, 1.52f, 0.20f), new Vector3(0.1f, 0.97f, -0.2f), Family.High),        // ↗ 팔상: 오른 어깨 앞에 세움
            new Pose(new Vector3(0.18f, 1.10f, 0.36f), new Vector3(0.3f, 0.3f, 0.9f), Family.Middle),        // → 중단: 칼끝 적에게, 오른쪽으로 기울임
            new Pose(new Vector3(0.30f, 0.95f, -0.02f), new Vector3(0.35f, -0.45f, -0.82f), Family.Low),     // ↘ 와키가마에: 오른쪽 낮게 뒤로(기본)
            new Pose(new Vector3(0.12f, 0.98f, 0.30f), new Vector3(0.05f, -0.45f, 0.89f), Family.Low),       // ↓ 하단: 앞으로 낮게
            new Pose(new Vector3(-0.05f, 0.98f, 0.22f), new Vector3(-0.55f, -0.45f, 0.7f), Family.Low),      // ↙ 왼쪽 낮게
            new Pose(new Vector3(0.02f, 1.10f, 0.36f), new Vector3(-0.3f, 0.3f, 0.9f), Family.Middle),       // ← 중단: 왼쪽으로 기울임
            new Pose(new Vector3(-0.12f, 1.50f, 0.15f), new Vector3(-0.15f, 0.95f, -0.25f), Family.High),    // ↖ 왼 어깨 팔상
        };

        public static Pose Of(AttackDirection d) => Poses[(int)d];

        /// <summary>R6 적 피격 단계: 0 스침 · 1 베임 · 2 깊게.</summary>
        public static int WoundTier(StrikeOutcome o)
        {
            if (o.critical || o.finisher || o.counter || o.dashSlash) return 2;
            if (o.sameDirectionSecond || o.damageToEnemy >= 15f) return 1;
            return 0;
        }
    }
}
