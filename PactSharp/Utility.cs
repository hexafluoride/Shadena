using System.Buffers;
using System.Globalization;
using System.Text;
using Blake2Fast;
using Microsoft.AspNetCore.WebUtilities;

namespace PactSharp;

public static class Utility
{
    public static string HashEncoded(this string data) => HashEncoded(Encoding.UTF8.GetBytes(data));
    public static string HashEncoded(this ReadOnlySequence<char> data) => HashEncoded(Encoding.UTF8.GetBytes(in data));

    public static string HashEncoded(this ReadOnlySpan<byte> data) =>
        WebEncoders.Base64UrlEncode(Blake2b.ComputeHash(32, data));

    public static byte[] ToByteArray(this string str)
    {
        if (str.Length % 2 != 0)
            throw new ArgumentException();
            
        byte[] data = new byte[str.Length / 2];
        for (int index = 0; index < data.Length; index++)
            data[index] = byte.Parse(str.AsSpan(index * 2, 2), NumberStyles.HexNumber);

        return data; 
    }

    public static string ToHexString(this byte[] arr)
    {
        return BitConverter.ToString(arr).Replace("-", "").ToLower();
    }
}