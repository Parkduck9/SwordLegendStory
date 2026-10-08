namespace SimpleBattle;

// 캐릭터 종류 대신 Character라는 공통 규격만 사용합니다.
// 전투 순서와 승패 처리를 맡으며, 콘솔 출력은 전달받은 함수를 사용합니다.
public sealed class BattleSystem
{
    private readonly Action<string> write;

    public BattleSystem(Action<string> write) => this.write = write;

    public Character Fight(Character player, Character enemy)
    {
        write($"전투 시작: {player.Name}(HP {player.Hp}) vs {enemy.Name}(HP {enemy.Hp})");
        int turn = 1;

        while (player.IsAlive && enemy.IsAlive)
        {
            write($"\n[{turn}턴]");
            Attack(player, enemy);

            // 쓰러진 적은 반격하지 않습니다.
            if (enemy.IsAlive)
                Attack(enemy, player);

            turn++;
        }

        Character winner = player.IsAlive ? player : enemy;
        write($"\n승리: {winner.Name}");
        return winner;
    }

    private void Attack(Character attacker, Character target)
    {
        // 핵심: Character 타입으로 호출해도 실제 객체의 override가 실행됩니다.
        int damage = attacker.GetAttackDamage();
        int applied = target.TakeDamage(damage);
        write($"{attacker.Name} → {target.Name}: 피해 {applied}, 남은 HP {target.Hp}");
    }
}
