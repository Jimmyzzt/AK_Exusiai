using AK_Exusiai.AK_ExusiaiCode.Character;
using AK_Exusiai.AK_ExusiaiCode.Extensions;
using AK_Exusiai.AK_ExusiaiCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.AK_ExusiaiCode.Relics;

[RegisterRelic(typeof(AKExusiaiRelicPool))]
public sealed class ExusiaiBadge : ModRelicTemplate
{
    private const int StartingAmmo = 4;

    public override RelicRarity Rarity => RelicRarity.Starter;
    public override string CustomIconPath => ResourcePaths.RelicIcon("placeholder.png");

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<AmmoPower>(null!, Owner.Creature, StartingAmmo, Owner.Creature, null!, false);
    }
}
