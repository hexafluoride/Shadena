using System.Text;
using System.Text.Json.Serialization;
using Chaos.NaCl;
using Microsoft.AspNetCore.WebUtilities;

namespace PactSharp.Types;

public class PactKeypair
{
    [JsonIgnore]
    public byte[] PublicKey { get; }
    public byte[] PrivateKey { get; }
    [JsonIgnore]
    public byte[] ExpandedPrivateKey { get; }

    public PactKeypair(byte[] publicKey, byte[] privateKey)
    {
        PublicKey = publicKey;
        PrivateKey = privateKey;
        ExpandedPrivateKey = Ed25519.ExpandedPrivateKeyFromSeed(privateKey);
    }

    [JsonConstructor]
    public PactKeypair(byte[] privateKey) : this(Ed25519.PublicKeyFromSeed(privateKey), privateKey)
    {
    }

    public PactKeypair(string privateKey) : this(privateKey.ToByteArray())
    {
    }

    public byte[] Sign(byte[] data) => Ed25519.Sign(data, ExpandedPrivateKey);
    public byte[] Sign(string str) => Sign(Encoding.UTF8.GetBytes(str));
    public byte[] Sign(PactCommand command) => Sign(Base64UrlTextEncoder.Decode(command.Hash));
    public string SignEncoded(byte[] data) => Base64UrlTextEncoder.Encode(Sign(data));
    public string SignEncoded(string str) => Base64UrlTextEncoder.Encode(Sign(str));
    public string SignEncoded(PactCommand command) => SignEncoded(command.Hash);
}