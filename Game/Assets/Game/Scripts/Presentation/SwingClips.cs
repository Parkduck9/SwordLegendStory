using SwordPrototype.Battle;

namespace SwordPrototype.Presentation
{
    /// <summary>
    /// R9: 8방향 베기에 배정한 실제 한손 검 동작(KayKit·Quaternius UAL, CC0) 표. 고칠 때는 이 표만 바꾼다.
    /// start·contact·end는 동작의 정규화 시간: 시작 지점 → 칼이 가장 빠른 순간(맞는 순간) → 겨눔 자세로 넘기는 지점.
    /// 값은 md/r9-swing-paths.txt 칼끝 궤적에서 정했다. 한손 동작은 좌우 반전하면 왼손이 베므로 반전은 쓰지 않는다.
    /// 판정 규칙은 없다(보이는 부분만).
    /// </summary>
    public static class SwingClips
    {
        public readonly struct Entry
        {
            public readonly string state, clip;
            public readonly float length, start, contact, end;
            public Entry(string state, string clip, float length, float start, float contact, float end)
            { this.state = state; this.clip = clip; this.length = length; this.start = start; this.contact = contact; this.end = end; }

            /// <summary>준비부터 맞는 순간까지 contactSeconds가 걸리게 하는 재생 속도.</summary>
            public float Speed(float contactSeconds) => (contact - start) * length / UnityEngine.Mathf.Max(0.01f, contactSeconds);
            /// <summary>그 속도로 시작 지점부터 겨눔으로 넘기는 지점까지 걸리는 실제 시간.</summary>
            public float Seconds(float contactSeconds) => (end - start) * length / Speed(contactSeconds);
        }

        // AttackDirection 순서: Up, UpRight, Right, DownRight, Down, DownLeft, Left, UpLeft
        public static readonly Entry[] Table =
        {
            // 모두 Quaternius UAL1·2(CC0). KayKit 한손 동작은 이 몸에서 팔이 뻣뻣하게 벌어져(미리보기 확인) 쓰지 않음.
            new Entry("Cut_Up", "Sword_Regular_A", 0.43f, 0.20f, 0.62f, 0.85f),          // 뒤로 젖힌 뒤 오른쪽 아래 → 머리 위로 올려 베기
            new Entry("Cut_UpRight", "Sword_Regular_Combo", 3.00f, 0.18f, 0.245f, 0.29f), // 연속 베기 둘째 수: 왼쪽 아래 → 오른쪽 위로 올려 베기
            new Entry("Cut_Right", "Sword_Dash", 1.57f, 0.12f, 0.225f, 0.32f),           // 내디디며 왼쪽 → 오른쪽 가로 베기
            new Entry("Cut_DownRight", "Sword_Regular_B", 0.53f, 0.10f, 0.42f, 0.48f),   // 왼쪽 위 → 아래로 감아 오른쪽 아래로
            new Entry("Cut_Down", "Sword_Regular_Combo", 3.00f, 0.10f, 0.18f, 0.22f),    // 연속 베기 첫 수 끝: 머리 위 → 오른쪽 앞 아래로 내려 베기(Heavy_Combo는 몸이 뒤로 돌아 제외)
            new Entry("Cut_DownLeft", "Sword_Attack", 1.53f, 0.15f, 0.28f, 0.40f),       // 오른쪽 위 → 왼쪽 아래 크게
            new Entry("Cut_Left", "Sword_Regular_Combo", 3.00f, 0.33f, 0.40f, 0.45f),    // 연속 베기 셋째 수: 오른쪽 → 왼쪽 가로 베기
            new Entry("Cut_UpLeft", "Sword_Regular_A", 0.43f, 0.20f, 0.58f, 0.64f),      // 올려 베기 앞부분: 오른쪽 아래 → 왼쪽 위
        };

        public static Entry Of(AttackDirection direction) => Table[(int)direction];
    }
}
