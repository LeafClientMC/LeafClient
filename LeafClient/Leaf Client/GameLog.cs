using DiscordRPC;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Leaf_Client
{
    public partial class GameLog : Form
    {
        public GameLog(Process process)
        {
            InitializeComponent();

            process.ErrorDataReceived += Process_DataReceived;
            process.OutputDataReceived += Process_DataReceived;
            output(process.StartInfo.Arguments);
        }

        private readonly ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();

        private void Process_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
                logQueue.Enqueue(e.Data);
        }

        private void output(string msg) => logQueue.Enqueue(msg);

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (logQueue.Count == 0)
                return;

            var sb = new StringBuilder();
            while (logQueue.TryDequeue(out string msg))
            {
                sb.AppendLine(msg);
            }
            richTextBox1.AppendText(sb.ToString());
            richTextBox1.ScrollToCaret();

            CrossAccessible.gameLogs = richTextBox1.Text;
            Console.WriteLine(CrossAccessible.gameLogs);
        }

        string searchTerm = "Connecting to";
        string lastSetDiscordRPC = null;

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

        private async void modifyDiscordRPC_Tick(object sender, EventArgs e)
        {

            string searchTerm = "Connecting to";

            try
            {
                // Split the logs into lines
                string[] logLines = CrossAccessible.gameLogs.Split('\n');

                // Find the lines containing the desired text
                var connectingLines = logLines
                    .Where(line => line.Contains(searchTerm))
                    .ToList();

                if (connectingLines.Count > 0)
                {
                    // Extract the latest connecting line
                    string latestConnectingLine = connectingLines.Last();

                    // Extract the IP using a regular expression
                    string ip = ExtractIpFromLine(latestConnectingLine);

                    Console.WriteLine($"Latest connecting line: {latestConnectingLine}");
                    Console.WriteLine($"Extracted IP: {ip}");

                    if (richTextBox1.Text.Contains("[main/ERROR]: Incompatible mods found!"))
                        CrossAccessible.FabricErrorFound = true;
                    else
                        CrossAccessible.FabricErrorFound = false;

                    Properties.Settings.Default.LastServerPlayedIP = ip;
                    Properties.Settings.Default.Save();

                    if (lastSetDiscordRPC != ip)
                    {

                        Thread thread = new Thread(() =>
                        {

                            lastSetDiscordRPC = ip;

                            using (WebClient IconClient = new WebClient())
                            {

                                //string iconLink = IconClient.DownloadString($"https://eu.mc-api.net/v3/server/favicon/{ip}");
                                // I was trying to put the iconLink as tge SmallImageKey on the DiscordRPC but it's too long so it gives me an exception
                                // Would've been hella cool though to be able to see the icon of the server on someone's discord status...

                                // I decided to display the player instead of the server icon

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

                                string smallPreviewSelection = PickRandomString(smallPreview);

                                string chosenPreview = null;

                                if (Properties.Settings.Default.SessionInfo != null)
                                {

                                    if (smallPreviewSelection == "Marching (Bust)")
                                        chosenPreview = $"https://starlightskins.lunareclipse.studio/render/marching/{Properties.Settings.Default.SessionInfo.Username}/bust";
                                    else if (smallPreviewSelection == "Walking (Bust)")
                                        chosenPreview = $"https://starlightskins.lunareclipse.studio/render/walking/{Properties.Settings.Default.SessionInfo.Username}/bust";
                                    else if (smallPreviewSelection == "Cheering (Bust)")
                                        chosenPreview = $"https://starlightskins.lunareclipse.studio/render/cheering/{Properties.Settings.Default.SessionInfo.Username}/bust";
                                    else if (smallPreviewSelection == "Relaxing (Full)")
                                        chosenPreview = $"https://starlightskins.lunareclipse.studio/render/relaxing/{Properties.Settings.Default.SessionInfo.Username}/full";
                                    else if (smallPreviewSelection == "Dungeons (Bust)")
                                        chosenPreview = $"https://starlightskins.lunareclipse.studio/render/dungeons/{Properties.Settings.Default.SessionInfo.Username}/bust";
                                    else if (smallPreviewSelection == "Facepalm (Bust)")
                                        chosenPreview = $"https://starlightskins.lunareclipse.studio/render/facepalm/{Properties.Settings.Default.SessionInfo.Username}/bust";
                                    else if (smallPreviewSelection == "Sleeping (Bust)")
                                        chosenPreview = $"https://starlightskins.lunareclipse.studio/render/sleeping/{Properties.Settings.Default.SessionInfo.Username}/bust";


                                }
                                else if (Properties.Settings.Default.OfflineSession != null)
                                {

                                    if (smallPreviewSelection == "Marching (Bust)")
                                        chosenPreview = $"https://starlightskins.lunareclipse.studio/render/marching/{Properties.Settings.Default.OfflineSession.Username}/bust";
                                    else if (smallPreviewSelection == "Walking (Bust)")
                                        chosenPreview = $"https://starlightskins.lunareclipse.studio/render/walking/{Properties.Settings.Default.OfflineSession.Username}/bust";
                                    else if (smallPreviewSelection == "Cheering (Bust)")
                                        chosenPreview = $"https://starlightskins.lunareclipse.studio/render/cheering/{Properties.Settings.Default.OfflineSession.Username}/bust";
                                    else if (smallPreviewSelection == "Relaxing (Full)")
                                        chosenPreview = $"https://starlightskins.lunareclipse.studio/render/relaxing/{Properties.Settings.Default.OfflineSession.Username}/full";
                                    else if (smallPreviewSelection == "Dungeons (Bust)")
                                        chosenPreview = $"https://starlightskins.lunareclipse.studio/render/dungeons/{Properties.Settings.Default.OfflineSession.Username}/bust";
                                    else if (smallPreviewSelection == "Facepalm (Bust)")
                                        chosenPreview = $"https://starlightskins.lunareclipse.studio/render/facepalm/{Properties.Settings.Default.OfflineSession.Username}/bust";
                                    else if (smallPreviewSelection == "Sleeping (Bust)")
                                        chosenPreview = $"https://starlightskins.lunareclipse.studio/render/sleeping/{Properties.Settings.Default.OfflineSession.Username}/bust";


                                }

                                CrossAccessible.chosenPreview = chosenPreview;

                                CrossAccessible.client.SetPresence(new RichPresence()
                                {
                                    Details = CrossAccessible.checkPlayer(),
                                    State = $"on {ip} | {Properties.Settings.Default.SavedVersion}",
                                    Assets = new Assets()
                                    {
                                        LargeImageKey = "https://raw.githubusercontent.com/LeafClientMC/LeafClient/main/Leaf%20Client.png",
                                        LargeImageText = "Leaf Client",
                                        SmallImageKey = chosenPreview,
                                        SmallImageText = CrossAccessible.GetPlayerNameOnRPC()
                                    },

                                    Timestamps = Timestamps.Now,

                                    Buttons = new DiscordRPC.Button[]
            {

                                                                                                new DiscordRPC.Button { Label = "Play on Leaf Client", Url = "https://www.github.com/LeafClientMC/LeafClient" }


            },
                                });

                            }


                        });

                        thread.Start();

                    }

                }
                else
                {
                    Console.WriteLine("No connecting lines found in the log string.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

        }

        static string ExtractIpFromLine(string line)
        {
            string pattern = @"Connecting to (\S+),";
            Match match = Regex.Match(line, pattern);

            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private void GameLog_FormClosing(object sender, FormClosingEventArgs e)
        {

            CrossAccessible.client.SetPresence(new RichPresence()
            {
                Details = CrossAccessible.checkPlayer(),
                State = "In the client launcher.",
                Assets = new Assets()
                {
                    LargeImageKey = "https://raw.githubusercontent.com/LeafClientMC/LeafClient/main/Leaf%20Client.png",
                    LargeImageText = "Leaf Client",
                    SmallImageKey = "",
                    SmallImageText = ""
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
