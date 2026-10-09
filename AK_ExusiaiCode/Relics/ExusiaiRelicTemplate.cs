using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Relics;

public abstract class ExusiaiRelicTemplate : ModRelicTemplate
{
    protected virtual string AssetName => GetType().Name;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/relics/{AssetName}.png",
        IconOutlinePath: $"{Entry.ResPath}/images/relics/{AssetName}Outline.png",
        BigIconPath: $"{Entry.ResPath}/images/relics/{AssetName}.png");
}
