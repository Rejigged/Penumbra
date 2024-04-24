using Newtonsoft.Json.Linq;
using Penumbra.Meta.Manipulations;
using Penumbra.Mods.Editor;
using Penumbra.Mods.Groups;
using Penumbra.String.Classes;

namespace Penumbra.Mods.SubMods;

public class DefaultSubMod(IMod mod) : IModDataContainer
{
    public const string FullName = "Default Option";

    internal readonly IMod Mod = mod;

    public Dictionary<Utf8GamePath, FullPath> Files { get; set; } = [];
    public Dictionary<Utf8GamePath, FullPath> FileSwaps { get; set; } = [];
    public HashSet<MetaManipulation> Manipulations { get; set; } = [];

    IMod IModDataContainer.Mod
        => Mod;

    IModGroup? IModDataContainer.Group
        => null;

    public void AddDataTo(Dictionary<Utf8GamePath, FullPath> redirections, HashSet<MetaManipulation> manipulations)
        => ((IModDataContainer)this).AddDataTo(redirections, manipulations);

    public (int GroupIndex, int DataIndex) GetDataIndices()
        => (-1, 0);
}