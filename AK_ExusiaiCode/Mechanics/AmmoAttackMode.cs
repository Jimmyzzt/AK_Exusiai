namespace AK_Exusiai.Mechanics;

public interface IAmmoFreeAttack
{
}

public interface IAmmoDamageMultiplier
{
    decimal AmmoDamageMultiplier { get; }
}

public interface IAmmoSpendAllAttack
{
}

internal interface IOverloadAmmoSpendListener
{
    Task AfterOverloadAmmoSpent(int amount);
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
    private decimal? _spendAllAmmoDamage;

    public AmmoAttackMode Mode { get; } = mode;
    public decimal AmmoDamagePerHit { get; } = ammoDamagePerHit;
    public int SpendLimit { get; } = Math.Max(0, spendLimit);
    public decimal CurrentHitAmmoDamage { get; private set; }
    public int CurrentHitLogicalAmmoSpent { get; private set; }
    public int AmmoSpent { get; private set; }
    public int AmmoBackedHitCount { get; private set; }
    public decimal TotalAmmoDamage { get; private set; }
    public List<decimal> HitAmmoDamage { get; } = [];

    public bool HasAmmoBackedCurrentHit => CurrentHitAmmoDamage > 0m;

    public void ClearCurrentHit()
    {
        CurrentHitAmmoDamage = 0m;
        CurrentHitLogicalAmmoSpent = 0;
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

            SetCurrentHit(AmmoDamagePerHit, spend: false, logicalSpent: 1);
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
        if (_spendAllAmmoDamage is { } cachedDamage)
        {
            SetCurrentHit(cachedDamage, spend: false);
            return true;
        }

        if (Mode == AmmoAttackMode.Overloaded)
        {
            if (AmmoDamagePerHit <= 0m || SpendLimit <= 0)
                return false;

            // Overload prevents the actual payment but preserves the full
            // spend-all damage calculation (30 Ammo at the normal cap).
            _spendAllAmmoDamage = AmmoDamagePerHit * SpendLimit;
            SetCurrentHit(
                _spendAllAmmoDamage.Value,
                spend: false,
                logicalSpent: SpendLimit);
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

        _spendAllAmmoDamage = AmmoDamagePerHit * ammo;
        SetCurrentHit(_spendAllAmmoDamage.Value, spend: true, spentAmount: ammo);
        return true;
    }

    private void SetCurrentHit(
        decimal ammoDamage,
        bool spend,
        int spentAmount = 1,
        int logicalSpent = 0)
    {
        CurrentHitAmmoDamage = ammoDamage;
        CurrentHitLogicalAmmoSpent = Math.Max(0, logicalSpent);
        AmmoBackedHitCount++;
        TotalAmmoDamage += ammoDamage;
        HitAmmoDamage.Add(ammoDamage);
        if (spend)
            AmmoSpent += spentAmount;
    }
}
