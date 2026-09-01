namespace AK_Exusiai.Mechanics;

public interface IAmmoFreeAttack
{
}

public interface IMultiAmmoAttack
{
    int MaxAmmoSpend { get; }
}

public interface IAmmoDamageMultiplier
{
    decimal AmmoDamageMultiplier { get; }
}

public interface IAmmoSpendAllAttack
{
}

internal enum AmmoAttackMode
{
    Paid,
    Overloaded,
    Prepaid,
    Free,
}

internal sealed class AmmoAttackInfo(
    AmmoAttackMode mode,
    decimal ammoDamagePerHit,
    int spendLimit)
{
    public AmmoAttackMode Mode { get; } = mode;
    public decimal AmmoDamagePerHit { get; } = ammoDamagePerHit;
    public int SpendLimit { get; } = Math.Max(0, spendLimit);
    public decimal CurrentHitAmmoDamage { get; private set; }
    public int AmmoSpent { get; private set; }
    public int AmmoBackedHitCount { get; private set; }
    public decimal TotalAmmoDamage { get; private set; }
    public List<decimal> HitAmmoDamage { get; } = [];

    public bool HasAmmoBackedCurrentHit => CurrentHitAmmoDamage > 0m;

    public void ClearCurrentHit()
    {
        CurrentHitAmmoDamage = 0m;
    }

    public bool TryApplyPrepaidHit()
    {
        if (AmmoDamagePerHit <= 0m)
            return false;

        SetCurrentHit(AmmoDamagePerHit, spend: false);
        return true;
    }

    public async Task<bool> TrySpendForHit(
        MegaCrit.Sts2.Core.Entities.Players.Player player,
        string resourceId,
        MegaCrit.Sts2.Core.Models.CardModel card,
        MegaCrit.Sts2.Core.Models.AbstractModel source)
    {
        if (Mode == AmmoAttackMode.Overloaded)
        {
            if (AmmoDamagePerHit <= 0m || AmmoBackedHitCount >= SpendLimit)
                return false;

            SetCurrentHit(AmmoDamagePerHit, spend: false);
            return true;
        }

        if (Mode != AmmoAttackMode.Paid ||
            AmmoDamagePerHit <= 0m ||
            AmmoSpent >= SpendLimit ||
            STS2RitsuLib.Combat.SecondaryResources.SecondaryResourceCmd.Get(player, resourceId) <= 0)
        {
            return false;
        }

        if (!await STS2RitsuLib.Combat.SecondaryResources.SecondaryResourceCmd.Spend(
                player,
                resourceId,
                1,
                card,
                source))
        {
            return false;
        }

        SetCurrentHit(AmmoDamagePerHit, spend: true);
        return true;
    }

    public async Task<bool> TrySpendAllForHit(
        MegaCrit.Sts2.Core.Entities.Players.Player player,
        string resourceId,
        MegaCrit.Sts2.Core.Models.CardModel card,
        MegaCrit.Sts2.Core.Models.AbstractModel source)
    {
        if (Mode == AmmoAttackMode.Overloaded)
        {
            if (AmmoDamagePerHit <= 0m || AmmoBackedHitCount >= SpendLimit)
                return false;

            SetCurrentHit(AmmoDamagePerHit * SpendLimit, spend: false);
            return true;
        }

        if (Mode != AmmoAttackMode.Paid || AmmoDamagePerHit <= 0m || AmmoSpent > 0)
            return false;

        int ammo = Math.Min(
            SpendLimit,
            STS2RitsuLib.Combat.SecondaryResources.SecondaryResourceCmd.Get(player, resourceId));
        if (ammo <= 0)
            return false;

        if (!await STS2RitsuLib.Combat.SecondaryResources.SecondaryResourceCmd.Spend(
                player,
                resourceId,
                ammo,
                card,
                source))
        {
            return false;
        }

        SetCurrentHit(AmmoDamagePerHit * ammo, spend: true, spentAmount: ammo);
        return true;
    }

    private void SetCurrentHit(decimal ammoDamage, bool spend, int spentAmount = 1)
    {
        CurrentHitAmmoDamage = ammoDamage;
        AmmoBackedHitCount++;
        TotalAmmoDamage += ammoDamage;
        HitAmmoDamage.Add(ammoDamage);
        if (spend)
            AmmoSpent += spentAmount;
    }
}
