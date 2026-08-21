namespace AK_Exusiai.AK_ExusiaiCode.Extensions;

public static class ResourcePaths
{
    public static string CardPortrait(string fileName)
    {
        return $"{MainFile.ResPath}/images/cards/{fileName}";
    }

    public static string PowerIcon(string fileName)
    {
        return $"{MainFile.ResPath}/images/powers/{fileName}";
    }

    public static string RelicIcon(string fileName)
    {
        return $"{MainFile.ResPath}/images/relics/{fileName}";
    }
}
