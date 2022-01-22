using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace PactSharp.Types;

public class ChainwebMetadata
{
    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string ChainId { get; set; }
    public string Sender { get; set; }
    public int GasLimit { get; set; }
    public decimal GasPrice { get; set; }
    public double Ttl { get; set; }
    public long CreationTime { get; set; }
}