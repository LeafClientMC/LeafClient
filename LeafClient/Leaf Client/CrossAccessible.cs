/*
 * 
 *       🌿 LeafClientMC™️ 2024
 *       All the code on this project was written by ZiAD on GitHub.
 *       Some code snippets were taken from stackoverflow.com (don't come at me, all developers do that).
 * 
 */

using DiscordRPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Leaf_Client
{
    public class CrossAccessible
    {

        public static DiscordRpcClient client;
        public static bool doneLoading = false;
        public static bool reloadSkin = false;
        public static string ServerAutoJoinIP = null;
        public static string ServerAutoJoinPort = null;
        public static bool FullScreenMode = false;
        public static string Addon = "Fabric";
        public static string AddonVersion = null;
        public static int MinimumRam = 0;
        public static int MaximumRam = 0;
        public static string LaunchingGame = "no";
        public static string gameLogs = null;
        public static string launcherArgs = "none";
        public static bool FabricErrorFound = false;
        public static string chosenPreview = null;

        public static List<string> Mods = new List<string>();

        public static string checkPlayer()
        {

            if (Properties.Settings.Default.SessionInfo != null)
            {

                return $"Playing as {Properties.Settings.Default.SessionInfo.Username}";

            }
            else if (Properties.Settings.Default.OfflineSession != null)
            {

                return $"Playing as {Properties.Settings.Default.OfflineSession.Username}";

            }

            return "User is not logged into any account";

        }

        public static string GetPlayerNameOnRPC()
        {

            if (Properties.Settings.Default.SessionInfo != null)
            {

                return $"{Properties.Settings.Default.SessionInfo.Username}";

            }
            else if (Properties.Settings.Default.OfflineSession != null)
            {

                return $"{Properties.Settings.Default.OfflineSession.Username}";

            }

            return "Unknown User";

        }

    }
}
