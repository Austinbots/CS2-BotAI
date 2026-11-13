using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace BotAI;

// Resolve near-branch displacements from the unmodified instruction at load time.
// Dodge now uses direct branches instead of the old UpdateLookAround code caves.
internal static class LinuxDisplacementFixups
{
    public static bool Apply(string name, List<byte> patch, IReadOnlyDictionary<string, nint> sites, ILogger log)
    {
        switch (name)
        {
            case "Vision_AlwaysWatchApproachPoints":
            case "Vision_SkipIsMovingGate":
            case "OnBombPlanted_AllBotsLearnSite":
            {
                if (!sites.TryGetValue(name, out nint site)) return false;
                // jcc rel32 (6 bytes) -> jmp rel32 + nop (6 bytes).
                // The jump itself shrinks by one byte; preserve its original target.
                byte condition = Marshal.ReadByte(site + 1);
                if (Marshal.ReadByte(site) != 0x0F || condition < 0x80 || condition > 0x8F)
                {
                    log.LogError($"'{name}': expected a near conditional branch for displacement fixup.");
                    return false;
                }
                long displacement = (long)Marshal.ReadInt32(site + 2) + 1;
                return WriteRel32(patch, 1, displacement, name, log);
            }
            default:
                return true;
        }
    }

    private static bool WriteRel32(List<byte> patch, int index, long value, string name, ILogger log)
    {
        if (value < int.MinValue || value > int.MaxValue || index + 4 > patch.Count)
        {
            log.LogError($"'{name}': computed rel32 0x{value:X} out of range at patch[{index}].");
            return false;
        }
        var bytes = BitConverter.GetBytes((int)value);
        for (int i = 0; i < 4; i++) patch[index + i] = bytes[i];
        return true;
    }
}
