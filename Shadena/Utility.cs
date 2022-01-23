using System.Buffers;
using System.Text;
using Blake2Fast;
using Microsoft.AspNetCore.WebUtilities;

namespace Shadena;

public static class Utility
{
    public static async Task YieldToBrowser() => await Task.Delay(1); // yeah, seriously
}