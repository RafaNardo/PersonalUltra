using System.Security.Cryptography;
using System.Text;

namespace PersonalUltra.ExerciseCatalogFactory.Normalization;

public static class UuidV5
{
    public static Guid Create(Guid namespaceId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Span<byte> namespaceBytes = stackalloc byte[16];
        namespaceId.TryWriteBytes(namespaceBytes, bigEndian: true, out _);
        var nameBytes = Encoding.UTF8.GetBytes(name);
        var input = new byte[namespaceBytes.Length + nameBytes.Length];
        namespaceBytes.CopyTo(input);
        nameBytes.CopyTo(input.AsSpan(namespaceBytes.Length));
        Span<byte> hash = stackalloc byte[20];
        SHA1.HashData(input, hash);
        hash[6] = (byte)((hash[6] & 0x0f) | 0x50);
        hash[8] = (byte)((hash[8] & 0x3f) | 0x80);
        return new Guid(hash[..16], bigEndian: true);
    }
}
