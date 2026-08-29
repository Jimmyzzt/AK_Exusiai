using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace AK_Exusiai.Characters;

internal static class ExusiaiAppearanceManager
{
    private static readonly string ConfigDirectory = $"user://{Entry.ModId}";
    private static readonly string ConfigPath = $"{ConfigDirectory}/appearance.cfg";
    private const string ConfigSection = "character";
    private const string CharacterKey = "family";

    private static readonly CharacterDefinition[] Characters =
    [
        new(
            "AK_EXUSIAI_APPEARANCE.character.exusiai",
            $"{Entry.ResPath}/images/character/appearance/Exusiai.png",
            [
                Skin(
                    "AK_EXUSIAI_APPEARANCE.outfit.default",
                    "exusiai/default"),
                Skin(
                    "AK_EXUSIAI_APPEARANCE.outfit.midnightDelivery",
                    "exusiai/midnight_delivery",
                    merchantUsesSpecial: true),
                Skin(
                    "AK_EXUSIAI_APPEARANCE.outfit.wildOperation",
                    "exusiai/wild_operation",
                    merchantUsesSpecial: true),
                Skin(
                    "AK_EXUSIAI_APPEARANCE.outfit.cityRider",
                    "exusiai/city_rider",
                    merchantUsesSpecial: true),
            ]),
        new(
            "AK_EXUSIAI_APPEARANCE.character.newCovenant",
            $"{Entry.ResPath}/images/character/appearance/Exusiai_the_New_Covenant.png",
            [
                Skin(
                    "AK_EXUSIAI_APPEARANCE.outfit.default",
                    "new_covenant/default"),
                Skin(
                    "AK_EXUSIAI_APPEARANCE.outfit.wingseekersSong",
                    "new_covenant/wingseekers_song",
                    merchantUsesSpecial: true),
            ]),
    ];

    private static readonly int[] SelectedSkins = new int[Characters.Length];
    private static bool _initialized;
    private static int _selectedCharacter;

    internal static int CharacterCount => Characters.Length;

    internal static int SelectedCharacterIndex
    {
        get
        {
            Initialize();
            return _selectedCharacter;
        }
    }

    internal static int SelectedSkinIndex
    {
        get
        {
            Initialize();
            return SelectedSkins[_selectedCharacter];
        }
    }

    internal static int SelectedSkinCount => SelectedCharacter.Skins.Length;
    internal static bool IsNewCovenant => SelectedCharacterIndex == 1;
    internal static string SelectedCharacterNameKey => SelectedCharacter.NameKey;
    internal static string SelectedSkinNameKey => SelectedSkin.NameKey;
    internal static string SelectedBackgroundPath => SelectedCharacter.BackgroundPath;
    internal static string SelectedMerchantAnimation =>
        SelectedSkin.MerchantUsesSpecial ? "Special" : "Relax";

    private static CharacterDefinition SelectedCharacter =>
        Characters[SelectedCharacterIndex];

    private static SkinDefinition SelectedSkin =>
        SelectedCharacter.Skins[SelectedSkinIndex];

    internal static void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        ConfigFile config = new();
        if (config.Load(ConfigPath) != Error.Ok)
            return;

        _selectedCharacter = Math.Clamp(
            config.GetValue(ConfigSection, CharacterKey, 0).AsInt32(),
            0,
            Characters.Length - 1);
        for (int index = 0; index < Characters.Length; index++)
        {
            SelectedSkins[index] = Math.Clamp(
                config.GetValue(ConfigSection, SkinKey(index), 0).AsInt32(),
                0,
                Characters[index].Skins.Length - 1);
        }
    }

    internal static void SelectCharacter(int index)
    {
        Initialize();
        _selectedCharacter = Wrap(index, Characters.Length);
        Save();
    }

    internal static void SelectSkin(int index)
    {
        Initialize();
        SelectedSkins[_selectedCharacter] = Wrap(index, SelectedSkinCount);
        Save();
    }

    internal static bool ApplyCombatSkin(Node root) =>
        ApplySkin(root, SelectedSkin.CombatSkeletonPath);

    internal static bool ApplyBuildSkin(Node root) =>
        ApplySkin(root, SelectedSkin.BuildSkeletonPath);

    internal static bool ApplyCombatSkinToSprite(Node spineNode) =>
        ApplySkinToSprite(spineNode, SelectedSkin.CombatSkeletonPath);

    internal static bool ApplyBuildSkinToSprite(Node spineNode) =>
        ApplySkinToSprite(spineNode, SelectedSkin.BuildSkeletonPath);

    internal static Node? FindSpineSprite(Node node)
    {
        if (node.GetClass().ToString() == MegaSprite.spineClassName)
            return node;

        foreach (Node child in node.GetChildren())
        {
            Node? result = FindSpineSprite(child);
            if (result is not null)
                return result;
        }

        return null;
    }

    private static SkinDefinition Skin(
        string nameKey,
        string relativeDirectory,
        bool merchantUsesSpecial = false)
    {
        string root = $"{Entry.ResPath}/images/character/spine/{relativeDirectory}";
        return new SkinDefinition(
            nameKey,
            $"{root}/combat_skeleton_data.tres",
            $"{root}/build_skeleton_data.tres",
            merchantUsesSpecial);
    }

    private static int Wrap(int value, int count) =>
        (value % count + count) % count;

    private static string SkinKey(int characterIndex) =>
        $"skin_{characterIndex}";

    private static void Save()
    {
#pragma warning disable RITSU013 // user:// is writable runtime data.
        string absoluteDirectory = ProjectSettings.GlobalizePath(ConfigDirectory);
#pragma warning restore RITSU013
        Error directoryError = DirAccess.MakeDirRecursiveAbsolute(absoluteDirectory);
        if (directoryError != Error.Ok && directoryError != Error.AlreadyExists)
        {
            Entry.Logger.Warn(
                $"Unable to create Exusiai appearance config directory: {directoryError}");
            return;
        }

        ConfigFile config = new();
        config.SetValue(ConfigSection, CharacterKey, _selectedCharacter);
        for (int index = 0; index < SelectedSkins.Length; index++)
            config.SetValue(ConfigSection, SkinKey(index), SelectedSkins[index]);

        Error saveError = config.Save(ConfigPath);
        if (saveError != Error.Ok)
            Entry.Logger.Warn($"Unable to save Exusiai appearance: {saveError}");
    }

    private static bool ApplySkin(Node root, string skeletonPath)
    {
        Node? spineNode = FindSpineSprite(root);
        return spineNode is not null && ApplySkinToSprite(spineNode, skeletonPath);
    }

    private static bool ApplySkinToSprite(Node spineNode, string skeletonPath)
    {
        Resource? skeletonData = ResourceLoader.Load<Resource>(
            skeletonPath,
            null,
            ResourceLoader.CacheMode.Reuse);
        if (skeletonData is null)
        {
            Entry.Logger.Error($"Unable to load Exusiai appearance: {skeletonPath}");
            return false;
        }

        MegaSprite sprite = new(spineNode);
        MegaSkeletonDataResource data = new(Variant.From(skeletonData));
        sprite.SetSkeletonDataRes(data);
        return true;
    }

    private sealed record CharacterDefinition(
        string NameKey,
        string BackgroundPath,
        SkinDefinition[] Skins);

    private sealed record SkinDefinition(
        string NameKey,
        string CombatSkeletonPath,
        string BuildSkeletonPath,
        bool MerchantUsesSpecial);
}
