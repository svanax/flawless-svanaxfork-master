#nullable enable

using ProtoBuf;
using System.IO;

namespace flawlesssvanaxfork;

public static class ByteSerializer
{
    public static byte[] ToBytes<T>(T obj)
    {
        using var memoryStream = new MemoryStream();
        Serializer.Serialize(memoryStream, obj);
        return memoryStream.ToArray();
    }
}