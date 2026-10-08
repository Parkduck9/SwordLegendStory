using System;
using UnityEngine;

namespace SwordPrototype.Battle
{
    /// <summary>시작 스탯 5종 (각 0~3, 총 10포인트 배분).</summary>
    [Serializable]
    public struct CharacterStats
    {
        public const int MaxLevel = 3;
        public const int TotalPoints = 10;

        [SerializeField, Range(0, MaxLevel)] private int health;
        [SerializeField, Range(0, MaxLevel)] private int stamina;
        [SerializeField, Range(0, MaxLevel)] private int speed;
        [SerializeField, Range(0, MaxLevel)] private int strength;
        [SerializeField, Range(0, MaxLevel)] private int special;

        public CharacterStats(int health, int stamina, int speed, int strength, int special)
        {
            this.health = health;
            this.stamina = stamina;
            this.speed = speed;
            this.strength = strength;
            this.special = special;
        }

        public int Health => health;
        public int Stamina => stamina;
        public int Speed => speed;
        public int Strength => strength;
        public int Special => special;
        public int PointsUsed => health + stamina + speed + strength + special;

        // 7차 답변: 10포인트를 다 쓰지 않아도 시작 가능(0포인트는 하드코어). 초과 사용만 막는다.
        public bool IsValid(out string reason)
        {
            if (!InRange(health) || !InRange(stamina) || !InRange(speed) || !InRange(strength) || !InRange(special))
            { reason = "each stat must be 0~3"; return false; }
            if (PointsUsed > TotalPoints) { reason = "more than 10 points used"; return false; }
            reason = null;
            return true;
        }

        private static bool InRange(int level) => level >= 0 && level <= MaxLevel;
    }
}
