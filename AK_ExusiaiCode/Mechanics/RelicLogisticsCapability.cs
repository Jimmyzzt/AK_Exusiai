using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;

namespace AK_Exusiai.Mechanics;

/// <summary>
/// Persistent Delivery/Transit state attached to a mutable relic instance.
/// The capability deliberately does not listen to hooks; inactive relics are
/// filtered from the vanilla run/combat hook streams by RelicLogisticsHookPatch.
/// </summary>
[RegisterModelCapability]
public sealed class RelicLogisticsCapability : ModelCapability
{
    private int _deliveryRemaining;
    private int _transitRemaining;
    private bool _isTransit;
    private bool _disabledByLogistics;

    public int DeliveryRemaining => _deliveryRemaining;
    public int TransitRemaining => _transitRemaining;
    public bool IsTransit => _isTransit;
    public bool IsExpiredTransit => _isTransit && _transitRemaining <= 0;
    public bool IsOperational => _deliveryRemaining <= 0 && !IsExpiredTransit;
    public bool HasVisibleState => _deliveryRemaining > 0 || _isTransit;

    public void AddDelivery(int amount)
    {
        if (amount <= 0)
            return;

        _deliveryRemaining += amount;
        StateChanged();
    }

    public void ReduceDelivery(int amount)
    {
        if (amount <= 0 || _deliveryRemaining <= 0)
            return;

        _deliveryRemaining = Math.Max(0, _deliveryRemaining - amount);
        StateChanged();
    }

    public void ReactivateDelivery()
    {
        if (_deliveryRemaining <= 0)
            return;

        _deliveryRemaining = 0;
        StateChanged();
    }

    public void StartOrExtendTransit(int amount)
    {
        if (amount <= 0)
            return;

        if (_isTransit && _deliveryRemaining > 0)
        {
            int deliveryReduction = Math.Min(_deliveryRemaining, amount);
            _deliveryRemaining -= deliveryReduction;
            amount -= deliveryReduction;
        }

        _isTransit = true;
        if (amount > 0)
            _transitRemaining += amount;
        StateChanged();
    }

    public void EndCombat()
    {
        bool changed = false;
        if (_deliveryRemaining > 0)
        {
            _deliveryRemaining--;
            changed = true;
        }

        if (_isTransit && _transitRemaining > 0)
        {
            _transitRemaining--;
            changed = true;
        }

        if (changed)
            StateChanged();
    }

    protected override JsonNode SaveAdditionalState() => new JsonObject
    {
        ["deliveryRemaining"] = _deliveryRemaining,
        ["transitRemaining"] = _transitRemaining,
        ["isTransit"] = _isTransit,
        ["disabledByLogistics"] = _disabledByLogistics,
    };

    protected override void LoadAdditionalState(JsonNode? state, int schemaVersion)
    {
        if (state is not JsonObject obj)
            return;

        _deliveryRemaining = Math.Max(0, obj["deliveryRemaining"]?.GetValue<int>() ?? 0);
        _transitRemaining = Math.Max(0, obj["transitRemaining"]?.GetValue<int>() ?? 0);
        _isTransit = obj["isTransit"]?.GetValue<bool>() ?? false;
        _disabledByLogistics = obj["disabledByLogistics"]?.GetValue<bool>() ?? false;
    }

    protected override void OnAttach(AbstractModel owner)
    {
        if (owner is not RelicModel)
            throw new InvalidOperationException("Relic logistics can only be attached to relics.");
    }

    protected override void OnLoadedFromSave()
    {
        SyncVisualStatus();
        NotifyUi();
    }

    private void StateChanged()
    {
        SyncVisualStatus();
        MarkDirty();
        NotifyUi();
    }

    private void SyncVisualStatus()
    {
        if (Owner is not RelicModel relic)
            return;

        if (!IsOperational)
        {
            relic.Status = RelicStatus.Disabled;
            _disabledByLogistics = true;
            return;
        }

        if (_disabledByLogistics && relic.Status == RelicStatus.Disabled)
            relic.Status = RelicStatus.Normal;
        _disabledByLogistics = false;
    }

    private void NotifyUi()
    {
        if (Owner is RelicModel relic)
            RelicLogisticsUi.NotifyChanged(relic);
    }
}
