namespace SimpleBattle;

// : Character → 상속. 이름, 체력, 피격 기능을 물려받습니다.
public sealed class Warrior : Character
{
    public Warrior(string name) : base(name, 50) { }

    // override → 부모의 가상함수를 검사에 맞게 재정의합니다.
    public override int GetAttackDamage() => 12;
}

public sealed class Mage : Character
{
    public Mage(string name) : base(name, 30) { }
    public override int GetAttackDamage() => 18;
}

public sealed class Slime : Character
{
    public Slime(string name) : base(name, 35) { }
    public override int GetAttackDamage() => 6;
}
