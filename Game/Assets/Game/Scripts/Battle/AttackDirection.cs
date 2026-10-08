namespace SwordPrototype.Battle
{
    public enum AttackDirection { Up, UpRight, Right, DownRight, Down, DownLeft, Left, UpLeft }

    /// <summary>좌우 막힘 규칙의 묶음(↖←↙ / ↗→↘). ↑↓는 어느 쪽에도 속하지 않는다.</summary>
    public enum AttackSide { None, Left, Right }

    public static class AttackDirectionExtensions
    {
        public static AttackSide Side(this AttackDirection direction)
        {
            switch (direction)
            {
                case AttackDirection.UpLeft:
                case AttackDirection.Left:
                case AttackDirection.DownLeft: return AttackSide.Left;
                case AttackDirection.UpRight:
                case AttackDirection.Right:
                case AttackDirection.DownRight: return AttackSide.Right;
                default: return AttackSide.None;
            }
        }
    }
}
