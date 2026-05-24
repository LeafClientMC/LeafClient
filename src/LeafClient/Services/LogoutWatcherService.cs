using System;

namespace LeafClient.Services
{
    public class LogoutWatcherService
    {
        public void StartMonitoring()
        {
            LeafLog.Info("Session", "Starting logout watcher...");
        }
    }
}
