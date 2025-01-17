using Newtonsoft.Json;
using Penumbra.Mods.SubMods;
using Penumbra.Services;

namespace Penumbra.Mods.Groups;

public readonly struct ModSaveGroup : ISavable
{
    private readonly DirectoryInfo  _basePath;
    private readonly IModGroup?     _group;
    private readonly int            _groupIdx;
    private readonly DefaultSubMod? _defaultMod;
    private readonly bool           _onlyAscii;

    private ModSaveGroup(DirectoryInfo basePath, IModGroup group, int groupIndex, bool onlyAscii)
    {
        _basePath  = basePath;
        _group     = group;
        _groupIdx  = groupIndex;
        _onlyAscii = onlyAscii;
    }

    public static ModSaveGroup WithoutMod(DirectoryInfo basePath, IModGroup group, int groupIndex, bool onlyAscii)
        => new(basePath, group, groupIndex, onlyAscii);

    public ModSaveGroup(IModGroup group, bool onlyAscii)
        : this(group.Mod.ModPath, group, group.GetIndex(), onlyAscii)
    { }

    public ModSaveGroup(DirectoryInfo basePath, DefaultSubMod @default, bool onlyAscii)
    {
        _basePath   = basePath;
        _groupIdx   = -1;
        _defaultMod = @default;
        _onlyAscii  = onlyAscii;
    }

    public ModSaveGroup(DirectoryInfo basePath, IModDataContainer container, bool onlyAscii)
    {
        _basePath   = basePath;
        _defaultMod = container as DefaultSubMod;
        _onlyAscii  = onlyAscii;
        if (_defaultMod != null)
        {
            _groupIdx = -1;
            _group    = null;
        }
        else
        {
            _group    = container.Group!;
            _groupIdx = _group.GetIndex();
        }
    }

    public ModSaveGroup(IModDataContainer container, bool onlyAscii)
    {
        _basePath = (container.Mod as Mod)?.ModPath
         ?? throw new Exception("Invalid save group from default data container without base path."); // Should not happen.
        _defaultMod = container as DefaultSubMod;
        _onlyAscii  = onlyAscii;
        _group      = container.Group;
        _groupIdx   = _group?.GetIndex() ?? -1;
    }

    public string ToFilename(FilenameService fileNames)
        => fileNames.OptionGroupFile(_basePath.FullName, _groupIdx, _group?.Name ?? string.Empty, _onlyAscii);

    public void Save(StreamWriter writer)
    {
        using var j = new JsonTextWriter(writer);
        j.Formatting = Formatting.Indented;
        var serializer = new JsonSerializer { Formatting = Formatting.Indented };
        j.WriteStartObject();
        if (_groupIdx >= 0)
            _group!.WriteJson(j, serializer);
        else
            SubMod.WriteModContainer(j, serializer, _defaultMod!, _basePath);
        j.WriteEndObject();
    }

    public static void WriteJsonBase(JsonTextWriter jWriter, IModGroup group)
    {
        jWriter.WritePropertyName(nameof(group.Name));
        jWriter.WriteValue(group.Name);
        jWriter.WritePropertyName(nameof(group.Description));
        jWriter.WriteValue(group.Description);
        jWriter.WritePropertyName(nameof(group.Priority));
        jWriter.WriteValue(group.Priority.Value);
        jWriter.WritePropertyName(nameof(group.Type));
        jWriter.WriteValue(group.Type.ToString());
        jWriter.WritePropertyName(nameof(group.DefaultSettings));
        jWriter.WriteValue(group.DefaultSettings.Value);
    }
}
