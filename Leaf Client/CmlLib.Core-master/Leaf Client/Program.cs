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

            if (Environment.OSVersion.Version.Major >= 6)
                SetProcessDPIAware();

            if (!new WebClient().DownloadString("https://raw.githubusercontent.com/LeafClientMC/LeafClient/main/launcherassets/latestversion.txt").Contains("1.0.2"))
            {
                MessageBox.Show("There's a new version of Leaf Client. Please update.", "Leaf Client", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {

                Application.Run(new MainForm());

            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
    }
}
