using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace AK_Exusiai.Relics;

internal sealed class StudyTourSmithRestSiteOption(Player owner) : RestSiteOption(owner)
{
    private CardModel[] _selection = [];

    public override string OptionId => "SMITH";
    public override IEnumerable<string> AssetPaths => base.AssetPaths.Concat(NCardSmithVfx.AssetPaths);
    public override bool IsEnabled => Owner.Deck.UpgradableCardCount != 0;

    public override async Task<bool> OnSelect()
    {
        int count = Math.Min(2, Owner.Deck.UpgradableCardCount);
        CardSelectorPrefs prefs = new(CardSelectorPrefs.UpgradeSelectionPrompt, count)
        {
            Cancelable = true,
            RequireManualConfirmation = true,
        };
        _selection = (await CardSelectCmd.FromDeckForUpgrade(Owner, prefs)).ToArray();
        if (_selection.Length == 0)
            return false;

        foreach (CardModel card in _selection)
            CardCmd.Upgrade(card, CardPreviewStyle.None);
        await Hook.AfterRestSiteSmith(Owner.RunState, Owner);
        return true;
    }

    public override async Task DoLocalPostSelectVfx(CancellationToken ct = default)
    {
        NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(NCardSmithVfx.Create(_selection));
        await Cmd.CustomScaledWait(1f, 2f, ignoreCombatEnd: false, ct);
    }

    public override Task DoRemotePostSelectVfx()
    {
        NRestSiteCharacter? character = NRestSiteRoom.Instance?.Characters.FirstOrDefault(c => c.Player == Owner);
        NCardSmithVfx? vfx = NCardSmithVfx.Create();
        if (vfx is not null)
        {
            character?.AddChildSafely(vfx);
            vfx.Position = Vector2.Zero;
        }
        return Task.CompletedTask;
    }
}
