using UnityEngine;

namespace SwordPrototype.Presentation
{
    /// <summary>타격감 출력: 충돌 불꽃·성공 파편(임시 파티클). 히트스톱·흔들림은 ExchangeDirector·카메라가 처리.</summary>
    public sealed class HitFeedback
    {
        public static readonly Color Spark = new Color(1f, 0.85f, 0.4f);
        public static readonly Color Fragment = new Color(0.85f, 0.15f, 0.12f);
        private readonly ParticleSystem particles;

        public HitFeedback()
        {
            var go = new GameObject("HitFeedback");
            particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = 0.3f;
            main.startSpeed = 5f;
            main.startSize = 0.07f;
            main.gravityModifier = 1.2f;
            main.useUnscaledTime = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        public void Burst(Vector3 position, Color color, int count)
        {
            var emit = new ParticleSystem.EmitParams { position = position, startColor = color, applyShapeToPosition = true };
            particles.Emit(emit, count);
        }
    }
}
