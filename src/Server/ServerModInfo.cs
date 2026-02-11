#nullable enable

namespace flawlesssvanaxfork;

public class ServerModInfo
{
    public required string Modid { get; init; }
    public required string ZipFilepath { get; init; }
    public required string Filename { get; init; }
    public required string Version { get; init; }
    public required string Description { get; init; }
    public required bool Public { get; init; }
}