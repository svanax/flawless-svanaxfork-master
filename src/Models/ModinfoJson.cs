#nullable enable

namespace flawlesssvanaxfork;

/// <summary>
/// Used to parse modid, version and description from `modinfo.json`.
/// </summary>
internal class ModinfoJson
{
    public string? Modid { get; init; }
    public string Version { get; init; } = "";
    public string Description { get; init; } = "";
}