#nullable enable

using ProtoBuf;

namespace flawlesssvanaxfork;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class ModDataPacket
{
    public required string FileName { get; init; }
    public required int ModIndex { get; init; }
    public required byte[] Data { get; init; }
}