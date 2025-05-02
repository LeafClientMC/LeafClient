/*
 * 
 *       🌿 LeafClientMC™️ 2024
 *       All the code on this project was written by ZiAD on GitHub.
 *       Some code snippets were taken from stackoverflow.com (don't come at me, all developers do that).
 * 
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows.Forms;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using MySql.Data;
using MySql.Data.MySqlClient;
using MojangAPI;
using MojangAPI.Model;
using System.Net.Http;
using Microsoft.IdentityModel.Abstractions;
using System.Runtime.InteropServices;
using System.Net;
using System.Diagnostics;
using Guna.UI2.WinForms;
using CmlLib.Core.Files;
using Leaf_Client.Properties;
using CmlLib.Utils;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using DiscordRPC;
using DiscordRPC.Logging;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using System.Web.Management;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolBar;
using System.Web.Configuration;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.Utils;
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ModLoaders.QuiltMC;
using CmlLib.Core.ModLoaders.LiteLoader;
using CmlLib.Core.Installer.Forge.Versions;
using CmlLib.Core.Installer.Forge;


namespace Leaf_Client
{
    public partial class MainForm : Form
    {

        // Definitions | Variables | DllImports
        MSession session;
        JELoginHandler login;
        GameOptionsFile optionFile;
        MinecraftPath gamePath;
        MinecraftLauncher launcher;
        private Process p;
        GameLog gameLog;

        private int uiThreadId = Thread.CurrentThread.ManagedThreadId;
        bool designMode = (LicenseManager.UsageMode == LicenseUsageMode.Designtime);
        bool discordSet = false;

        const uint WM_SETICON = 0x80;
        const int ICON_SMALL = 0;
        const int ICON_BIG = 1;
        const uint IMAGE_ICON = 1;
        const uint LR_LOADFROMFILE = 0x10;

        bool isAlreadyModded = false;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadImage(IntPtr hInstance, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern int SetWindowText(IntPtr hWnd, string text);

        // Custom Functions | Voids | Override Voids

        protected override CreateParams CreateParams
        {
            get
            {
                if (designMode == false)
                {

                    CreateParams handleParams = base.CreateParams;
                    handleParams.ExStyle |= 0x02000000;
                    return handleParams;

                }
                return null;
            }
        }

        static void DeleteFileWithSubstring(string directoryPath, string searchString)
        {
            try
            {
                // Get all files in the specified directory
                string[] files = Directory.GetFiles(directoryPath);

                // Check if any file name contains the specified substring
                foreach (string filePath in files)
                {
                    string fileName = Path.GetFileName(filePath);

                    if (fileName.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Found a file with the specified substring
                        Console.WriteLine($"Deleting file: {fileName}");

                        // Delete the file
                        File.Delete(filePath);
                        Console.WriteLine($"File deleted successfully.");
                        return; // If you want to stop searching after deleting the first occurrence, remove this line.
                    }
                }

                // No file with the specified substring found
                Console.WriteLine($"No file containing '{searchString}' was found in the directory.");
            }
            catch (Exception ex)
            {
                // Handle any exceptions (e.g., directory not found, access denied)
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static bool FindFileWithSubstring(string directoryPath, string searchString)
        {
            try
            {
                // Get all files in the specified directory
                string[] files = Directory.GetFiles(directoryPath);

                // Check if any file name contains the specified substring
                foreach (string filePath in files)
                {
                    string fileName = Path.GetFileName(filePath);

                    if (fileName.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Found a file with the specified substring
                        Console.WriteLine($"Found file: {fileName}");
                        return true; // If you want to stop searching after deleting the first occurrence, remove this line.
                    }
                }

                Console.WriteLine($"No file containing '{searchString}' was found in the directory.");
                return false;

            }
            catch (Exception ex)
            {
                // Handle any exceptions (e.g., directory not found, access denied)
                Console.WriteLine($"Error: {ex.Message}");

                return false;
            }
        }

        private async void LoadAvatars()
        {

            if (Properties.Settings.Default.SessionInfo != null)
            {

                List<string> smallPreview = new List<string>
        {
            "Marching (Bust)",
            "Walking (Bust)",
            "Cheering (Bust)",
            "Relaxing (Full)",
            "Dungeons (Bust)",
            "Facepalm (Bust)",
            "Sleeping (Bust)"
        };

                List<string> largePreview = new List<string>
        {
            "Marching",
            "Crouching",
            "Criss_Cross",
            "Cheering",
            "Relaxing",
            "Cowering",
            "Lunging",
            "Dungeons",
            "Facepalm",
            "Sleeping",
            "Archer",
            "Kicking"
        };

                string smallPreviewSelection = PickRandomString(smallPreview);
                string largePreviewSelection = PickRandomString(largePreview);

                await Task.Run(() =>
                {


                    if (smallPreviewSelection == "Marching (Bust)")
                        pbMUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/marching/{Properties.Settings.Default.SessionInfo.Username}/bust");
                    else if (smallPreviewSelection == "Walking (Bust)")
                        pbMUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/walking/{Properties.Settings.Default.SessionInfo.Username}/bust");
                    else if (smallPreviewSelection == "Cheering (Bust)")
                        pbMUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/cheering/{Properties.Settings.Default.SessionInfo.Username}/bust");
                    else if (smallPreviewSelection == "Relaxing (Full)")
                        pbMUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/relaxing/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (smallPreviewSelection == "Dungeons (Bust)")
                        pbMUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/dungeons/{Properties.Settings.Default.SessionInfo.Username}/bust");
                    else if (smallPreviewSelection == "Facepalm (Bust)")
                        pbMUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/facepalm/{Properties.Settings.Default.SessionInfo.Username}/bust");
                    else if (smallPreviewSelection == "Sleeping (Bust)")
                        pbMUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/sleeping/{Properties.Settings.Default.SessionInfo.Username}/bust");


                    if (largePreviewSelection == "Marching")
                        pbMACUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/marching/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Crouching")
                        pbMACUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/crouching/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Criss_Cross")
                        pbMACUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/criss_cross/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Cheering")
                        pbMACUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/cheering/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Relaxing")
                        pbMACUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/relaxing/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Cowering")
                        pbMACUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/cowering/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Lunging")
                        pbMACUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/lunging/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Dungeons")
                        pbMACUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/dungeons/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Facepalm")
                        pbMACUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/facepalm/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Sleeping")
                        pbMACUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/sleeping/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Archer")
                        pbMACUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/archer/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Kicking")
                        pbMACUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/kicking/{Properties.Settings.Default.SessionInfo.Username}/full");

                    Console.WriteLine(smallPreviewSelection);
                    Console.WriteLine(largePreviewSelection);

                });

            }
            else if (Properties.Settings.Default.OfflineSession != null)
            {

                List<string> smallPreview = new List<string>
        {
            "Marching (Bust)",
            "Walking (Bust)",
            "Cheering (Bust)",
            "Relaxing (Full)",
            "Dungeons (Bust)",
            "Facepalm (Bust)",
            "Sleeping (Bust)"
        };

                List<string> largePreview = new List<string>
        {
            "Marching",
            "Crouching",
            "Criss_Cross",
            "Cheering",
            "Relaxing",
            "Cowering",
            "Lunging",
            "Dungeons",
            "Facepalm",
            "Sleeping",
            "Archer",
            "Kicking"
        };

                string smallPreviewSelection = PickRandomString(smallPreview);
                string largePreviewSelection = PickRandomString(largePreview);

                await Task.Run(() =>
                {


                    if (smallPreviewSelection == "Marching (Bust)")
                        pbMUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/marching/{Properties.Settings.Default.OfflineSession.Username}/bust");
                    else if (smallPreviewSelection == "Walking (Bust)")
                        pbMUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/walking/{Properties.Settings.Default.OfflineSession.Username}/bust");
                    else if (smallPreviewSelection == "Cheering (Bust)")
                        pbMUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/cheering/{Properties.Settings.Default.OfflineSession.Username}/bust");
                    else if (smallPreviewSelection == "Relaxing (Full)")
                        pbMUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/relaxing/{Properties.Settings.Default.OfflineSession.Username}/full");
                    else if (smallPreviewSelection == "Dungeons (Bust)")
                        pbMUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/dungeons/{Properties.Settings.Default.OfflineSession.Username}/bust");
                    else if (smallPreviewSelection == "Facepalm (Bust)")
                        pbMUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/facepalm/{Properties.Settings.Default.OfflineSession.Username}/bust");
                    else if (smallPreviewSelection == "Sleeping (Bust)")
                        pbMUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/sleeping/{Properties.Settings.Default.OfflineSession.Username}/bust");


                    if (largePreviewSelection == "Marching")
                        pbMACUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/marching/{Properties.Settings.Default.OfflineSession.Username}/full");
                    else if (largePreviewSelection == "Crouching")
                        pbMACUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/crouching/{Properties.Settings.Default.OfflineSession.Username}/full");
                    else if (largePreviewSelection == "Criss_Cross")
                        pbMACUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/criss_cross/{Properties.Settings.Default.OfflineSession.Username}/full");
                    else if (largePreviewSelection == "Cheering")
                        pbMACUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/cheering/{Properties.Settings.Default.OfflineSession.Username}/full");
                    else if (largePreviewSelection == "Relaxing")
                        pbMACUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/relaxing/{Properties.Settings.Default.OfflineSession.Username}/full");
                    else if (largePreviewSelection == "Cowering")
                        pbMACUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/cowering/{Properties.Settings.Default.OfflineSession.Username}/full");
                    else if (largePreviewSelection == "Lunging")
                        pbMACUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/lunging/{Properties.Settings.Default.OfflineSession.Username}/full");
                    else if (largePreviewSelection == "Dungeons")
                        pbMACUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/dungeons/{Properties.Settings.Default.OfflineSession.Username}/full");
                    else if (largePreviewSelection == "Facepalm")
                        pbMACUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/facepalm/{Properties.Settings.Default.OfflineSession.Username}/full");
                    else if (largePreviewSelection == "Sleeping")
                        pbMACUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/sleeping/{Properties.Settings.Default.OfflineSession.Username}/full");
                    else if (largePreviewSelection == "Archer")
                        pbMACUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/archer/{Properties.Settings.Default.OfflineSession.Username}/full");
                    else if (largePreviewSelection == "Kicking")
                        pbMACUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/kicking/{Properties.Settings.Default.OfflineSession.Username}/full");

                    Console.WriteLine(smallPreviewSelection);
                    Console.WriteLine(largePreviewSelection);

                });

            }
        }

        private bool IsInternetAvailable()
        {

            try
            {

                if (new Ping().Send("www.google.com.mx").Status == IPStatus.Success)
                    return true;

                return false;


            }
            catch
            {

                return false;

            }

        }

        public MainForm()
        {
            InitializeComponent();

            if (IsInternetAvailable() == true)
            {

                pnMPlay.BringToFront();

                LoadAvatars();

                SetStyle(ControlStyles.OptimizedDoubleBuffer |
             ControlStyles.UserPaint |
             ControlStyles.AllPaintingInWmPaint, true);
                UpdateStyles();

                //Setting background programatically to avoid unnecessary lag when designing on the [Design] form.
                this.BackgroundImage = Properties.Resources._4178c126edac36deed9c8ecba2447443;

                Mandatories();

                login = JELoginHandlerBuilder.BuildDefault();

            }
            else
            {

                pnNoInternet.BringToFront();
                CrossAccessible.doneLoading = true;

            }

            CrossAccessible.client = new DiscordRpcClient("YOUR_CLIENT_ID"); // Replace "YOUR_CLIENT_ID" with the actual Discord client ID
            CrossAccessible.client.Initialize();

        }

        private async void Mandatories()
        {

            pnMain.FillColor = Color.FromArgb(30, 30, 30);
            pnMain.FillColor2 = Color.Transparent;
            pnMain.FillColor3 = Color.FromArgb(50, 50, 50);
            pnMain.FillColor4 = Color.Transparent;

            // Change Version to previously saved version
            lbMVersion.Text = Properties.Settings.Default.SavedVersion;
            lbMVersion.Left = (pnMPlay.ClientSize.Width - lbMVersion.Size.Width) / 2;

            // Download Minecraft Instance .ico
            if (!File.Exists(Environment.SpecialFolder.ApplicationData + "\\.minecraft\\LCP.ico"))
            {

                using (WebClient client = new WebClient())
                {
                    client.DownloadFile("https://raw.githubusercontent.com/LeafClientMC/LeafClient/main/Leaf%20Client%20Process.ico", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\LCP.ico");

                }

            }

            if (IsInternetAvailable() == true)
            {

                pnLoginForm.Visible = false;
                pnMain.Visible = false;
                pnNoInternet.Visible = false;
                pnFTLoginProcess.Visible = false; 
                pnFTOfflineLogin.Visible = false;

                if (Properties.Settings.Default.SessionInfo != null)
                {


                    Console.WriteLine("Username has been found.");
                    Console.WriteLine("Session Information has been found.");
                    var loginHandler = JELoginHandlerBuilder.BuildDefault();
                    session = await loginHandler.Authenticate();
                    var result = session.CheckIsValid;

                    lbMUsername.Text = Properties.Settings.Default.SessionInfo.Username;

                    var timer = new System.Windows.Forms.Timer
                    {
                        Interval = 1
                    };

                    timer.Tick += (sender, e) =>
                    {
                        timer.Stop();

                        if (result() == true)
                        {

                            Console.WriteLine("Logged in Successfully.");
                            Console.WriteLine($"Username: {session.Username}");
                            Console.WriteLine($"UUID: {session.UUID}");

                            pnMain.BringToFront();


                        }
                        else
                        {

                            Console.WriteLine("Failed to login.");
                            pnLoginForm.BringToFront();

                        }


                    };

                    timer.Start();

                }
                else if (Properties.Settings.Default.SessionInfo == null && Properties.Settings.Default.OfflineSession == null)
                {

                    pnLoginForm.BringToFront();

                }
                else if (Properties.Settings.Default.SessionInfo == null && Properties.Settings.Default.OfflineSession != null)
                {

                    this.session = MSession.CreateOfflineSession(Properties.Settings.Default.OfflineSession.Username);
                    Properties.Settings.Default.OfflineSession = session;
                    Properties.Settings.Default.Save();

                    if (session.CheckIsValid() == true)
                    {

                        lbMUsername.Text = Properties.Settings.Default.OfflineSession.Username;
                        pnMain.BringToFront();
                        Console.WriteLine("Logged in successfully with Offline mode from previous save.");
                        Console.WriteLine($"Username: {session.Username}");
                        Console.WriteLine($"UUID: {session.UUID}");

                    }
                    else
                    {

                        Console.WriteLine("Failed to login.");
                        pnLoginForm.BringToFront();

                    }

                }
                else
                {

                    Console.WriteLine("Failed to login.");
                    pnLoginForm.BringToFront();


                }

                var defaultPath = new MinecraftPath(MinecraftPath.GetOSDefaultPath());
                await initializeLauncher(defaultPath);

                LoadGameSettings();

                CrossAccessible.doneLoading = true;


            }

            pnLoginForm.Visible = true;
            pnMain.Visible = true;
            pnNoInternet.Visible = true;
            pnFTLoginProcess.Visible = true;
            pnFTOfflineLogin.Visible = true;

            checkForInternet.Start();

        }

        private void ShowAccountPanel()
        {

            pnMAccountInfo.Visible = true;
            pnMAccountInfo.BringToFront();


            if (Properties.Settings.Default.SessionInfo != null)
            {

                // Load current username
                lbMACUsername.Text = Properties.Settings.Default.SessionInfo.Username;
                lbMACUsername.Left = (pnMAccountInfo.ClientSize.Width - lbMACUsername.Size.Width) / 2;

                // Load UUID
                lbMACUUID.Text = Properties.Settings.Default.SessionInfo.UUID;
                lbMACUUID.Left = (pnMAccountInfo.ClientSize.Width - lbMACUUID.Size.Width) / 2;

            }
            else if (Properties.Settings.Default.OfflineSession != null)
            {

                // Load current username
                lbMACUsername.Text = Properties.Settings.Default.OfflineSession.Username;
                lbMACUsername.Left = (pnMAccountInfo.ClientSize.Width - lbMACUsername.Size.Width) / 2;

                // Load UUID
                lbMACUUID.Text = Properties.Settings.Default.OfflineSession.UUID;
                lbMACUUID.Left = (pnMAccountInfo.ClientSize.Width - lbMACUUID.Size.Width) / 2;

            }

        }

        private void HideAccountPanel()
        {

            pnMAccountInfo.Visible = false;

        }

        static string PickRandomString(List<string> list)
        {
            if (list == null || list.Count == 0)
            {
                throw new ArgumentException("The list is null or empty.");
            }

            Random random = new Random();
            int randomIndex = random.Next(0, list.Count);

            return list[randomIndex];
        }

        private async Task initializeLauncher(MinecraftPath path)
        {
            //txtPath.Text = path.BasePath;
            this.gamePath = path;

            launcher = new MinecraftLauncher(path);
            //await refreshVersions(null);
        }

        private async void LaunchGame()
        {

            Random rand = new Random();

            if (Properties.Settings.Default.QuickLaunch == true)
            {

                btnMPLaunch.Text = $"LAUNCHING (EST. {rand.Next(2, 5)}min)";
                btnMPLaunch.FillColor = Color.Purple;
                btnMPLaunch.PressedColor = Color.Purple;
                btnMPLaunch.HoverState.FillColor = Color.Purple;

            }
            else
            {

                btnMPLaunch.Text = $"LAUNCHING (EST. {rand.Next(2, 5)}min)";
                btnMPLaunch.FillColor = Color.Purple;
                btnMPLaunch.PressedColor = Color.Purple;
                btnMPLaunch.HoverState.FillColor = Color.Purple;

            }

            CrossAccessible.LaunchingGame = "yes";

            CrossAccessible.client.SetPresence(new RichPresence()
            {
                Details = CrossAccessible.checkPlayer(),
                State = "Launching Minecraft: Java Edition",
                Assets = new Assets()
                {
                    LargeImageKey = "https://raw.githubusercontent.com/LeafClientMC/LeafClient/main/Leaf%20Client.png",
                    LargeImageText = "Leaf Client",
                    SmallImageKey = $"{CrossAccessible.chosenPreview}",
                    SmallImageText = ""
                },

                Timestamps = Timestamps.Now,

                Buttons = new DiscordRPC.Button[]
            {

                                                                                                new DiscordRPC.Button { Label = "Play on Leaf Client", Url = "https://www.github.com/LeafClientMC/LeafClient" }


            },
            });

            if (Properties.Settings.Default.MaximumRam == 0 && Properties.Settings.Default.MinimumRam == 0)
            {

                int computerMemory = (int)Util.GetMemoryMb();
                int max = computerMemory / 2;
                if (max < 1024)
                    max = 1024;
                else if (max > 8192)
                    max = 8192;
                int min = max / 10;
                CrossAccessible.MaximumRam = max;
                CrossAccessible.MinimumRam = min;

            }

            if (Properties.Settings.Default.SessionInfo != null)
                session = Properties.Settings.Default.SessionInfo;
            else if (Properties.Settings.Default.OfflineSession != null)
                session = Properties.Settings.Default.OfflineSession;

            var result = session.CheckIsValid;

            if (!string.IsNullOrEmpty(CrossAccessible.ServerAutoJoinIP) && !string.IsNullOrEmpty(tbCustomServerIP.Text))
            {

                CrossAccessible.ServerAutoJoinIP = tbCustomServerIP.Text;
                CrossAccessible.ServerAutoJoinPort = tbCustomServerPort.Text;

            }

            if (Properties.Settings.Default.AccountType == "microsoft")
            {

                var loginHandler = JELoginHandlerBuilder.BuildDefault();
                session = await loginHandler.Authenticate();
                Properties.Settings.Default.SessionInfo = session;
                Properties.Settings.Default.Save();

            }
            else if (Properties.Settings.Default.AccountType == "offline")
            {

                this.session = MSession.CreateOfflineSession(Properties.Settings.Default.OfflineSession.Username);
                Properties.Settings.Default.OfflineSession = session;
                Properties.Settings.Default.Save();

            }
            else if (Properties.Settings.Default.AccountType == "none")
            {

                return;

            }

            var launchOption = new MLaunchOption()
            {
                MaximumRamMb = CrossAccessible.MaximumRam,
                Session = this.session,

                VersionType = "",
                GameLauncherName = "",
                GameLauncherVersion = "",

                FullScreen = CrossAccessible.FullScreenMode,

                ServerIp = CrossAccessible.ServerAutoJoinIP,

                DockName = "",
                DockIcon = ""
            };


            if (!string.IsNullOrEmpty(CrossAccessible.MinimumRam.ToString()))
                launchOption.MinimumRamMb = CrossAccessible.MinimumRam;

            if (!string.IsNullOrEmpty(CrossAccessible.ServerAutoJoinPort))
                launchOption.ServerPort = int.Parse(CrossAccessible.ServerAutoJoinPort);

            p = await launcher.CreateProcessAsync(lbMVersion.Text, launchOption);

            StartProcess();

        }

        private void StartProcess()
        {
            File.WriteAllText("launcher.txt", p.StartInfo.Arguments);

            // process options to display game log

            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;
            p.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
            p.EnableRaisingEvents = true;

            p.Start();

            p.BeginErrorReadLine();
            p.BeginOutputReadLine();

            ChangeMinecraftName.Start();

            gameLog = new GameLog(p);

        }

        private void LoadGameSettings()
        {

            if (Properties.Settings.Default.QuickLaunch == true)
            {

                btnMSTQuickLaunch.Text = $"ENABLED";

            }
            else if (Properties.Settings.Default.QuickLaunch == false)
            {

                btnMSTQuickLaunch.Text = $"DISABLED";

            }

            try
            {

                string path = System.IO.Path.Combine(gamePath.BasePath, "options.txt");

                optionFile = GameOptionsFile.ReadFile(path);

                foreach (var item in optionFile)
                {

                    if (item.Key == "autoJump")
                    {

                        try
                        {
                            string OnOrOff = "OFF";

                            if (item.Value == "false")
                                OnOrOff = "OFF";
                            else
                                OnOrOff = "ON";

                            btnMSTAutoJump.Text = $"AUTOJUMP: {OnOrOff}";
                        }
                        catch
                        {



                        }

                    }
                    else if (item.Key == "touchscreen")
                    {

                        try
                        {
                            string OnOrOff = "OFF";

                            if (item.Value == "false")
                                OnOrOff = "OFF";
                            else
                                OnOrOff = "ON";

                            btnMSTTouchScreen.Text = $"TOUCHSCREEN: {OnOrOff}";
                        }
                        catch
                        {



                        }

                    }
                    else if (item.Key == "showSubtitles")
                    {

                        try
                        {
                            string OnOrOff = "OFF";

                            if (item.Value == "false")
                                OnOrOff = "OFF";
                            else
                                OnOrOff = "ON";

                            btnMSTSubtitles.Text = $"SUBTITLES: {OnOrOff}";
                        }
                        catch
                        {



                        }

                    }
                    else if (item.Key == "toggleSprint")
                    {

                        try
                        {
                            string OnOrOff = "OFF";

                            if (item.Value == "false")
                                OnOrOff = "OFF";
                            else
                                OnOrOff = "ON";

                            btnMSTToggleSprint.Text = $"TOGGLESPRINT: {OnOrOff}";
                        }
                        catch
                        {



                        }

                    }
                    else if (item.Key == "toggleCrouch")
                    {

                        try
                        {
                            string OnOrOff = "OFF";

                            if (item.Value == "false")
                                OnOrOff = "OFF";
                            else
                                OnOrOff = "ON";

                            btnMSTToggleCrouch.Text = $"TOGGLECROUCH: {OnOrOff}";
                        }
                        catch
                        {



                        }

                    }
                    else if (item.Key == "fullscreen")
                    {

                        try
                        {
                            string OnOrOff = "OFF";

                            if (item.Value == "false")
                                OnOrOff = "OFF";
                            else
                            {
                                CrossAccessible.FullScreenMode = true;
                                OnOrOff = "ON";
                            }

                            btnMSTFullScreen.Text = $"FULLSCREEN: {OnOrOff}";
                        }
                        catch
                        {



                        }

                    }
                    else if (item.Key == "enableVsync")
                    {

                        try
                        {
                            string OnOrOff = "OFF";

                            if (item.Value == "false")
                                OnOrOff = "OFF";
                            else
                                OnOrOff = "ON";

                            btnMSTVSync.Text = $"VSYNC: {OnOrOff}";
                        }
                        catch
                        {



                        }

                    }
                    else if (item.Key == "entityShadows")
                    {

                        try
                        {
                            string OnOrOff = "OFF";

                            if (item.Value == "false")
                                OnOrOff = "OFF";
                            else
                                OnOrOff = "ON";

                            btnMSTEntityShadows.Text = $"ENTITY SHADOWS: {OnOrOff}";
                        }
                        catch
                        {



                        }

                    }
                    else if (item.Key == "highContrast")
                    {

                        try
                        {
                            string OnOrOff = "OFF";

                            if (item.Value == "false")
                                OnOrOff = "OFF";
                            else
                                OnOrOff = "ON";

                            btnMSTHighContrast.Text = $"HIGH CONTRAST: {OnOrOff}";
                        }
                        catch
                        {



                        }

                    }
                    else if (item.Key == "renderClouds")
                    {

                        try
                        {
                            if (item.Value == @"""fast""")
                                btnMSTRenderClouds.Text = $"RENDER CLOUDS: FAST";
                            else if (item.Value == @"""fancy""")
                                btnMSTRenderClouds.Text = $"RENDER CLOUDS: FANCY";
                            else if (item.Value == @"""off""")
                                btnMSTRenderClouds.Text = $"RENDER CLOUDS: OFF";
                        }
                        catch
                        {



                        }


                    }
                    else if (item.Key == "modelPart_cape")
                    {

                        try
                        {
                            string OnOrOff = "OFF";

                            if (item.Value == "false")
                                OnOrOff = "OFF";
                            else
                                OnOrOff = "ON";

                            btnMSTCape.Text = $"CAPE: {OnOrOff}";
                        }
                        catch
                        {



                        }

                    }
                    else if (item.Key == "modelPart_jacket")
                    {

                        try
                        {
                            string OnOrOff = "OFF";

                            if (item.Value == "false")
                                OnOrOff = "OFF";
                            else
                                OnOrOff = "ON";

                            btnMSTKJacket.Text = $"JACKET: {OnOrOff}";

                        }
                        catch
                        {



                        }

                    }
                    else if (item.Key == "modelPart_left_sleeve")
                    {
                        try
                        {
                            string OnOrOff = "OFF";

                            if (item.Value == "false")
                                OnOrOff = "OFF";
                            else
                                OnOrOff = "ON";

                            btnMSTLeftSleeve.Text = $"LEFT SLEEVE: {OnOrOff}";
                        }
                        catch
                        {



                        }

                    }
                    else if (item.Key == "modelPart_right_sleeve")
                    {
                        try
                        {
                            string OnOrOff = "OFF";

                            if (item.Value == "false")
                                OnOrOff = "OFF";
                            else
                                OnOrOff = "ON";

                            btnMSTRightSleeve.Text = $"RIGHT SLEEVE: {OnOrOff}";
                        }
                        catch
                        {



                        }


                    }
                    else if (item.Key == "modelPart_left_pants_leg")
                    {

                        try
                        {
                            string OnOrOff = "OFF";

                            if (item.Value == "false")
                                OnOrOff = "OFF";
                            else
                                OnOrOff = "ON";

                            btnMSTLeftPant.Text = $"LEFT PANT: {OnOrOff}";
                        }
                        catch
                        {



                        }

                    }
                    else if (item.Key == "modelPart_right_pants_leg")
                    {

                        try
                        {
                            string OnOrOff = "OFF";

                            if (item.Value == "false")
                                OnOrOff = "OFF";
                            else
                                OnOrOff = "ON";

                            btnMSTRightPant.Text = $"RIGHT PANT: {OnOrOff}";
                        }
                        catch
                        {



                        }

                    }
                    else if (item.Key == "modelPart_hat")
                    {

                        try
                        {
                            string OnOrOff = "OFF";

                            if (item.Value == "false")
                                OnOrOff = "OFF";
                            else
                                OnOrOff = "ON";

                            btnMSTHat.Text = $"HAT: {OnOrOff}";
                        }
                        catch
                        {



                        }

                    }

                    // Input-sensitive loading

                    else if (item.Key == "mainHand")
                    {

                        try
                        {

                            if (item.Value == @"""right""")
                                btnMSTMainHand.Text = $"MAIN HAND: RIGHT";
                            else
                                btnMSTMainHand.Text = $"MAIN HAND: LEFT";
                        }
                        catch
                        {



                        }

                    }
                    else if (item.Key == "mouseSensitivity")
                    {
                        try
                        {

                            if (float.TryParse(item.Value, out float mouseSensitivityValue))
                            {
                                lbMouseSensitivity.Text = $"Mouse Sensitivity ({mouseSensitivityValue})";
                                trbMouseSensitivity.Value = (int)mouseSensitivityValue;
                            }
                            else
                            {
                                Console.WriteLine($"Error converting mouse sensitivity: {item.Value}");
                            }

                        }
                        catch
                        {



                        }
                    }
                    else if (item.Key == "mouseWheelSensitivity")
                    {
                        try
                        {

                            if (float.TryParse(item.Value, out float scrollSensitivityValue))
                            {
                                lbScrollSensitivity.Text = $"Scroll Sensitivity ({scrollSensitivityValue})";
                                trbScrollSensitivity.Value = (int)scrollSensitivityValue;
                            }
                            else
                            {
                                Console.WriteLine($"Error converting scroll sensitivity: {item.Value}");
                            }

                        }
                        catch
                        {



                        }
                    }
                    else if (item.Key == "renderDistance")
                    {
                        try
                        {

                            if (float.TryParse(item.Value, out float renderDistanceValue))
                            {
                                lbRenderDistance.Text = $"Render Distance ({renderDistanceValue})";
                                trbRenderDistance.Value = (int)renderDistanceValue;
                            }
                            else
                            {
                                Console.WriteLine($"Error converting render distance: {item.Value}");
                            }

                        }
                        catch
                        {



                        }
                    }
                    else if (item.Key == "simulationDistance")
                    {
                        try
                        {

                            if (float.TryParse(item.Value, out float simulationDistanceValue))
                            {
                                lbSimulationDistance.Text = $"Simulation Distance ({simulationDistanceValue})";
                                trbSimulationDistance.Value = (int)simulationDistanceValue;
                            }
                            else
                            {
                                Console.WriteLine($"Error converting simulation distance: {item.Value}");
                            }

                        }
                        catch
                        {



                        }
                    }
                    else if (item.Key == "entityDistanceScaling")
                    {
                        try
                        {

                            if (float.TryParse(item.Value, out float entityDistanceScalingValue))
                            {
                                lbEntityDistance.Text = $"Entity Distance ({entityDistanceScalingValue})";
                                trbEntityDistance.Value = (int)entityDistanceScalingValue;
                            }
                            else
                            {
                                Console.WriteLine($"Error converting entity distance scaling: {item.Value}");
                            }

                        }
                        catch
                        {



                        }
                    }
                    else if (item.Key == "maxFps")
                    {
                        try
                        {

                            if (float.TryParse(item.Value, out float maxFpsValue))
                            {
                                lbMaxFPS.Text = $"Max FPS ({maxFpsValue})";
                                trbMaxFPS.Value = (int)maxFpsValue;
                            }
                            else
                            {
                                Console.WriteLine($"Error converting max FPS setting: {item.Value}");
                            }

                        }
                        catch
                        {



                        }
                    }


                    // Other Settings

                    lbMinRAM.Text = $"Min. RAM Allocation ({trbMinimumRam.Value}MB)";
                    lbMaxRAM.Text = $"Max. RAM Allocation ({trbMaximumRam.Value}MB)";

                    // JVM

                    tbJVMArgs.Text = Properties.Settings.Default.JVMArgs;

                }

            }
            catch (Exception ex)
            {

                Console.WriteLine("Settings Exception: " + ex.ToString());

            }

        }

        // Other

        private void guna2PictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void MainForm_Load(object sender, EventArgs e)
        {

            

        }

        private async void btnFTMicrosoft_Click(object sender, EventArgs e)
        {

            pnFTLoginProcess.BringToFront();

            var loginHandler = JELoginHandlerBuilder.BuildDefault();
            //var session = await loginHandler.Authenticate();
            try
            {

                var session = await loginHandler.AuthenticateInteractively();
                Console.WriteLine("Logged in successfully with account " + session.Username + ".");
                lbFTLPLoginStatusBig.Text = "WELCOME, " + session.Username + " TO LEAF CLIENT!";
                lbFTLPLoginStatusSmall.Text = "YOU'LL BE REDIRECTED TO THE LAUNCHER SHORTLY.";
                lbFTLPLoginStatusBig.Left = (pnFTLoginProcess.ClientSize.Width - lbFTLPLoginStatusBig.Size.Width) / 2;
                lbFTLPLoginStatusSmall.Left = (pnFTLoginProcess.ClientSize.Width - lbFTLPLoginStatusSmall.Size.Width) / 2;

                Properties.Settings.Default.SessionInfo = session;
                Properties.Settings.Default.AccountType = "microsoft";
                Properties.Settings.Default.OfflineSession = null;
                Properties.Settings.Default.OfflineSessionUsername = null;
                Properties.Settings.Default.Save();

                lbMUsername.Text = Properties.Settings.Default.SessionInfo.Username;
                LoadAvatars();
                RedirectToLauncher.Start();

            }
            catch
            {

                Console.WriteLine("User cancelled login.");
                pnLoginForm.BringToFront();

            }
        }

        private void btnFTMJBack_Click(object sender, EventArgs e)
        {

            pnLoginForm.BringToFront();

        }

        private void btnFTOFBack_Click(object sender, EventArgs e)
        {

            pnLoginForm.BringToFront();

        }

        private void HideMessage_Tick(object sender, EventArgs e)
        {
            HideMessage.Stop();
            pnFTOFLMALOGIN.Visible = false;
        }

        private void RedirectToLauncher_Tick(object sender, EventArgs e)
        {
            RedirectToLauncher.Stop();
            pnMain.BringToFront();
        }

        private void btnMSBPlay_Click(object sender, EventArgs e)
        {
            pnMPlay.BringToFront();
        }

        private void btnOFMLogin_Click(object sender, EventArgs e)
        {
            if (tbOFMUsername.Text != string.Empty)
            {

                MSession session = MSession.CreateOfflineSession(tbOFMUsername.Text);

                if (session.CheckIsValid() == true)
                {

                    Console.WriteLine("There was a problem logging the user in. Either the username/password are wrong or the account does not exist.");
                    lbFTOFLTop.Text = "WOO-HOO!";
                    lbFTOFLBottom.Text = @"You've logged into your account successfully!
You'll be redirected to the launcher in a
moment.";
                    pnFTOFLMALOGIN.Visible = true;

                    Properties.Settings.Default.OfflineSessionUsername = tbOFMUsername.Text;
                    Properties.Settings.Default.OfflineSession = session;
                    Properties.Settings.Default.AccountType = "offline";
                    Properties.Settings.Default.Save();

                    pbMUserPreview.Image = Properties.Resources.leaf_client_fade;
                    lbMUsername.Text = Properties.Settings.Default.OfflineSessionUsername;

                    HideMessage.Start();
                    LoadAvatars();
                    RedirectToLauncher.Start();

                }
                else
                {

                    Console.WriteLine("There was a problem logging the user in.");
                    lbFTOFLTop.Text = "UH-OH!";
                    lbFTOFLBottom.Text = @"There was an issue with logging you into this
account. Please try again later.";
                    pnFTOFLMALOGIN.Visible = true;

                    HideMessage.Start();

                }

            }
        }

        private void guna2CustomGradientPanel1_Click(object sender, EventArgs e)
        {
            ShowAccountPanel();
        }

        private void pbMUserPreview_Click(object sender, EventArgs e)
        {
            ShowAccountPanel();
        }

        private void label12_Click(object sender, EventArgs e)
        {
            ShowAccountPanel();
        }

        private void lbMUsername_Click(object sender, EventArgs e)
        {
            ShowAccountPanel();
        }

        private void btnMACBack_Click(object sender, EventArgs e)
        {
            HideAccountPanel();
        }

        private void btnMACChangeName_Click(object sender, EventArgs e)
        {

            var changeForm = new ChangeSkinOrName("Name");
            changeForm.ShowDialog();
        }

        private async void btnMACResetSkin_Click(object sender, EventArgs e)
        {
            try
            {

                Mojang mojang = new Mojang(new HttpClient());
                await mojang.ResetSkin(Properties.Settings.Default.SessionInfo.UUID, Properties.Settings.Default.SessionInfo.AccessToken);

                LoadAvatars();

                lbMACSRStatus.Text = "YOUR SKIN WAS RESET!";
                lbMACSRStatus.ForeColor = Color.SeaGreen;
                lbMACSRStatus.Left = (pnMAccountInfo.ClientSize.Width - lbMACSRStatus.Size.Width) / 2;
                lbMACSRStatus.Visible = true;
                lbMACSRStatus2.Text = "(COULD TAKE A WHILE TO UPDATE)";
                lbMACSRStatus2.ForeColor = Color.SeaGreen;
                lbMACSRStatus2.Left = (pnMAccountInfo.ClientSize.Width - lbMACSRStatus2.Size.Width) / 2;
                lbMACSRStatus2.Visible = true;


            }
            catch
            {

                lbMACSRStatus.Text = "THERE WAS AN ISSUE RESETTING YOUR SKIN.";
                lbMACSRStatus.ForeColor = Color.Firebrick;
                lbMACSRStatus.Left = (pnMAccountInfo.ClientSize.Width - lbMACSRStatus.Size.Width) / 2;
                lbMACSRStatus.Visible = true;
                lbMACSRStatus2.Text = "(YOU NEED AN XBOX ACCOUNT)";
                lbMACSRStatus2.ForeColor = Color.Firebrick;
                lbMACSRStatus2.Left = (pnMAccountInfo.ClientSize.Width - lbMACSRStatus2.Size.Width) / 2;
                lbMACSRStatus2.Visible = true;

            }
        }

        private void btnMACChangeSkin_Click(object sender, EventArgs e)
        {

            var changeForm = new ChangeSkinOrName("Skin");
            changeForm.ShowDialog();
        }

        private void btnMACLogout_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.SessionInfo = null;
            Properties.Settings.Default.OfflineSession = null;
            Properties.Settings.Default.OfflineSessionUsername = null;
            Properties.Settings.Default.Save();

            session = null;

            pnLoginForm.BringToFront();
            HideAccountPanel();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
        }

        private void RandomizePlayerAvatarStyle_Tick(object sender, EventArgs e)
        {

            LoadAvatars();

        }

        private void btnMInstagram_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.instagram.com/leafclient/");
        }

        private void btnMDiscord_Click(object sender, EventArgs e)
        {
            Process.Start("https://direct-link.net/420207/leafclient-discord-server");
        }

        private void checkIfSkinChanged_Tick(object sender, EventArgs e)
        {
            if (CrossAccessible.reloadSkin == true)
            {

                CrossAccessible.reloadSkin = false;

                LoadAvatars();

            }
        }

        private void pbAJHypixel_Click(object sender, EventArgs e)
        {
            CrossAccessible.ServerAutoJoinIP = "mc.hypixel.net";
            btnMPLaunch.PerformClick();
            pnMPlay.BringToFront();
        }

        private void pbAJPVPLegacy_Click(object sender, EventArgs e)
        {
            CrossAccessible.ServerAutoJoinIP = "play.pvplegacy.net";
            btnMPLaunch.PerformClick();
            pnMPlay.BringToFront();
        }

        private void pbAJCraftRise_Click(object sender, EventArgs e)
        {
            CrossAccessible.ServerAutoJoinIP = "play.craftrise.net";
            btnMPLaunch.PerformClick();
            pnMPlay.BringToFront();
        }

        private void pbAJMCCIsland_Click(object sender, EventArgs e)
        {
            CrossAccessible.ServerAutoJoinIP = "mh.mccisland.net";
            btnMPLaunch.PerformClick();
            pnMPlay.BringToFront();
        }

        private void pbAJ2B2T_Click(object sender, EventArgs e)
        {
            CrossAccessible.ServerAutoJoinIP = "2b2t.org";
            btnMPLaunch.PerformClick();
            pnMPlay.BringToFront();
        }

        private string outputVersion = null;

        private async void btnMPLaunch_Click(object sender, EventArgs e)
        {

            if (CrossAccessible.LaunchingGame == "yes")
            {

                var timer = new System.Windows.Forms.Timer
                {
                    Interval = 1000
                };

                timer.Tick += (sender, e) =>
                {

                    btnMPLaunch.Text = "CANCELLING LAUNCH";
                    btnMPLaunch.FillColor = Color.Firebrick;
                    btnMPLaunch.PressedColor = Color.Firebrick;
                    btnMPLaunch.HoverState.FillColor = Color.Firebrick;

                    if (p != null)
                    {
                        timer.Stop();
                        p.Kill();

                        btnMPLaunch.Text = "LAUNCH LEAF CLIENT";
                        btnMPLaunch.FillColor = Color.SeaGreen;
                        btnMPLaunch.PressedColor = Color.SeaGreen;
                        btnMPLaunch.HoverState.FillColor = Color.SeaGreen;

                    }
                    else
                    {

                        timer.Stop();

                        btnMPLaunch.Text = "LAUNCH LEAF CLIENT";
                        btnMPLaunch.FillColor = Color.SeaGreen;
                        btnMPLaunch.PressedColor = Color.SeaGreen;
                        btnMPLaunch.HoverState.FillColor = Color.SeaGreen;

                    }


                };

                timer.Start();
            }



            if (CrossAccessible.LaunchingGame == "no")
            {
                CrossAccessible.LaunchingGame = "yes";
                Console.WriteLine(chosenMode.ToString());

                CrossAccessible.FabricErrorFound = false;

                if (lbMVersion.Text.Contains("1.8") || lbMVersion.Text.Contains("1.12"))
                {

                    btnMPLaunch.Text = $"SWITCHING TO VANILLA MINECRAFT";
                    btnMPLaunch.FillColor = Color.DeepSkyBlue;
                    btnMPLaunch.PressedColor = Color.DeepSkyBlue;
                    btnMPLaunch.HoverState.FillColor = Color.DeepSkyBlue;

                    LaunchGame();

                }
                else
                {

                    if (chosenMode == addonModes.Vanilla)
                    {

                        btnMPLaunch.Text = $"LAUNCHING VANILLA {lbMVersion.Text}";
                        btnMPLaunch.FillColor = Color.DeepSkyBlue;
                        btnMPLaunch.PressedColor = Color.DeepSkyBlue;
                        btnMPLaunch.HoverState.FillColor = Color.DeepSkyBlue;


                        // update version list
                        await launcher.GetAllVersionsAsync();

                        LaunchGame();

                    }
                    else if (chosenMode == addonModes.Fabric)
                    {

                        var path = new MinecraftPath();
                        var fabricVersionLoader = new FabricLoader();
                        var fabricInstaller = new FabricInstaller(new HttpClient());
                        var fabricVersions = await fabricInstaller.GetSupportedVersionNames();

                        foreach (var v in fabricVersions)
                        {

                            btnMPLaunch.Text = $"INSTALLING FABRIC {lbMVersion.Text}";
                            btnMPLaunch.FillColor = Color.DeepSkyBlue;
                            btnMPLaunch.PressedColor = Color.DeepSkyBlue;
                            btnMPLaunch.HoverState.FillColor = Color.DeepSkyBlue;

                            var launcher = new MinecraftLauncher(path);

                            // install the latest fabric loader for 1.20.4
                            var versionName = await fabricInstaller.Install(lbMVersion.Text, path);

                            // update version list
                            await launcher.GetAllVersionsAsync();

                            SelectMods selectMods = new SelectMods();
                            selectMods.ShowDialog();

                            btnMPLaunch.Text = $"INSTALLING PERFORMANCE MODS";
                            btnMPLaunch.FillColor = Color.DeepSkyBlue;
                            btnMPLaunch.PressedColor = Color.DeepSkyBlue;
                            btnMPLaunch.HoverState.FillColor = Color.DeepSkyBlue;

                            try
                            {

                                if (CrossAccessible.Mods.Contains("Sodium"))
                                {

                                    using (WebClient client = new WebClient())
                                    {

                                        DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "sodium-fabric");


                                        if (lbMVersion.Text == "1.20.4")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/AANobbMI/versions/Wzzjm5lQ/sodium-fabric-0.5.7%2Bmc1.20.3.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\sodium-fabric-0.5.7+mc1.20.3.jar");

                                        }
                                        else if (lbMVersion.Text == "1.20.3")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/AANobbMI/versions/Wzzjm5lQ/sodium-fabric-0.5.7%2Bmc1.20.3.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\sodium-fabric-0.5.7+mc1.20.3.jar");

                                        }
                                        else if (lbMVersion.Text == "1.20.2")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/AANobbMI/versions/pmgeU5yX/sodium-fabric-mc1.20.2-0.5.5.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\sodium-fabric-mc1.20.2-0.5.5.jar");

                                        }
                                        else if (lbMVersion.Text == "1.20.1")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/Kw7Sm3Xf/versions/bzEcw9Eb/noxesium-1.1.1.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\noxesium-1.2.0.jar");


                                        }
                                        else if (lbMVersion.Text == "1.20")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/AANobbMI/versions/b4hTi3mo/sodium-fabric-mc1.19.4-0.4.10%2Bbuild.24.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\sodium-fabric-0.5.7+mc1.19.4.jar");

                                        }
                                        else if (lbMVersion.Text == "1.19.4")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/AANobbMI/versions/b4hTi3mo/sodium-fabric-mc1.19.4-0.4.10%2Bbuild.24.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\sodium-fabric-0.5.7+mc1.19.4.jar");

                                        }
                                        else if (lbMVersion.Text == "1.19.3")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/AANobbMI/versions/mc1.18.2-0.4.1/sodium-fabric-mc1.18.2-0.4.1%2Bbuild.15.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\sodium-fabric-0.5.7+mc1.18.2.jar");

                                        }
                                        else if (lbMVersion.Text == "1.19.2")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/AANobbMI/versions/mc1.18.2-0.4.1/sodium-fabric-mc1.18.2-0.4.1%2Bbuild.15.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\sodium-fabric-0.5.7+mc1.18.2.jar");

                                        }
                                        else if (lbMVersion.Text == "1.19")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/AANobbMI/versions/mc1.18.2-0.4.1/sodium-fabric-mc1.18.2-0.4.1%2Bbuild.15.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\sodium-fabric-0.5.7+mc1.18.2.jar");

                                        }
                                        else if (lbMVersion.Text == "1.18.2")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/AANobbMI/versions/mc1.18.2-0.4.1/sodium-fabric-mc1.18.2-0.4.1%2Bbuild.15.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\sodium-fabric-0.5.7+mc1.18.2.jar");

                                        }

                                    }


                                }
                                if (CrossAccessible.Mods.Contains("Noxesium"))
                                {

                                    using (WebClient client = new WebClient())
                                    {

                                        DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "noxesium");


                                        if (lbMVersion.Text == "1.20.4")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/Kw7Sm3Xf/versions/5GATAz7a/noxesium-1.2.0.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\noxesium-1.2.0.jar");

                                        }
                                        else if (lbMVersion.Text == "1.20.3")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/Kw7Sm3Xf/versions/5GATAz7a/noxesium-1.2.0.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\noxesium-1.2.0.jar");

                                        }
                                        else if (lbMVersion.Text == "1.20.2")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/Kw7Sm3Xf/versions/bzEcw9Eb/noxesium-1.1.1.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\noxesium-1.1.1.jar");

                                        }
                                        else if (lbMVersion.Text == "1.20.1")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/Kw7Sm3Xf/versions/xuV51Sqy/noxesium-1.0.3.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\noxesium-1.0.3.jar");

                                        }
                                        else if (lbMVersion.Text == "1.20")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/Kw7Sm3Xf/versions/xWaw0b6F/noxesium-0.1.9.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\noxesium-0.1.9.jar");

                                        }
                                        else if (lbMVersion.Text == "1.19.4")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/Kw7Sm3Xf/versions/5QKzTtlI/noxesium-0.1.8.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\noxesium-0.1.8.jar");

                                        }
                                        else if (lbMVersion.Text == "1.19.3")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/Kw7Sm3Xf/versions/YJ3s9buY/noxesium-0.1.6.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\noxesium-0.1.6.jar");

                                        }
                                        else if (lbMVersion.Text == "1.19.2")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/Kw7Sm3Xf/versions/WhRq6Q4n/noxesium-0.1.4.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\noxesium-0.1.4.jar");

                                        }

                                    }

                                }
                                if (CrossAccessible.Mods.Contains("Lithium"))
                                {

                                    using (WebClient client = new WebClient())
                                    {

                                        DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "lithium-fabric-mc");


                                        if (lbMVersion.Text == "1.20.4")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/gvQqBUqZ/versions/nMhjKWVE/lithium-fabric-mc1.20.4-0.12.1.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\lithium-fabric-mc1.20.4-0.12.1.jar");

                                        }
                                        else if (lbMVersion.Text == "1.20.3")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/gvQqBUqZ/versions/WzQmxYRa/lithium-fabric-mc1.20.3-0.12.1.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\lithium-fabric-mc1.20.3-0.12.1.jar");

                                        }
                                        else if (lbMVersion.Text == "1.20.2")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/gvQqBUqZ/versions/qdzL5Hkg/lithium-fabric-mc1.20.2-0.12.0.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\lithium-fabric-mc1.20.2-0.12.0.jar");

                                        }
                                        else if (lbMVersion.Text == "1.20.1")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/gvQqBUqZ/versions/ZSNsJrPI/lithium-fabric-mc1.20.1-0.11.2.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\lithium-fabric-mc1.20.1-0.11.2.jar");

                                        }
                                        else if (lbMVersion.Text == "1.20")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/gvQqBUqZ/versions/2KMrj5c1/lithium-fabric-mc1.20-0.11.2.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\lithium-fabric-mc1.20-0.11.2.jar");

                                        }
                                        else if (lbMVersion.Text == "1.19.4")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/gvQqBUqZ/versions/14hWYkog/lithium-fabric-mc1.19.4-0.11.1.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\lithium-fabric-mc1.19.4-0.11.1.jar");

                                        }
                                        else if (lbMVersion.Text == "1.19.3")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/gvQqBUqZ/versions/53cwYYb1/lithium-fabric-mc1.19.3-0.11.1.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\lithium-fabric-mc1.19.3-0.11.1.jar");

                                        }
                                        else if (lbMVersion.Text == "1.19.2")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/gvQqBUqZ/versions/m6sVgAi6/lithium-fabric-mc1.19.2-0.11.1.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\lithium-fabric-mc1.19.2-0.11.1.jar");

                                        }
                                        else if (lbMVersion.Text == "1.19.1")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/gvQqBUqZ/versions/mc1.19.1-0.8.3/lithium-fabric-mc1.19.1-0.8.3.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\lithium-fabric-mc1.19.1-0.8.3.jar");

                                        }
                                        else if (lbMVersion.Text == "1.19")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/gvQqBUqZ/versions/mc1.19-0.8.1/lithium-fabric-mc1.19-0.8.1.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\lithium-fabric-mc1.19-0.8.1.jar");

                                        }
                                        else if (lbMVersion.Text == "1.18.1")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/gvQqBUqZ/versions/mc1.18.1-0.7.8/lithium-fabric-mc1.18.1-0.7.8.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\lithium-fabric-mc1.18.1-0.7.8.jar");

                                        }
                                        else if (lbMVersion.Text == "1.18.2")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/gvQqBUqZ/versions/ALnv7Npy/lithium-fabric-mc1.18.2-0.10.3.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\lithium-fabric-mc1.18.2-0.10.3.jar");

                                        }
                                        else if (lbMVersion.Text == "1.17.1")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/gvQqBUqZ/versions/mc1.17.1-0.7.5/lithium-fabric-mc1.17.1-0.7.5.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\lithium-fabric-mc1.17.1-0.7.5.jar");

                                        }
                                        else if (lbMVersion.Text == "1.16.5")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/gvQqBUqZ/versions/mc1.16.5-0.6.6/lithium-fabric-mc1.16.5-0.6.6.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\lithium-fabric-mc1.16.5-0.6.6.jar");

                                        }

                                    }

                                }
                                if (CrossAccessible.Mods.Contains("Iris"))
                                {

                                    using (WebClient client = new WebClient())
                                    {

                                        DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "iris-mc");


                                        if (lbMVersion.Text == "1.20.4")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/YL57xq9U/versions/dtaGVXSk/iris-mc1.20.4-1.6.15.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\iris-mc1.20.4-1.6.15.jar");

                                        }
                                        else if (lbMVersion.Text == "1.20.3")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/YL57xq9U/versions/dtaGVXSk/iris-mc1.20.4-1.6.15.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\iris-mc1.20.4-1.6.15.jar");

                                        }
                                        else if (lbMVersion.Text == "1.20.2")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/YL57xq9U/versions/Cjwm9s3i/iris-mc1.20.2-1.6.14.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\iris-mc1.20.2-1.6.14.jar");

                                        }
                                        else if (lbMVersion.Text == "1.19.4")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/YL57xq9U/versions/wN6PuLPa/iris-mc1.19.4-1.6.11.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\iris-mc1.19.4-1.6.11.jar");

                                        }
                                        else if (lbMVersion.Text == "1.18.2")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/YL57xq9U/versions/ogIRhnAJ/iris-mc1.18.2-1.6.11.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\iris-mc1.18.2-1.6.11.jar");

                                        }

                                    }

                                }
                                if (CrossAccessible.Mods.Contains("Phosphor"))
                                {

                                    using (WebClient client = new WebClient())
                                    {

                                        DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "phosphor-fabric-mc");


                                        if (lbMVersion.Text == "1.19" || lbMVersion.Text == "1.19.1" || lbMVersion.Text == "1.19.2" || lbMVersion.Text == "1.19.3" || lbMVersion.Text == "1.19.4")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/hEOCdOgW/versions/mc1.19.x-0.8.1/phosphor-fabric-mc1.19.x-0.8.1.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\phosphor-fabric-mc1.19.x-0.8.1.jar");

                                        }
                                        else if (lbMVersion.Text == "1.18.1" || lbMVersion.Text == "1.18.2")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/hEOCdOgW/versions/mc1.18.x-0.8.1/phosphor-fabric-mc1.18.x-0.8.1.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\phosphor-fabric-mc1.18.x-0.8.1.jar");

                                        }
                                        else if (lbMVersion.Text == "1.17.1")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/hEOCdOgW/versions/mc1.17.x-0.8.0/phosphor-fabric-mc1.17.x-0.8.0.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\phosphor-fabric-mc1.17.x-0.8.0.jar");

                                        }
                                        else if (lbMVersion.Text == "1.16.5")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/hEOCdOgW/versions/mc1.16.2-0.8.0/phosphor-fabric-mc1.16.2-0.8.0.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\phosphor-fabric-mc1.16.2-0.8.0.jar");

                                        }

                                    }

                                }
                                if (CrossAccessible.Mods.Contains("Starlight"))
                                {

                                    using (WebClient client = new WebClient())
                                    {

                                        DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "starlight");


                                        if (lbMVersion.Text == "1.20.4")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/H8CaAYZC/versions/HZYU0kdg/starlight-1.1.3%2Bfabric.f5dcd1a.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\starlight-1.1.3%2Bfabric.f5dcd1a.jar");

                                        }
                                        else if (lbMVersion.Text == "1.20.3")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/H8CaAYZC/versions/1QrtjfDy/starlight-1.1.3%2Bfabric.0c447bf.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\starlight-1.1.3%2Bfabric.0c447bf.jar");

                                        }
                                        else if (lbMVersion.Text == "1.20.2")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/H8CaAYZC/versions/PLbxwptm/starlight-1.1.3%2Bfabric.5867eae.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\starlight-1.1.3%2Bfabric.5867eae.jar");

                                        }
                                        else if (lbMVersion.Text == "1.20" || lbMVersion.Text == "1.20.1")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/H8CaAYZC/versions/XGIsoVGT/starlight-1.1.2%2Bfabric.dbc156f.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\starlight-1.1.2%2Bfabric.dbc156f.jar");

                                        }
                                        else if (lbMVersion.Text == "1.19" || lbMVersion.Text == "1.19.1" || lbMVersion.Text == "1.19.2" || lbMVersion.Text == "1.19.3" || lbMVersion.Text == "1.19.4")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/H8CaAYZC/versions/1.1.1%2B1.19/starlight-1.1.1%2Bfabric.ae22326.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\starlight-1.1.1%2Bfabric.ae22326.jar");

                                        }
                                        else if (lbMVersion.Text == "1.18.2")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/H8CaAYZC/versions/1.0.2%2B1.18.2/starlight-1.0.2%2Bfabric.89b8d9f.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\starlight-1.0.2%2Bfabric.89b8d9f.jar");

                                        }
                                        else if (lbMVersion.Text == "1.18.1")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/H8CaAYZC/versions/Starlight%201.0.0%201.18.x/starlight-1.0.0%2Bfabric.d0a3220.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\starlight-1.0.0%2Bfabric.d0a3220.jar");

                                        }
                                        else if (lbMVersion.Text == "1.17.1")
                                        {

                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/H8CaAYZC/versions/Starlight%201.0.0%201.17.x/starlight-1.0.0%2Bfabric.73f6d37.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\starlight-1.0.0%2Bfabric.73f6d37.jar");

                                        }

                                    }

                                }

                                if (!CrossAccessible.Mods.Contains("Sodium"))
                                    DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "sodium");
                                if (!CrossAccessible.Mods.Contains("Noxesium"))
                                    DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "noxesium");
                                if (!CrossAccessible.Mods.Contains("Lithium"))
                                    DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "lithium");
                                if (!CrossAccessible.Mods.Contains("Iris"))
                                    DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "iris");
                                if (!CrossAccessible.Mods.Contains("Phosphor"))
                                    DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "phosphor");
                                if (!CrossAccessible.Mods.Contains("Starlight"))
                                    DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "starlight");

                                CrossAccessible.Mods = new List<string>();

                                DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "fabric-api");
                                DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "waveycapes-fabric");
                                DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "advancementinfo");
                                DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "EnchantmentDescriptions");
                                DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "notenoughanimations");
                                DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "loadingscreentips");
                                DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "betterstats");
                                DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "PickUpNotifier");
                                DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "lazydfu");
                                DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "BetterF3");
                                DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "cosmetica");
                                DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "appleskin");
                                DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "status-effect-bars");
                                DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "AdvancementPlaques");
                                DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "sound-physics-remastered");
                                DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "Zoomify");
                                DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "cloth-config");
                                DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "ForgeConfigAPIPort");
                                DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "Iceberg");
                                DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "PuzzlesLib");
                                DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "Bookshelf-Fabric");
                                DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "yet-another-config-lib");
                                DeleteFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\mods\\", "fabric-language-kotlin");

                                if (lbMVersion.Text == "1.20.4")
                                {
                                    using (WebClient client = new WebClient())
                                    {

                                        if (!FindFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\", "fabric-api"))
                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/P7dR8mSH/versions/cpC3P6YE/fabric-api-0.95.4%2B1.20.4.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\fabric-api-0.95.4%2B1.20.4.jar");
                                        if (!FindFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\", "cloth-config"))
                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/9s6osm5g/versions/eBZiZ9NS/cloth-config-13.0.121-fabric.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\cloth-config-13.0.121-fabric.jar");
                                        if (!FindFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\", "ForgeConfigAPIPort"))
                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/ohNO6lps/versions/xbVGsTLe/ForgeConfigAPIPort-v20.4.3-1.20.4-Fabric.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\ForgeConfigAPIPort-v20.4.3-1.20.4-Fabric.jar");
                                        if (!FindFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\", "Iceberg"))
                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/5faXoLqX/versions/ZioCfzuX/Iceberg-1.20.4-fabric-1.1.18.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\Iceberg-1.20.4-fabric-1.1.18.jar");
                                        if (!FindFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\", "PuzzlesLib"))
                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/QAGBst4M/versions/Hd24Sjqn/PuzzlesLib-v20.4.17-1.20.4-Fabric.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\PuzzlesLib-v20.4.17-1.20.4-Fabric.jar");
                                        if (!FindFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\", "Bookshelf"))
                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/uy4Cnpcm/versions/wj2Iiewg/Bookshelf-Fabric-1.20.4-23.0.2.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\Bookshelf-Fabric-1.20.4-23.0.2.jar");
                                        if (!FindFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\", "yet-another-config"))
                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/1eAoo2KR/versions/StXMrAsz/yet-another-config-lib-fabric-3.3.2%2B1.20.4.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\yet-another-config-lib-fabric-3.3.2%2B1.20.4.jar");
                                        if (!FindFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\", "fabric-language-kotlin"))
                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/Ha28R6CL/versions/JjrWZ7m8/fabric-language-kotlin-1.10.17%2Bkotlin.1.9.22.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\fabric-language-kotlin-1.10.17%2Bkotlin.1.9.22.jar");
                                        if (!FindFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\", "waveycapes"))
                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/kYuIpRLv/versions/b2TTz9XR/waveycapes-fabric-1.4.2-mc1.20.4.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\waveycapes-fabric-1.4.2-mc1.20.4.jar");
                                        if (!FindFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\", "advancementinfo"))
                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/G1epq3jN/versions/gq5isc7f/advancementinfo-1.20.4-fabric0.91.2-1.4.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\advancementinfo-1.20.4-fabric0.91.2-1.4.jar");
                                        if (!FindFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\", "EnchantmentDescriptions"))
                                            client.DownloadFile(new System.Uri($"https://www.curseforge.com/api/v1/mods/250419/files/4989082/download"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\EnchantmentDescriptions-Fabric-1.20.4-20.0.1.jar");
                                        if (!FindFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\", "notenoughanimations"))
                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/MPCX6s5C/versions/4e9kpBqk/notenoughanimations-fabric-1.7.0-mc1.20.4.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\notenoughanimations-fabric-1.7.0-mc1.20.4.jar");
                                        if (!FindFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\", "loadingscreentips"))
                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/9iE55lp5/versions/mfe4BtpH/loadingscreentips-1.3.4.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\loadingscreentips-1.3.4.jar");
                                        if (!FindFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\", "betterstats"))
                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/n6PXGAoM/versions/1LjgPqG5/betterstats-3.8%2Bfabric-1.20.4.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\betterstats-3.8%2Bfabric-1.20.4.jar");
                                        if (!FindFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\", "PickUpNotifier"))
                                            client.DownloadFile(new System.Uri($"https://www.curseforge.com/api/v1/mods/351441/files/5058676/download"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\PickUpNotifier-v20.4.1-1.20.4-Fabric.jar");
                                        if (!FindFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\", "moreculling"))
                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/51shyZVL/versions/KpriJ15b/moreculling-1.20.4-0.22.1.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\moreculling-1.20.4-0.22.1.jar");
                                        if (!FindFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\", "lazydfu"))
                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/hvFnDODi/versions/0.1.3/lazydfu-0.1.3.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\lazydfu-0.1.3.jar");
                                        if (!FindFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\", "BetterF3"))
                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/8shC1gFX/versions/jwhdsLMc/BetterF3-9.0.0-Forge-1.20.4.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\BetterF3-9.0.0-Forge-1.20.4.jar");
                                        if (!FindFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\", "cosmetica"))
                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/s9hF9QGp/versions/HNRBmXaG/cosmetica-1.20.4-1.2.7.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\cosmetica-1.20.4-1.2.7.jar");
                                        if (!FindFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\", "appleskin"))
                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/EsAfCjCV/versions/FupqKtcB/appleskin-neoforge-mc1.20.4-2.5.1.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\appleskin-neoforge-mc1.20.4-2.5.1.jar");
                                        if (!FindFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\", "status-effect"))
                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/x02cBj9Y/versions/i7dHnAbG/status-effect-bars-1.0.4.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\status-effect-bars-1.0.4.jar");
                                        if (!FindFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\", "AdvancementPlaques"))
                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/9NM0dXub/versions/wDGTVjTo/AdvancementPlaques-1.20.4-fabric-1.5.1.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\AdvancementPlaques-1.20.4-fabric-1.5.1.jar");
                                        if (!FindFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\", "sound-physics-remastered"))
                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/qyVF9oeo/versions/th5AIucC/sound-physics-remastered-fabric-1.20.4-1.3.1.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\sound-physics-remastered-fabric-1.20.4-1.3.1.jar");
                                        if (!FindFileWithSubstring(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\", "Zoomify"))
                                            client.DownloadFile(new System.Uri($"https://cdn.modrinth.com/data/w7ThoJFB/versions/JiEpJuon/Zoomify-2.13.0.jar"), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\.minecraft\\mods\\Zoomify-2.13.0.jar");

                                    }
                                }


                                LaunchGame();

                            }
                            catch (Exception ex)
                            {

                                btnMPLaunch.Text = $"OOPS! SOMETHING WENT WRONG.";
                                btnMPLaunch.FillColor = Color.Firebrick;
                                btnMPLaunch.PressedColor = Color.Firebrick;
                                btnMPLaunch.HoverState.FillColor = Color.Firebrick;

                                Console.WriteLine(ex);

                            }
                        }

                    }
                    else if (chosenMode == addonModes.Quilt)
                    {

                        btnMPLaunch.Text = $"LAUNCHING QUILT {lbMVersion.Text}";
                        btnMPLaunch.FillColor = Color.DeepSkyBlue;
                        btnMPLaunch.PressedColor = Color.DeepSkyBlue;
                        btnMPLaunch.HoverState.FillColor = Color.DeepSkyBlue;

                        var path = new MinecraftPath();

                        var quiltInstaller = new QuiltInstaller(new HttpClient());

                        // install the latest quilt loader for 1.20.4
                        var versionName = await quiltInstaller.Install(lbMVersion.Text, path);
                        outputVersion = versionName;

                        LaunchGame();

                    }
                    else if (chosenMode == addonModes.LiteLoader)
                    {

                        btnMPLaunch.Text = $"LAUNCHING LITELOADER {lbMVersion.Text}";
                        btnMPLaunch.FillColor = Color.DeepSkyBlue;
                        btnMPLaunch.PressedColor = Color.DeepSkyBlue;
                        btnMPLaunch.HoverState.FillColor = Color.DeepSkyBlue;

                        var path = new MinecraftPath();
                        var version = "1.7.10";

                        var liteLoaderInstaller = new LiteLoaderInstaller(new HttpClient());
                        var loaders = await liteLoaderInstaller.GetAllLiteLoaders();
                        var loaderToInstall = loaders.First(loader => loader.BaseVersion == version);

                        var installedVersion = await liteLoaderInstaller.Install(
                            loaderToInstall,
                            await launcher.GetVersionAsync(version),
                            path);

                        outputVersion = installedVersion;

                        LaunchGame();


                    }
                    else if (chosenMode == addonModes.Forge)
                    {

                        btnMPLaunch.Text = $"LAUNCHING FORGE {lbMVersion.Text}";
                        btnMPLaunch.FillColor = Color.DeepSkyBlue;
                        btnMPLaunch.PressedColor = Color.DeepSkyBlue;
                        btnMPLaunch.HoverState.FillColor = Color.DeepSkyBlue;

                        var versionLoader = new ForgeVersionLoader(new HttpClient());
                        var versions = await versionLoader.GetForgeVersions(lbMVersion.Text);
                        var recommendedVersion = versions.First(v => v.IsRecommendedVersion);

                        var forge = new ForgeInstaller(launcher);
                        await forge.Install(recommendedVersion.MinecraftVersionName, recommendedVersion.ForgeVersionName);

                        outputVersion = recommendedVersion.ForgeVersionName;

                        LaunchGame();

                    }

                }

                return;

            }
        }

        private void ChangeMinecraftName_Tick(object sender, EventArgs e)
        {
            if (p != null)
            {

                if (p.HasExited == true)
                {

                    p = null;
                    CrossAccessible.LaunchingGame = "no";
                    btnMPLaunch.Text = "LAUNCH LEAF CLIENT";
                    btnMPLaunch.FillColor = Color.SeaGreen;
                    btnMPLaunch.PressedColor = Color.SeaGreen;
                    btnMPLaunch.HoverState.FillColor = Color.SeaGreen;
                    gameLog.Close();
                }


            }

            try
            {

                if (p != null)
                {

                    if (IsWindowVisible(p.MainWindowHandle) == true)
                    {

                        SetWindowText(p.MainWindowHandle, "LEAF CLIENT — Minecraft");
                        IntPtr customIconHandle = LoadImage(IntPtr.Zero, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\.minecraft\\LCP.ico", IMAGE_ICON, 0, 0, LR_LOADFROMFILE);
                        SendMessage(p.MainWindowHandle, WM_SETICON, new IntPtr(ICON_SMALL), customIconHandle);
                        SendMessage(p.MainWindowHandle, WM_SETICON, new IntPtr(ICON_BIG), customIconHandle);
                        // Get target process window handle

                        IntPtr targetHandle = p.MainWindowHandle; // Obtain target process handle;

                        // if (isAlreadyModded == false)
                        // {
                        // isAlreadyModded = true;
                        // overlayForm.Show();
                        //  SetParent(overlayForm.Handle, targetHandle);
                        //  Console.WriteLine("Mod implemented");
                        // }


                    }

                    if (p != null && p.MainWindowHandle != IntPtr.Zero)
                    {

                        btnMPLaunch.Text = "PLAYING ON LEAF CLIENT";
                        btnMPLaunch.FillColor = Color.DeepSkyBlue;
                        btnMPLaunch.PressedColor = Color.DeepSkyBlue;
                        btnMPLaunch.HoverState.FillColor = Color.DeepSkyBlue;

                        if (discordSet == false)
                        {

                            discordSet = true;

                            CrossAccessible.client.SetPresence(new RichPresence()
                            {
                                Details = CrossAccessible.checkPlayer(),
                                State = "Playing Minecraft: Java Edition",
                                Assets = new Assets()
                                {
                                    LargeImageKey = "https://raw.githubusercontent.com/LeafClientMC/LeafClient/main/Leaf%20Client.png",
                                    LargeImageText = "Leaf Client",
                                    SmallImageKey = $"{CrossAccessible.chosenPreview}",
                                    SmallImageText = "Minecraft: Java Edition"
                                },

                                Timestamps = Timestamps.Now,

                                Buttons = new DiscordRPC.Button[]
                {

                                                new DiscordRPC.Button { Label = "Play on Leaf Client", Url = "https://www.github.com/LeafClientMC/LeafClient" }


                },
                            });

                        }

                    }

                }

            }
            catch
            {



            }
        }

        private void btnFTOfflineMode_Click(object sender, EventArgs e)
        {
            pnFTOfflineLogin.BringToFront();
        }

        private void btnMSHYP_Click(object sender, EventArgs e)
        {
            CrossAccessible.ServerAutoJoinIP = "mc.hypixel.net";

            btnMPLaunch.PerformClick();
            pnMPlay.BringToFront();
        }

        private void btnMSPVP_Click(object sender, EventArgs e)
        {
            CrossAccessible.ServerAutoJoinIP = "play.pvplegacy.net";

            btnMPLaunch.PerformClick();
            pnMPlay.BringToFront();
        }

        private void btnMS2B2_Click(object sender, EventArgs e)
        {
            CrossAccessible.ServerAutoJoinIP = "2b2t.org";

            btnMPLaunch.PerformClick();
            pnMPlay.BringToFront();
        }

        private void btnMSBSettings_Click_1(object sender, EventArgs e)
        {


            // Load all settings before showing
            LoadGameSettings();

            if (!string.IsNullOrEmpty(Properties.Settings.Default.LastServerPlayedIP))
            {

                using (WebClient Game = new WebClient())
                {

                    lbMSLSIP.Text = Properties.Settings.Default.LastServerPlayedIP;

                    try
                    {

                        string json = Game.DownloadString($"https://api.mcstatus.io/v2/status/java/{Properties.Settings.Default.LastServerPlayedIP}");

                        dynamic data = JObject.Parse(json);
                        string Status = data.online;
                        string Players = data.players.online;
                        string MaxPlayers = data.players.max;
                        string MOTD = data.motd.clean;
                        string Version = data.version.name_clean;

                        if (Int32.Parse(Players) > 0)
                        {

                            lbMSLSS.Text = $"ONLINE";
                            lbMSLSS.ForeColor = Color.SeaGreen;

                        }
                        else
                        {

                            lbMSLSS.Text = $"OFFLINE";
                            lbMSLSS.ForeColor = Color.Firebrick;

                        }

                        lbMSLSP.Text = $"{Players}/{MaxPlayers}";

                        lbMSLSMOTD.Text = $"{MOTD}";
                        Console.WriteLine(MOTD);

                        Version = Version.Replace("Requires MC ", "");
                        Version = Version.Replace("Velocity ", "");
                        Version = Version.Replace(" / ", "-");

                        lbMSLSVersion.Text = $"{Version}";
                        lbMSLSVersion.Left = (pnMSRecenter.ClientSize.Width - lbMSLSVersion.Size.Width) / 2;

                    }
                    catch
                    {



                    }

                    try
                    {

                        lbMSLSPC.Load($"https://eu.mc-api.net/v3/server/favicon/{Properties.Settings.Default.LastServerPlayedIP}");

                    }
                    catch
                    {

                        Console.WriteLine("Server no longer exists or is offline");

                    }

                    lbMSLS.Visible = true;
                    pnMSLS.Visible = true;
                }

            }

            pnMSettings.BringToFront();

        }

        private void trbMouseSensitivity_Scroll(object sender, ScrollEventArgs e)
        {

            lbMouseSensitivity.Text = $"Mouse Sensitivity ({trbMouseSensitivity.Value.ToString()})";

            optionFile.SetRawValue("mouseSensitivity", trbMouseSensitivity.Value.ToString());
            optionFile.Save();
        }

        private void trbScrollSensitivity_Scroll(object sender, ScrollEventArgs e)
        {

            lbScrollSensitivity.Text = $"Scroll Sensitivity ({trbScrollSensitivity.Value.ToString()})";

            optionFile.SetRawValue("mouseWheelSensitivity", trbScrollSensitivity.Value.ToString());
            optionFile.Save();
        }

        private void trbRenderDistance_Scroll(object sender, ScrollEventArgs e)
        {

            lbRenderDistance.Text = $"Render Distance ({trbRenderDistance.Value.ToString()})";

            optionFile.SetRawValue("renderDistance", trbRenderDistance.Value.ToString());
            optionFile.Save();
        }

        private void trbSimulationDistance_Scroll(object sender, ScrollEventArgs e)
        {

            lbSimulationDistance.Text = $"Simulation Distance ({trbSimulationDistance.Value.ToString()})";

            optionFile.SetRawValue("simulationDistance", trbSimulationDistance.Value.ToString());
            optionFile.Save();
        }

        private void trbEntityDistance_Scroll(object sender, ScrollEventArgs e)
        {

            lbEntityDistance.Text = $"Entity Distance ({trbEntityDistance.Value.ToString()})";

            optionFile.SetRawValue("entityDistanceScaling", trbEntityDistance.Value.ToString());
            optionFile.Save();
        }

        private void trbMaxFPS_Scroll(object sender, ScrollEventArgs e)
        {

            lbMaxFPS.Text = $"Max FPS ({trbMaxFPS.Value.ToString()})";

            optionFile.SetRawValue("maxFps", trbMaxFPS.Value.ToString());
            optionFile.Save();
        }

        private void trbMinimumRam_Scroll(object sender, ScrollEventArgs e)
        {

            lbMinRAM.Text = $"Min. RAM Allocation ({trbMinimumRam.Value.ToString()}MB)";

            Properties.Settings.Default.MinimumRam = trbMinimumRam.Value;
            Properties.Settings.Default.Save();
        }

        private void trbMaximumRam_Scroll(object sender, ScrollEventArgs e)
        {

            lbMaxRAM.Text = $"Max. RAM Allocation ({trbMaximumRam.Value.ToString()}MB)";

            Properties.Settings.Default.MaximumRam = trbMaximumRam.Value;
            Properties.Settings.Default.Save();
        }

        private void btnMSTAutoJump_Click(object sender, EventArgs e)
        {

            try
            {

                foreach (var item in optionFile)
                {

                    if (item.Key == "autoJump")
                    {

                        if (item.Value.Contains("False"))
                        {

                            optionFile.SetRawValue(item.Key, "True");
                            optionFile.Save();

                            btnMSTAutoJump.Text = $"AUTOJUMP: ON";

                        }
                        else if (item.Value.Contains("True"))
                        {

                            optionFile.SetRawValue(item.Key, "False");
                            optionFile.Save();

                            btnMSTAutoJump.Text = $"AUTOJUMP: OFF";

                        }
                    }
                }

            }
            catch
            {



            }
        }

        private void btnMSTTouchScreen_Click(object sender, EventArgs e)
        {
            try
            {

                string path = System.IO.Path.Combine(gamePath.BasePath, "options.txt");
                optionFile = GameOptionsFile.ReadFile(path);

                foreach (var item in optionFile)
                {

                    if (item.Key == "touchscreen")
                    {

                        if (item.Value.Contains("false"))
                        {

                            optionFile.SetRawValue(item.Key, "true");
                            optionFile.Save();

                            btnMSTTouchScreen.Text = $"TOUCHSCREEN: ON";

                        }
                        else if (item.Value.Contains("true"))
                        {

                            optionFile.SetRawValue(item.Key, "false");
                            optionFile.Save();

                            btnMSTTouchScreen.Text = $"TOUCHSCREEN: OFF";

                        }
                    }
                }

            }
            catch
            {



            }
        }

        private void btnMSTSubtitles_Click(object sender, EventArgs e)
        {
            try
            {

                string path = System.IO.Path.Combine(gamePath.BasePath, "options.txt");
                optionFile = GameOptionsFile.ReadFile(path);

                foreach (var item in optionFile)
                {

                    if (item.Key == "showSubtitles")
                    {

                        if (item.Value.Contains("false"))
                        {

                            optionFile.SetRawValue(item.Key, "true");
                            optionFile.Save();

                            btnMSTSubtitles.Text = $"SUBTITLES: ON";

                        }
                        else if (item.Value.Contains("true"))
                        {

                            optionFile.SetRawValue(item.Key, "false");
                            optionFile.Save();

                            btnMSTSubtitles.Text = $"SUBTITLES: OFF";

                        }
                    }
                }

            }
            catch
            {



            }
        }

        private void btnMSTToggleSprint_Click(object sender, EventArgs e)
        {
            try
            {

                string path = System.IO.Path.Combine(gamePath.BasePath, "options.txt");
                optionFile = GameOptionsFile.ReadFile(path);

                foreach (var item in optionFile)
                {

                    if (item.Key == "toggleSprint")
                    {

                        if (item.Value.Contains("false"))
                        {

                            optionFile.SetRawValue(item.Key, "true");
                            optionFile.Save();

                            btnMSTToggleSprint.Text = $"TOGGLESPRINT: ON";

                        }
                        else if (item.Value.Contains("true"))
                        {

                            optionFile.SetRawValue(item.Key, "false");
                            optionFile.Save();

                            btnMSTToggleSprint.Text = $"TOGGLESPRINT: OFF";

                        }
                    }
                }

            }
            catch
            {



            }
        }

        private void btnMSTToggleCrouch_Click(object sender, EventArgs e)
        {
            try
            {

                string path = System.IO.Path.Combine(gamePath.BasePath, "options.txt");
                optionFile = GameOptionsFile.ReadFile(path);

                foreach (var item in optionFile)
                {

                    if (item.Key == "toggleCrouch")
                    {

                        if (item.Value.Contains("false"))
                        {

                            optionFile.SetRawValue(item.Key, "true");
                            optionFile.Save();

                            btnMSTToggleCrouch.Text = $"TOGGLECROUCH: ON";

                        }
                        else if (item.Value.Contains("true"))
                        {

                            optionFile.SetRawValue(item.Key, "false");
                            optionFile.Save();

                            btnMSTToggleCrouch.Text = $"TOGGLECROUCH: OFF";

                        }
                    }
                }

            }
            catch
            {



            }
        }

        private void btnMSTFullScreen_Click(object sender, EventArgs e)
        {
            try
            {

                string path = System.IO.Path.Combine(gamePath.BasePath, "options.txt");
                optionFile = GameOptionsFile.ReadFile(path);

                foreach (var item in optionFile)
                {

                    if (item.Key == "fullscreen")
                    {

                        if (item.Value.Contains("false"))
                        {

                            optionFile.SetRawValue(item.Key, "true");
                            optionFile.Save();

                            btnMSTFullScreen.Text = $"FULLSCREEN: ON";

                        }
                        else if (item.Value.Contains("true"))
                        {

                            optionFile.SetRawValue(item.Key, "false");
                            optionFile.Save();

                            btnMSTFullScreen.Text = $"FULLSCREEN: OFF";

                        }
                    }
                }

            }
            catch
            {



            }
        }

        private void btnMSTVSync_Click(object sender, EventArgs e)
        {
            try
            {

                string path = System.IO.Path.Combine(gamePath.BasePath, "options.txt");
                optionFile = GameOptionsFile.ReadFile(path);

                foreach (var item in optionFile)
                {

                    if (item.Key == "enableVsync")
                    {

                        if (item.Value.Contains("false"))
                        {

                            optionFile.SetRawValue(item.Key, "true");
                            optionFile.Save();

                            btnMSTVSync.Text = $"VSYNC: ON";

                        }
                        else if (item.Value.Contains("true"))
                        {

                            optionFile.SetRawValue(item.Key, "false");
                            optionFile.Save();

                            btnMSTVSync.Text = $"VSYNC: OFF";

                        }
                    }
                }

            }
            catch
            {



            }
        }

        private void btnMSTEntityShadows_Click(object sender, EventArgs e)
        {
            try
            {

                string path = System.IO.Path.Combine(gamePath.BasePath, "options.txt");
                optionFile = GameOptionsFile.ReadFile(path);

                foreach (var item in optionFile)
                {

                    if (item.Key == "entityShadows")
                    {

                        if (item.Value.Contains("false"))
                        {

                            optionFile.SetRawValue(item.Key, "true");
                            optionFile.Save();

                            btnMSTEntityShadows.Text = $"ENTITY SHADOWS: ON";

                        }
                        else if (item.Value.Contains("true"))
                        {

                            optionFile.SetRawValue(item.Key, "false");
                            optionFile.Save();

                            btnMSTEntityShadows.Text = $"ENTITY SHADOWS: OFF";

                        }
                    }
                }

            }
            catch
            {



            }
        }

        private void btnMSTHighContrast_Click(object sender, EventArgs e)
        {
            try
            {

                string path = System.IO.Path.Combine(gamePath.BasePath, "options.txt");
                optionFile = GameOptionsFile.ReadFile(path);

                foreach (var item in optionFile)
                {

                    if (item.Key == "highContrast")
                    {

                        if (item.Value.Contains("false"))
                        {

                            optionFile.SetRawValue(item.Key, "true");
                            optionFile.Save();

                            btnMSTHighContrast.Text = $"HIGH CONTRAST: ON";

                        }
                        else if (item.Value.Contains("true"))
                        {

                            optionFile.SetRawValue(item.Key, "false");
                            optionFile.Save();

                            btnMSTHighContrast.Text = $"HIGH CONTRAST: OFF";

                        }
                    }
                }

            }
            catch
            {



            }
        }

        private void btnMSTRenderClouds_Click(object sender, EventArgs e)
        {
            try
            {

                string path = System.IO.Path.Combine(gamePath.BasePath, "options.txt");
                optionFile = GameOptionsFile.ReadFile(path);

                foreach (var item in optionFile)
                {

                    if (item.Key == "renderClouds")
                    {

                        if (item.Value.Contains(@"""fast"""))
                        {

                            optionFile.SetRawValue(item.Key, @"""fancy""");
                            optionFile.Save();

                            btnMSTRenderClouds.Text = $"RENDER CLOUDS: FANCY";

                        }
                        else if (item.Value.Contains(@"""fancy"""))
                        {

                            optionFile.SetRawValue(item.Key, @"""off""");
                            optionFile.Save();

                            btnMSTRenderClouds.Text = $"RENDER CLOUDS: OFF";

                        }
                        if (item.Value.Contains(@"""off"""))
                        {

                            optionFile.SetRawValue(item.Key, @"""fast""");
                            optionFile.Save();

                            btnMSTRenderClouds.Text = $"RENDER CLOUDS: FAST";

                        }
                    }
                }

            }
            catch
            {



            }
        }

        private void btnMSTHat_Click(object sender, EventArgs e)
        {
            try
            {

                string path = System.IO.Path.Combine(gamePath.BasePath, "options.txt");
                optionFile = GameOptionsFile.ReadFile(path);

                foreach (var item in optionFile)
                {

                    if (item.Key == "modelPart_hat")
                    {

                        if (item.Value.Contains("false"))
                        {

                            optionFile.SetRawValue(item.Key, "true");
                            optionFile.Save();

                            btnMSTHat.Text = $"HAT: ON";

                        }
                        else if (item.Value.Contains("true"))
                        {

                            optionFile.SetRawValue(item.Key, "false");
                            optionFile.Save();

                            btnMSTHat.Text = $"HAT: OFF";

                        }
                    }
                }

            }
            catch
            {



            }
        }

        private void btnMSTCape_Click(object sender, EventArgs e)
        {
            try
            {

                string path = System.IO.Path.Combine(gamePath.BasePath, "options.txt");
                optionFile = GameOptionsFile.ReadFile(path);

                foreach (var item in optionFile)
                {

                    if (item.Key == "modelPart_cape")
                    {

                        if (item.Value.Contains("false"))
                        {

                            optionFile.SetRawValue(item.Key, "true");
                            optionFile.Save();

                            btnMSTCape.Text = $"CAPE: ON";

                        }
                        else if (item.Value.Contains("true"))
                        {

                            optionFile.SetRawValue(item.Key, "false");
                            optionFile.Save();

                            btnMSTCape.Text = $"CAPE: OFF";

                        }
                    }
                }

            }
            catch
            {



            }
        }

        private void btnMSTKJacket_Click(object sender, EventArgs e)
        {
            try
            {

                string path = System.IO.Path.Combine(gamePath.BasePath, "options.txt");
                optionFile = GameOptionsFile.ReadFile(path);

                foreach (var item in optionFile)
                {

                    if (item.Key == "modelPart_jacket")
                    {

                        if (item.Value.Contains("false"))
                        {

                            optionFile.SetRawValue(item.Key, "true");
                            optionFile.Save();

                            btnMSTKJacket.Text = $"JACKET: ON";

                        }
                        else if (item.Value.Contains("true"))
                        {

                            optionFile.SetRawValue(item.Key, "false");
                            optionFile.Save();

                            btnMSTKJacket.Text = $"JACKET: OFF";

                        }
                    }
                }

            }
            catch
            {



            }
        }

        private void btnMSTLeftSleeve_Click(object sender, EventArgs e)
        {
            try
            {

                string path = System.IO.Path.Combine(gamePath.BasePath, "options.txt");
                optionFile = GameOptionsFile.ReadFile(path);

                foreach (var item in optionFile)
                {

                    if (item.Key == "modelPart_left_sleeve")
                    {

                        if (item.Value.Contains("false"))
                        {

                            optionFile.SetRawValue(item.Key, "true");
                            optionFile.Save();

                            btnMSTLeftSleeve.Text = $"LEFT SLEEVE: ON";

                        }
                        else if (item.Value.Contains("true"))
                        {

                            optionFile.SetRawValue(item.Key, "false");
                            optionFile.Save();

                            btnMSTLeftSleeve.Text = $"LEFT SLEEVE: OFF";

                        }
                    }
                }

            }
            catch
            {



            }
        }

        private void btnMSTRightSleeve_Click(object sender, EventArgs e)
        {
            try
            {

                string path = System.IO.Path.Combine(gamePath.BasePath, "options.txt");
                optionFile = GameOptionsFile.ReadFile(path);

                foreach (var item in optionFile)
                {

                    if (item.Key == "modelPart_right_sleeve")
                    {

                        if (item.Value.Contains("false"))
                        {

                            optionFile.SetRawValue(item.Key, "true");
                            optionFile.Save();

                            btnMSTRightSleeve.Text = $"RIGHT SLEEVE: ON";

                        }
                        else if (item.Value.Contains("true"))
                        {

                            optionFile.SetRawValue(item.Key, "false");
                            optionFile.Save();

                            btnMSTRightSleeve.Text = $"RIGHT SLEEVE: OFF";

                        }
                    }
                }

            }
            catch
            {



            }
        }

        private void btnMSTLeftPant_Click(object sender, EventArgs e)
        {
            try
            {

                string path = System.IO.Path.Combine(gamePath.BasePath, "options.txt");
                optionFile = GameOptionsFile.ReadFile(path);

                foreach (var item in optionFile)
                {

                    if (item.Key == "modelPart_left_pants_leg")
                    {

                        if (item.Value.Contains("false"))
                        {

                            optionFile.SetRawValue(item.Key, "true");
                            optionFile.Save();

                            btnMSTLeftPant.Text = $"LEFT PANT: ON";

                        }
                        if (item.Value.Contains("true"))
                        {

                            optionFile.SetRawValue(item.Key, "false");
                            optionFile.Save();

                            btnMSTLeftPant.Text = $"LEFT PANT: OFF";

                        }
                    }
                }

            }
            catch
            {



            }
        }

        private void btnMSTRightPant_Click(object sender, EventArgs e)
        {
            try
            {

                string path = System.IO.Path.Combine(gamePath.BasePath, "options.txt");
                optionFile = GameOptionsFile.ReadFile(path);

                foreach (var item in optionFile)
                {

                    if (item.Key == "modelPart_right_pants_leg")
                    {

                        if (item.Value.Contains("false"))
                        {

                            optionFile.SetRawValue(item.Key, "true");
                            optionFile.Save();

                            btnMSTRightPant.Text = $"RIGHT PANT: ON";

                        }
                        else if (item.Value.Contains("true"))
                        {

                            optionFile.SetRawValue(item.Key, "false");
                            optionFile.Save();

                            btnMSTRightPant.Text = $"RIGHT PANT: OFF";

                        }
                    }
                }

            }
            catch
            {



            }
        }

        private void btnMSTMainHand_Click(object sender, EventArgs e)
        {
            try
            {

                string path = System.IO.Path.Combine(gamePath.BasePath, "options.txt");
                optionFile = GameOptionsFile.ReadFile(path);

                foreach (var item in optionFile)
                {

                    if (item.Key == "mainHand")
                    {

                        if (item.Value.Contains(@"""right"""))
                        {

                            optionFile.SetRawValue(item.Key, @"""left""");
                            optionFile.Save();

                            btnMSTMainHand.Text = $"MAIN HAND: LEFT";

                        }
                        else if (item.Value.Contains(@"""left"""))
                        {

                            optionFile.SetRawValue(item.Key, @"""right""");
                            optionFile.Save();

                            btnMSTMainHand.Text = $"MAIN HAND: RIGHT";

                        }
                    }
                }

            }
            catch
            {



            }
        }

        private void btnMSTQuickLaunch_Click(object sender, EventArgs e)
        {


            if (Properties.Settings.Default.QuickLaunch == false)
            {

                Properties.Settings.Default.QuickLaunch = true;
                Properties.Settings.Default.Save();

                btnMSTQuickLaunch.Text = $"ENABLED";

            }
            else if (Properties.Settings.Default.QuickLaunch == true)
            {

                Properties.Settings.Default.QuickLaunch = false;
                Properties.Settings.Default.Save();

                btnMSTQuickLaunch.Text = $"DISABLED";

            }

        }

        private void checkForInternet_Tick(object sender, EventArgs e)
        {

            /*if (IsInternetAvailable() == false)
            {

                pnNoInternet.BringToFront();
                checkForInternet.Stop();

            }*/

        }

        private void btnMPSodiumFabric_Click(object sender, EventArgs e)
        {

        }

        private void btnMSLSJoin_Click(object sender, EventArgs e)
        {
            CrossAccessible.ServerAutoJoinIP = Properties.Settings.Default.LastServerPlayedIP;

            btnMPLaunch.PerformClick();
            pnMPlay.BringToFront();
        }

        private void pnToolTip_Paint(object sender, PaintEventArgs e)
        {

        }

        private void checkForMinecraftStatus_Tick(object sender, EventArgs e)
        {

            if (CrossAccessible.FabricErrorFound == true)
            {

                btnMPLaunch.Text = $"FABRIC ERROR";
                btnMPLaunch.FillColor = Color.Firebrick;
                btnMPLaunch.PressedColor = Color.Firebrick;
                btnMPLaunch.HoverState.FillColor = Color.Firebrick;

            }

        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void niMainForm_Click(object sender, EventArgs e)
        {
            this.Show();
        }

        private string externalModLink = "https://modrinth.com/mod/google-chat/versions";


        private void lbChangeVersion_Click(object sender, EventArgs e)
        {


        }
        private void btnMSBCredits_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/LeafClientMC/LeafClient?tab=readme-ov-file#%EF%B8%8F-credits-to-contributors-directly-or-indirectly");
        }

        private void btnMSBChanges_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/LeafClientMC/LeafClient/releases");
        }

        private void btnSettingsBack_Click(object sender, EventArgs e)
        {
            pnMPlay.BringToFront();
        }

        private void tbChangeVersion_Leave_1(object sender, EventArgs e)
        {
            tbChangeVersion.Visible = false;
            lbMVersion.Text = tbChangeVersion.Text;
        }

        private void tbChangeVersion_KeyDown_1(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                tbChangeVersion.Visible = false;
                lbMVersion.Text = tbChangeVersion.Text;
            }
        }

        private void tbChangeVersion_KeyPress_1(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
                return;

            if (char.IsDigit(e.KeyChar))
                return;

            if (e.KeyChar == '.')
            {
                var tb = sender as Guna.UI2.WinForms.Guna2TextBox;
                int dotCount = tb.Text.Count(c => c == '.');
                if (dotCount < 2)
                    return;
            }

            e.Handled = true;
        }

        private void lbChangeVersion_Click_1(object sender, EventArgs e)
        {
            tbChangeVersion.BringToFront();
            tbChangeVersion.Visible = true;
        }

        private addonModes chosenMode = addonModes.Vanilla;

        private enum addonModes
        {
            Vanilla,
            Fabric,
            Forge,
            LiteLoader,
            Quilt
        }

        private void btnMODEVanilla_Click(object sender, EventArgs e)
        {
            btnMODEVanilla.ForeColor = Color.White;
            btnMODEFabric.ForeColor = Color.DarkGray;
            btnMODEQuilt.ForeColor = Color.DarkGray;
            btnMODELiteLoader.ForeColor = Color.DarkGray;
            btnMODEForge.ForeColor = Color.DarkGray;

            chosenMode = addonModes.Vanilla;
        }

        private void btnMODEFabric_Click(object sender, EventArgs e)
        {
            btnMODEFabric.ForeColor = Color.White;
            btnMODEVanilla.ForeColor = Color.DarkGray;
            btnMODEQuilt.ForeColor = Color.DarkGray;
            btnMODELiteLoader.ForeColor = Color.DarkGray;
            btnMODEForge.ForeColor = Color.DarkGray;

            chosenMode = addonModes.Fabric;
        }

        private void btnMODEQuilt_Click(object sender, EventArgs e)
        {
            btnMODEQuilt.ForeColor = Color.White;
            btnMODEVanilla.ForeColor = Color.DarkGray;
            btnMODEFabric.ForeColor = Color.DarkGray;
            btnMODELiteLoader.ForeColor = Color.DarkGray;
            btnMODEForge.ForeColor = Color.DarkGray;

            chosenMode = addonModes.Quilt;
        }

        private void btnMODELiteLoader_Click(object sender, EventArgs e)
        {
            btnMODELiteLoader.ForeColor = Color.White;
            btnMODEFabric.ForeColor = Color.DarkGray;
            btnMODEQuilt.ForeColor = Color.DarkGray;
            btnMODEVanilla.ForeColor = Color.DarkGray;
            btnMODEForge.ForeColor = Color.DarkGray;

            chosenMode = addonModes.LiteLoader;
        }

        private void btnMODEForge_Click(object sender, EventArgs e)
        {
            btnMODEForge.ForeColor = Color.White;
            btnMODEFabric.ForeColor = Color.DarkGray;
            btnMODEQuilt.ForeColor = Color.DarkGray;
            btnMODELiteLoader.ForeColor = Color.DarkGray;
            btnMODEVanilla.ForeColor = Color.DarkGray;

            chosenMode = addonModes.Forge;
        }
    }
}
