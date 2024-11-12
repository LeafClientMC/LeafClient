/*
 * 
 *       🌿 LeafClientMC™️ 2024
 *       All the code on this project was written by ZiAD on GitHub.
 *       Some code snippets were taken from stackoverflow.com (don't come at me, all developers do that).
 * 
 */

using DiscordRPC.Logging;
using DiscordRPC;
using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Leaf_Client
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (!new WebClient().DownloadString("https://raw.githubusercontent.com/LeafClientMC/LeafClient/main/launcherassets/latestversion.txt").Contains(Leaf_Client.Properties.Settings.Default.YourVersion))
            {
                new NotifyIcon().ShowBalloonTip(1000, "Leaf Client", "Leaf Client is currently updating, please wait.", ToolTipIcon.None);

                string downloadLink = new WebClient().DownloadString("https://raw.githubusercontent.com/LeafClientMC/LeafClient/main/launcherassets/lvdl.txt");

                new WebClient().DownloadFileAsync(new Uri(downloadLink), Path.Combine(Application.StartupPath + "\\Leaf Client_update.exe"));

                Process.Start(Path.Combine(Application.StartupPath + "\\Leaf Client_update.exe"));

                Application.Exit();
            }
            else
            {

                if (Application.ExecutablePath == Path.Combine(Application.StartupPath + "\\Leaf Client_update.exe"))
                {

                    new NotifyIcon().ShowBalloonTip(1000, "Leaf Client", $"Welcome to version {Properties.Settings.Default.YourVersion} of Leaf Client!.", ToolTipIcon.None);

                    File.Delete(Path.Combine(Application.StartupPath + "\\Leaf Client.exe"));

                    FileInfo file = new FileInfo(Path.Combine(Application.StartupPath + "\\Leaf Client_update.exe"));
                    file.Rename("Leaf Client.exe");

                }

                var thread = new Thread(ThreadStart);
                // allow UI with ApartmentState.STA though [STAThread] above should give that to you
                thread.TrySetApartmentState(ApartmentState.STA);
                thread.Start();

                Application.Run(new MainForm());

            }
        }

        private static void ThreadStart()
        {
            Application.Run(new ToastForm()); // <-- other form started on its own UI thread
        }
    }
}
