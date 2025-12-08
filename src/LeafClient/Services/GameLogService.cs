using System.Diagnostics;
using System;

namespace LeafClient.Services
{
    public class GameLogService
    {
        public GameLogService(Process process)
        {
            // Placeholder: Attach to process output/error for logging
            process.OutputDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine($"[Game Output] {e.Data}"); };
            process.ErrorDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.Error.WriteLine($"[Game Error] {e.Data}"); };
        }
    }
}
