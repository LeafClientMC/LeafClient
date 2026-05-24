using System.Diagnostics;
using System;

namespace LeafClient.Services
{
    public class GameLogService
    {
        public GameLogService(Process process)
        {
            process.OutputDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) LeafLog.Info("Game Output", $"{e.Data}"); };
            process.ErrorDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) LeafLog.Error("Game Error", $"{e.Data}"); };
        }
    }
}
