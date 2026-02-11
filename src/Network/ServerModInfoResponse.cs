#nullable enable

using ProtoBuf;

namespace flawlesssvanaxfork;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ServerModInfoResponse
{
    public required ServerModDownloadInfo[] mods { get; init; }
}