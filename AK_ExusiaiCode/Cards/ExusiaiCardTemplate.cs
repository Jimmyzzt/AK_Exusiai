using AK_Exusiai.Mechanics;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using AK_Exusiai.Powers;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace AK_Exusiai.Cards;

public abstract class ExusiaiCardTemplate(
    int baseCost,
    CardType type,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : ModCardTemplate(baseCost, type, rarity, target, showInCardLibrary)
{
    public override CardAssetProfile AssetProfile
    {
        get
        {
            string portraitPath = $"{Entry.ResPath}/images/cards/{GetType().Name}.png";
            return ResourceLoader.Exists(portraitPath)
                ? new CardAssetProfile(PortraitPath: portraitPath)
                : CardAssetProfile.Empty;
        }
    }

    protected virtual bool ShowAmmoHoverTip => false;
    protected virtual bool ShowDeliveryHoverTip => false;
    protected virtual bool ShowTransitHoverTip => false;
    protected virtual bool ShowAngelHoverTip => false;
    protected virtual bool ShowOverloadHoverTip => false;
    protected virtual bool ShowInterferenceHoverTip => false;
    protected virtual bool ShowFirepowerHoverTip => false;
    protected virtual bool ShowDeliveryTransitInteractionHoverTip => false;
    protected virtual bool PlaysOwnNonAttackAnimation => false;
    protected virtual IEnumerable<IHoverTip> CardHoverTips => [];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            if (ShowAmmoHoverTip)
                yield return ModSecondaryResourceRegistry.CreateHoverTip(AmmoResource.Id);
            if (ShowDeliveryHoverTip)
                yield return ExusiaiKeywords.DeliveryHoverTip;
            if (ShowTransitHoverTip)
                yield return ExusiaiKeywords.TransitHoverTip;
            if (ShowAngelHoverTip)
                yield return ExusiaiKeywords.AngelHoverTip;
            if (ShowAmmoHoverTip || ShowOverloadHoverTip)
                yield return ExusiaiKeywords.OverloadHoverTip;
            if (ShowInterferenceHoverTip)
            {
                yield return HoverTipFactory.FromPower<InterferencePower>();
                yield return HoverTipFactory.FromPower<SilencePower>(1);
            }
            if (ShowFirepowerHoverTip)
                yield return FirepowerPower.CreateGenericHoverTip();
            if (ShowDeliveryTransitInteractionHoverTip)
                yield return ExusiaiKeywords.DeliveryTransitInteractionHoverTip;

            foreach (IHoverTip hoverTip in CardHoverTips)
                yield return hoverTip;
        }
    }

    public override Task OnEnqueuePlayVfx(Creature? target)
    {
        if (Owner.Character is AK_Exusiai.Characters.Exusiai || PlaysOwnNonAttackAnimation || Type == CardType.Attack)
            return Task.CompletedTask;

        string animation = Type == CardType.Power ? "PowerUp" : "Cast";
        float delay = Type == CardType.Power
            ? Owner.Character.PowerUpAnimDelay
            : Owner.Character.CastAnimDelay;
        return CreatureCmd.TriggerAnim(Owner.Creature, animation, delay);
    }
}
