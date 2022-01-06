using System.Buffers;
using System.Text;
using Blake2Fast;
using Microsoft.AspNetCore.WebUtilities;

namespace Shadena;

public static class Utility
{
    public static string HashEncoded(this string data) => HashEncoded(Encoding.UTF8.GetBytes(data));
    public static string HashEncoded(this ReadOnlySequence<char> data) => HashEncoded(Encoding.UTF8.GetBytes(in data));

    public static string HashEncoded(this ReadOnlySpan<byte> data) =>
        WebEncoders.Base64UrlEncode(Blake2b.ComputeHash(32, data));
}