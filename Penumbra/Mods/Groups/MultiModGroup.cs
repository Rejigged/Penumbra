using Dalamud.Interface.Internal.Notifications;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OtterGui;
using OtterGui.Classes;
using Penumbra.Api.Enums;
using Penumbra.Meta.Manipulations;
using Penumbra.Mods.Settings;
using Penumbra.Mods.SubMods;
using Penumbra.String.Classes;

namespace Penumbra.Mods.Groups;

/// <summary> Groups that allow all available options to be selected at once. </summary>
public sealed class MultiModGroup(Mod mod) : IModGroup, ITexToolsGroup
{
    public GroupType Type
        => GroupType.Multi;

    public GroupDrawBehaviour Behaviour
        => GroupDrawBehaviour.MultiSelection;

    public          Mod               Mod             { get; }      = mod;
    public          string            Name            { get; set; } = "Group";
    public          string            Description     { get; set; } = "A non-exclusive group of settings.";
    public          ModPriority       Priority        { get; set; }
    public          Setting           DefaultSettings { get; set; }
    public readonly List<MultiSubMod> OptionData = [];

    public IReadOnlyList<IModOption> Options
        => OptionData;

    public IReadOnlyList<IModDataContainer> DataContainers
        => OptionData;

    public bool IsOption
        => OptionData.Count > 0;

    public FullPath? FindBestMatch(Utf8GamePath gamePath)
        => OptionData.OrderByDescending(o => o.Priority)
            .SelectWhere(o => (o.Files.TryGetValue(gamePath, out var file) || o.FileSwaps.TryGetValue(gamePath, out file), file))
            .FirstOrDefault();

    public IModOption? AddOption(string name, string description = "")
    {
        var groupIdx = Mod.Groups.IndexOf(this);
        if (groupIdx < 0)
            return null;

        var subMod = new MultiSubMod(this)
        {
            Name        = name,
            Description = description,
        };
        OptionData.Add(subMod);
        return subMod;
    }

    public static MultiModGroup? Load(Mod mod, JObject json)
    {
        var ret = new MultiModGroup(mod)
        {
            Name            = json[nameof(Name)]?.ToObject<string>() ?? string.Empty,
            Description     = json[nameof(Description)]?.ToObject<string>() ?? string.Empty,
            Priority        = json[nameof(Priority)]?.ToObject<ModPriority>() ?? ModPriority.Default,
            DefaultSettings = json[nameof(DefaultSettings)]?.ToObject<Setting>() ?? Setting.Zero,
        };
        if (ret.Name.Length == 0)
            return null;

        var options = json["Options"];
        if (options != null)
            foreach (var child in options.Children())
            {
                if (ret.OptionData.Count == IModGroup.MaxMultiOptions)
                {
                    Penumbra.Messager.NotificationMessage(
                        $"Multi Group {ret.Name} in {mod.Name} has more than {IModGroup.MaxMultiOptions} options, ignoring excessive options.",
                        NotificationType.Warning);
                    break;
                }

                var subMod = new MultiSubMod(ret, child);
                ret.OptionData.Add(subMod);
            }

        ret.DefaultSettings = ret.FixSetting(ret.DefaultSettings);

        return ret;
    }

    public SingleModGroup ConvertToSingle()
    {
        var single = new SingleModGroup(Mod)
        {
            Name            = Name,
            Description     = Description,
            Priority        = Priority,
            DefaultSettings = DefaultSettings.TurnMulti(OptionData.Count),
        };
        single.OptionData.AddRange(OptionData.Select(o => o.ConvertToSingle(single)));
        return single;
    }

    public int GetIndex()
        => ModGroup.GetIndex(this);

    public void AddData(Setting setting, Dictionary<Utf8GamePath, FullPath> redirections, HashSet<MetaManipulation> manipulations)
    {
        foreach (var (option, index) in OptionData.WithIndex().OrderByDescending(o => o.Value.Priority))
        {
            if (setting.HasFlag(index))
                option.AddDataTo(redirections, manipulations);
        }
    }

    public void WriteJson(JsonTextWriter jWriter, JsonSerializer serializer, DirectoryInfo? basePath = null)
    {
        ModSaveGroup.WriteJsonBase(jWriter, this);
        jWriter.WritePropertyName("Options");
        jWriter.WriteStartArray();
        foreach (var option in OptionData)
        {
            jWriter.WriteStartObject();
            SubMod.WriteModOption(jWriter, option);
            jWriter.WritePropertyName(nameof(option.Priority));
            jWriter.WriteValue(option.Priority.Value);
            SubMod.WriteModContainer(jWriter, serializer, option, basePath ?? Mod.ModPath);
            jWriter.WriteEndObject();
        }

        jWriter.WriteEndArray();
    }

    public (int Redirections, int Swaps, int Manips) GetCounts()
        => ModGroup.GetCountsBase(this);

    public Setting FixSetting(Setting setting)
        => new(setting.Value & ((1ul << OptionData.Count) - 1));

    /// <summary> Create a group without a mod only for saving it in the creator. </summary>
    internal static MultiModGroup WithoutMod(string name)
        => new(null!)
        {
            Name = name,
        };

    IReadOnlyList<OptionSubMod> ITexToolsGroup.OptionData
        => OptionData;
}
