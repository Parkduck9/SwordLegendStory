using UnityEngine;

namespace SwordPrototype.Battle
{
    /// <summary>
    /// 일시정지 메뉴 시간을 뺀 전투 시계(실제 시간 기준, 슬로모션 영향 없음).
    /// 대쉬 충전·시간 기반 무적·전투 기록이 같은 기준을 쓰도록 한 곳에 모은다(review R1).
    /// 선택 화면·교환 연출은 메뉴를 열 수 없으므로 기존 실제 시간 규칙과 같다.
    /// </summary>
    public static class BattleClock
    {
        private static float pausedTotal;
        private static float pauseStart = -1f;

        public static bool Paused => pauseStart >= 0f;
        /// <summary>메뉴 시간을 뺀 현재 전투 시각.</summary>
        public static float Now => NowAt(Time.unscaledTime);
        /// <summary>이번 프레임 전투 실제 시간 간격(메뉴 중 0).</summary>
        public static float RealDelta => Paused ? 0f : Time.unscaledDeltaTime;

        public static void SetPaused(bool value) => SetPaused(value, Time.unscaledTime);

        // 시각을 받는 판(검사용)
        public static void SetPaused(bool value, float unscaledNow)
        {
            if (value == Paused) return;
            if (value) pauseStart = unscaledNow;
            else { pausedTotal += unscaledNow - pauseStart; pauseStart = -1f; }
        }

        public static float NowAt(float unscaledNow) => (Paused ? pauseStart : unscaledNow) - pausedTotal;

        /// <summary>씬 시작·재시작 시 정지 상태를 남기지 않는다.</summary>
        public static void Reset() { pausedTotal = 0f; pauseStart = -1f; }
    }
}
