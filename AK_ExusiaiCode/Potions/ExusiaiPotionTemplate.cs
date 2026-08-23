using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Potions;

public abstract class ExusiaiPotionTemplate : ModPotionTemplate
{
    protected virtual string AssetName => GetType().Name;

    public override PotionAssetProfile AssetProfile => new(
        ImagePath: $"{Entry.ResPath}/images/potions/{AssetName}.png",
        OutlinePath: $"{Entry.ResPath}/images/potions/{AssetName}Outline.png");
}
