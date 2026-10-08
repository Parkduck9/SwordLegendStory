namespace SimpleBattle;

// 모든 캐릭터가 공유하는 데이터와 기능입니다.
// abstract: 공통 틀이므로 new Character()로 직접 만들지 않습니다.
public abstract class Character
{
    public string Name { get; }
    public int Hp { get; private set; }
    public bool IsAlive => Hp > 0;

    protected Character(string name, int hp)
    {
        Name = name;
        Hp = hp;
    }

    // virtual: 기본 동작을 제공하고, 자식이 원하는 동작으로 바꿀 수 있습니다.
    public virtual int GetAttackDamage() => 5;

    // 체력 처리 규칙은 모든 자식이 그대로 사용합니다.
    public int TakeDamage(int damage)
    {
        int actualDamage = Math.Min(Hp, Math.Max(0, damage));
        Hp -= actualDamage;
        return actualDamage;
    }
}
