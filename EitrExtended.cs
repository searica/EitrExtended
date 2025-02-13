using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Utils;
using Jotunn.Managers;
using Configs;
using Logging;
using Jotunn.Extensions;

// To begin using: rename the solution and project, then find and replace all instances of "ModTemplate"
// Next: rename the main plugin as desired.

// If using Jotunn then the following files should be removed from Configs:
// - ConfigManagerWatcher
// - ConfigurationManagerAttributes
// - ConfigFileExtensions should be editted to only include `DisableSaveOnConfigSet`


namespace EitrExtended;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[BepInDependency(Jotunn.Main.ModGuid, Jotunn.Main.Version)]
[NetworkCompatibility(CompatibilityLevel.VersionCheckOnly, VersionStrictness.Patch)]
[SynchronizationMode(AdminOnlyStrictness.IfOnServer)]
internal sealed class EitrExtended : BaseUnityPlugin
{
    public const string PluginName = "EitrExtended";
    internal const string Author = "Searica";
    public const string PluginGUID = $"{Author}.Valheim.{PluginName}";
    public const string PluginVersion = "0.1.1";

    internal static EitrExtended Instance;
    internal static ConfigFile ConfigFile;
    internal static ConfigFileWatcher ConfigFileWatcher;


    // Global settings
    internal const string GlobalSection = "Global";
    internal const string EitrRegenSection = "Eitr Regen";
    internal const string EitrBaseSection = "Eitr Base";

    internal ConfigEntry<bool> EnableEitrRegen;
    internal ConfigEntry<float> ExtraEitrRegenFlat;
    internal ConfigEntry<float> ExtraEitrRegen;
    internal ConfigEntry<bool> ExtraEitrRegenFoodOnly;

    internal ConfigEntry<bool> EnableEitrBase;
    internal ConfigEntry<float> BloodMagicBasePower;
    internal ConfigEntry<float> BloodMagicBaseCoeff;
    internal ConfigEntry<float> ElementMagicBasePower;
    internal ConfigEntry<float> ElementMagicBaseCoeff;

    public void Awake()
    {
        Instance = this;
        ConfigFile = Config;
        Log.Init(Logger);

        Config.DisableSaveOnConfigSet();
        SetUpConfigEntries();
        Config.Save();
        Config.SaveOnConfigSet = true;

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);
        Game.isModded = true;

        // Re-initialization after reloading config and don't save since file was just reloaded
        ConfigFileWatcher = new(Config);
        ConfigFileWatcher.OnConfigFileReloaded += () =>
        {
            // do stuff
        };

        SynchronizationManager.OnConfigurationSynchronized += (obj, e) =>
        {
            // do stuff
        };

        SynchronizationManager.OnConfigurationWindowClosed += () =>
        {
            // do stuff
        };

    }

    internal void SetUpConfigEntries()
    {
        Log.Verbosity = Config.BindConfigInOrder(
            GlobalSection,
            "Verbosity",
            Log.InfoLevel.Low,
            "Information level of logging."
        );

        EnableEitrRegen = Config.BindConfigInOrder(
            EitrRegenSection,
            "Enable Extra Eitr Regen",
            true,
            "Enable increased eitr regen.",
            synced: true
        );
        ExtraEitrRegen = Config.BindConfigInOrder(
            EitrRegenSection,
            "Extra Eitr Regen",
            0.2f,
            "Increase eitr regen by X% of total eitr above base eitr value.",
            acceptableValues: new AcceptableValueRange<float>(0f, 100f),
            synced: true
        );
        ExtraEitrRegenFoodOnly = Config.BindConfigInOrder(
            EitrRegenSection, 
            "Total From Food Only",
            true,
            "Only count eitr from food when calcuating total eitr above base eitr value.",
            synced: true
        );

        EnableEitrBase = Config.BindConfigInOrder(
            EitrBaseSection,
            "Enable Extra Base Eitr",
            true,
            "Enable magic skills granting base eitr as `extra eitr = coeff*(magic skil)^power`.",
            synced: true
        );

        BloodMagicBasePower = Config.BindConfigInOrder(
            EitrBaseSection,
            "Blood Magic Power",
            0.5f,
            "The power to raise your magic skill to when calculating exta base eitr `(magic skill)^power`.",
            acceptableValues: new AcceptableValueRange<float>(0f, 2f),
            synced: true
        );
        BloodMagicBaseCoeff = Config.BindConfigInOrder(
            EitrBaseSection,
            "Blood Magic Coeff",
            2.5f,
            "The number to multiply your magic skill by after raising it to a power when calculating exta base eitr.",
            acceptableValues: new AcceptableValueRange<float>(0f, 10f),
            synced: true,
            configAttributes: new ConfigurationManagerAttributes() { ShowRangeAsPercent = false }
        );


        ElementMagicBasePower = Config.BindConfigInOrder(
            EitrBaseSection,
            "Elemental Magic Power",
            0.5f,
            "The power to raise your magic skill to when calculating exta base eitr `(magic skill)^power`.",
            acceptableValues: new AcceptableValueRange<float>(0f, 2f),
            synced: true
        );
        ElementMagicBaseCoeff = Config.BindConfigInOrder(
            EitrBaseSection,
            "Elemental Magic Coeff",
            2.5f,
            "The number to multiply your magic skill by after raising it to a power when calculating exta base eitr.",
            acceptableValues: new AcceptableValueRange<float>(0f, 10f),
            synced: true,
            configAttributes: new ConfigurationManagerAttributes() { ShowRangeAsPercent = false }
        );
    }

    public void OnDestroy()
    {
        Config.Save();
    }

}
