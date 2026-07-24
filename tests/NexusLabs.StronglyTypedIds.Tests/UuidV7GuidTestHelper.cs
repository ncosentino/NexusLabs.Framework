namespace NexusLabs.StronglyTypedIds.Tests;

internal static class UuidV7GuidTestHelper
{
    public static long ReadUnixTimeMilliseconds(Guid value)
    {
        var bytes = value.ToByteArray(bigEndian: true);
        return
            ((long)bytes[0] << 40) |
            ((long)bytes[1] << 32) |
            ((long)bytes[2] << 24) |
            ((long)bytes[3] << 16) |
            ((long)bytes[4] << 8) |
            bytes[5];
    }
}
