using UnityEngine;
using SwordPrototype.Battle;

namespace SwordPrototype.Enemy
{
    /// <summary>적 투사체(투척·후퇴 사격). 유도 없이 직진. 적이 정지한 동안 함께 멈춘다. 패링·대쉬 무적은 Health 경로로 처리.</summary>
    public sealed class EnemyProjectile : MonoBehaviour
    {
        private BattleFlow flow;
        private Transform target;
        private Vector3 velocity;
        private float radius;
        private float damage;
        private string label;
        private float life = 2f;

        public static void Spawn(BattleFlow flow, Transform target, Vector3 position, Vector3 direction, float speed, float radius, float damage, string label)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "EnemyProjectile";
            Destroy(go.GetComponent<Collider>());
            go.transform.localScale = new Vector3(0.12f, 0.12f, 0.6f);
            Vector3 dir = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            go.transform.SetPositionAndRotation(position, Quaternion.LookRotation(dir));
            go.GetComponent<Renderer>().material.color = new Color(0.9f, 0.85f, 0.8f);
            Audio.Sound.Play(Audio.Sfx.EnemyThrow, position);
            var p = go.AddComponent<EnemyProjectile>();
            p.flow = flow; p.target = target; p.velocity = dir * speed; p.radius = radius; p.damage = damage; p.label = label;
        }

        private void Update()
        {
            if (flow == null || flow.State == BattleState.Victory || flow.State == BattleState.Defeat) { Destroy(gameObject); return; }
            if (!flow.EnemyMayAct) return;
            float dt = Time.deltaTime;
            Vector3 from = transform.position, to = from + velocity * dt;
            life -= dt;
            // R2: 이번 프레임 경로 전체를 앞에서부터 훑어 먼저 닿은 쪽(엄폐/몸)을 처리. 몸은 실제 캡슐(높이 포함).
            if (body == null) body = target.GetComponent<CharacterController>();
            Body(out Vector3 a, out Vector3 b, out float bodyRadius);
            switch (Sweep(from, to, radius, a, b, bodyRadius, World.PropBreaker.Blocked))
            {
                case SweepResult.Blocked:   // S6: 소품 엄폐
                    flow.AddLog($"적 {label}: 엄폐에 막힘");
                    Destroy(gameObject);
                    return;
                case SweepResult.Body:
                    EnemyBrain.ReportHit(flow, label, damage);
                    Destroy(gameObject);
                    return;
            }
            transform.position = to;
            if (life <= 0f) Destroy(gameObject);
        }

        private CharacterController body;

        // 플레이어 몸 캡슐(아래·위 구 중심과 반지름). 컨트롤러가 없으면 발 위치 기준 기본 크기.
        private void Body(out Vector3 a, out Vector3 b, out float r)
        {
            float height = body != null ? body.height : 2.1f;
            r = body != null ? body.radius : 0.35f;
            Vector3 center = body != null ? target.TransformPoint(body.center) : target.position + Vector3.up * height * 0.5f;
            float half = Mathf.Max(0f, height * 0.5f - r);
            a = center - Vector3.up * half;
            b = center + Vector3.up * half;
        }

        public enum SweepResult { None, Blocked, Body }

        /// <summary>
        /// 경로 from→to를 투사체 반지름 간격으로 나눠 앞에서부터 검사한다(빠른 이동·낮은 프레임에서도 통과 없음).
        /// 같은 지점에서는 엄폐를 먼저 본다. 몸은 선분(a-b)+반지름 캡슐.
        /// </summary>
        public static SweepResult Sweep(Vector3 from, Vector3 to, float projectileRadius, Vector3 a, Vector3 b, float bodyRadius, System.Func<Vector3, bool> blocked)
        {
            float step = Mathf.Max(0.05f, projectileRadius);
            int count = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(from, to) / step));
            for (int i = 0; i <= count; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, i / (float)count);
                if (blocked != null && blocked(p)) return SweepResult.Blocked;
                if (DistanceToSegment(p, a, b) <= bodyRadius + projectileRadius) return SweepResult.Body;
            }
            return SweepResult.None;
        }

        private static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
            return Vector3.Distance(p, a + ab * t);
        }
    }
}
