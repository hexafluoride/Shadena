using System.Text.Json.Serialization;

namespace PactSharp.Types;

public class PactCapability
{
    public string Name { get; set; }

    [JsonPropertyName("args")] public List<object> Arguments { get; set; } = new();

    public override string ToString() => 
        string.IsNullOrWhiteSpace(Name) ? "" : 
        Arguments.Count == 0 ? $"({Name})" :
        $"({Name} {string.Join(' ', Arguments)})";

    static List<string> SplitSpaceSeparatedValues(string str)
    {
        var ret = new List<string>();

        var quoting = false;
        string current_run = "";
        char quote_char = ' ';

        for(int i = 0; i < str.Length; i++)
        {
            var current_char = str[i];

            if(quoting && current_char == quote_char)
            {
                current_run += current_char;
                quoting = false;

                if (!string.IsNullOrWhiteSpace(current_run))
                    ret.Add(current_run);

                current_run = "";
            }
            else if (current_char == '"')
            {
                quoting = true;
                quote_char = current_char;

                if (!string.IsNullOrWhiteSpace(current_run))
                    ret.Add(current_run.TrimEnd(' '));

                current_run = "";
                current_run += current_char;
            }
            else if (!quoting && current_char == ' ')
            {
                if (!string.IsNullOrWhiteSpace(current_run))
                    ret.Add(current_run);
                current_run = "";
            }
            else
            {
                current_run += current_char;
            }
        }

        if (!string.IsNullOrWhiteSpace(current_run))
            ret.Add(current_run);

        return ret;
    }
    
    public static PactCapability FromString(string str)
    {
        if (str.First() != '(' || str.Last() != ')')
            return null;

        var parts = SplitSpaceSeparatedValues(str.Substring(1, str.Length - 2));
        Console.WriteLine($"Split \"{str}\" to {parts.Count} parts: [{string.Join(", ", parts)}]");

        var cap = new PactCapability();
        cap.Name = parts[0];
        cap.Arguments = new List<object>();

        foreach (var arg in parts.Skip(1))
        {
            if (arg.Length > 1 && arg[0] == '"' && arg[arg.Length - 1] == '"')
                cap.Arguments.Add(arg.Substring(1, arg.Length - 2));
            else if (arg.Length > 1 && arg[0] == '\'')
                cap.Arguments.Add(arg.Substring(1));
            else if (bool.TryParse(arg, out bool boolArgument))
                cap.Arguments.Add(boolArgument);
            else if (int.TryParse(arg, out int intArgument))
                cap.Arguments.Add(intArgument);
            else if (decimal.TryParse(arg, out decimal decimalArgument))
                cap.Arguments.Add(decimalArgument);
            else
                return null;
        }

        return cap;
    }
}