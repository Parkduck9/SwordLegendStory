namespace SimpleBattle;

public static class Program
{
    public static void Main(string[] args)
    {
        // Main은 어떤 실제 객체를 쓸지 선택하고 연결합니다.
        // mage 인수를 주면 마법사, 생략하면 검사입니다.
        Character player = args.Length > 0 && args[0] == "mage"
            ? new Mage("마법사")
            : new Warrior("검사");
        Character enemy = new Slime("슬라임");

        var battle = new BattleSystem(Console.WriteLine);
        battle.Fight(player, enemy);
    }
}
