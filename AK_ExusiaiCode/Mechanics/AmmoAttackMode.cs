namespace AK_Exusiai.Mechanics;

public interface IAmmoFreeAttack
{
}

public interface IMultiAmmoAttack
{
    int MaxAmmoSpend { get; }
}

public interface IExtraAmmoTriggerPreview
{
    int ExtraAmmoTriggers { get; }
}

internal enum AmmoAttackMode
{
    Paid,
    Prepaid,
    Free,
}

internal readonly record struct AmmoAttackInfo(AmmoAttackMode Mode, int Multiplier);
