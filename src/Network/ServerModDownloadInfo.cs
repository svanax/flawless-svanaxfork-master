#nullable enable

using ProtoBuf;

namespace flawlesssvanaxfork;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ServerModDownloadInfo
{
    public required string Modid { get; init; }
    public required string Filename { get; init; }
    public required string Version { get; init; }
    public required string Description { get; init; }
    public required int Index { get; init; }
}