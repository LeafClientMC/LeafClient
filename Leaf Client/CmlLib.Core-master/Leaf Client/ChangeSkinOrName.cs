/*
 * 
 *       🌿 LeafClientMC™️ 2024
 *       All the code on this project was written by ZiAD on GitHub.
 *       Some code snippets were taken from stackoverflow.com (don't come at me, all developers do that).
 * 
 */

using MojangAPI.Model;
using MojangAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Security.Policy;
using System.IO;

namespace Leaf_Client
{
    public partial class ChangeSkinOrName : Form
    {

        SkinType skinType = SkinType.Steve;

        public ChangeSkinOrName(string NameOrSkin)
        {

            InitializeComponent();

            SetStyle(ControlStyles.OptimizedDoubleBuffer |
         ControlStyles.UserPaint |
         ControlStyles.AllPaintingInWmPaint, true);
            UpdateStyles();

            LoadAvatars();

            if (NameOrSkin == "Skin")
            {

                LoadSkin();
                pnChangeSkin.BringToFront();

            }
            else if (NameOrSkin == "Name")
            {

                lbNameJoke.Text = $"(JUST KIDDING, WE LOVE YOU {Properties.Settings.Default.SessionInfo.Username.ToUpper()}!)";
                lbNameJoke.Left = (pnChangeName.ClientSize.Width - lbNameJoke.Size.Width) / 2;
                pnChangeName.BringToFront();

            }

        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams handleParams = base.CreateParams;
                handleParams.ExStyle |= 0x02000000;
                return handleParams;
            }
        }

        public void LoadSkin()
        {

            Image slim = GetImageFromUrl("https://mc-heads.net/body/MHF_Alex");
            Image regular = GetImageFromUrl("https://mc-heads.net/body/MHF_Steve");

            btnCSRegular.Image = regular;
            btnCSSlim.Image = slim;

        }

        static Image GetImageFromUrl(string url)
        {
            using (WebClient webClient = new WebClient())
            {
                try
                {
                    // Download the image bytes
                    byte[] imageBytes = webClient.DownloadData(url);

                    // Create an Image object from the downloaded bytes
                    using (MemoryStream stream = new MemoryStream(imageBytes))
                    {
                        return Image.FromStream(stream);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error downloading image: {ex.Message}");
                    return null;
                }
            }
        }

        private void ChangeSkinOrName_Load(object sender, EventArgs e)
        {

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

        private async void LoadAvatars()
        {

            if (Properties.Settings.Default.SessionInfo != null)
            {

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

                string largePreviewSelection = PickRandomString(largePreview);

                await Task.Run(() =>
                {

                    if (largePreviewSelection == "Marching")
                        pbCHSUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/marching/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Crouching")
                        pbCHSUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/crouching/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Criss_Cross")
                        pbCHSUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/criss_cross/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Cheering")
                        pbCHSUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/cheering/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Relaxing")
                        pbCHSUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/relaxing/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Cowering")
                        pbCHSUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/cowering/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Lunging")
                        pbCHSUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/lunging/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Dungeons")
                        pbCHSUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/dungeons/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Facepalm")
                        pbCHSUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/facepalm/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Sleeping")
                        pbCHSUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/sleeping/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Archer")
                        pbCHSUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/archer/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Kicking")
                        pbCHSUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/kicking/{Properties.Settings.Default.SessionInfo.Username}/full");

                    if (largePreviewSelection == "Marching")
                        pbCHNUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/marching/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Crouching")
                        pbCHNUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/crouching/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Criss_Cross")
                        pbCHNUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/criss_cross/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Cheering")
                        pbCHNUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/cheering/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Relaxing")
                        pbCHNUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/relaxing/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Cowering")
                        pbCHNUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/cowering/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Lunging")
                        pbCHNUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/lunging/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Dungeons")
                        pbCHNUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/dungeons/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Facepalm")
                        pbCHNUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/facepalm/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Sleeping")
                        pbCHNUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/sleeping/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Archer")
                        pbCHNUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/archer/{Properties.Settings.Default.SessionInfo.Username}/full");
                    else if (largePreviewSelection == "Kicking")
                        pbCHNUserPreview.LoadAsync($"https://starlightskins.lunareclipse.studio/render/kicking/{Properties.Settings.Default.SessionInfo.Username}/full");

                    Console.WriteLine(largePreviewSelection);

                });

            }

        }

        private void btnCSSlim_Click(object sender, EventArgs e)
        {
            skinType = SkinType.Alex;
        }

        private void btnCSRegular_Click(object sender, EventArgs e)
        {
            skinType = SkinType.Steve;
        }

        private async void btnChangeSkin_Click(object sender, EventArgs e)
        {

            if (tbCHSLink.Text != string.Empty && tbCHSLink.Text.EndsWith(".png"))
            {

                Mojang mojang = new Mojang(new HttpClient());

                try
                {

                    PlayerProfile response = await mojang.ChangeSkin(Properties.Settings.Default.SessionInfo.UUID, Properties.Settings.Default.SessionInfo.AccessToken, skinType, tbCHSLink.Text);

                    var timer = new System.Windows.Forms.Timer
                    {
                        Interval = 3000
                    };

                    timer.Tick += (sender, e) =>
                    {
                        timer.Stop();

                        LoadAvatars();
                        CrossAccessible.reloadSkin = true;


                    };

                    timer.Start();

                    lbCHSStatus.Text = @"SKIN CHANGED SUCCESSFULLY.
P.S. IT MAY TAKE A FEW MINS TO UPDATE.";
                    lbCHSStatus.ForeColor = Color.SeaGreen;
                    lbCHSStatus.Visible = true;
                    Console.WriteLine("Skin changed successfully");

                }
                catch
                {


                    lbCHSStatus.Text = "COULDN'T CHANGE YOUR SKIN.";
                    lbCHSStatus.ForeColor = Color.Firebrick;
                    lbCHSStatus.Visible = true;
                    Console.WriteLine("Failed to change skin");


                }

            }
            else if (tbCHSLink.Text == string.Empty)
            {

                lbCHSStatus.Text = "INPUT A LINK FIRST.";
                lbCHSStatus.ForeColor = Color.Firebrick;
                lbCHSStatus.Visible = true;

            }
            else if (!tbCHSLink.Text.EndsWith(".png"))
            {

                lbCHSStatus.Text = "YOUR LINK MUST END WITH .PNG";
                lbCHSStatus.ForeColor = Color.Firebrick;
                lbCHSStatus.Visible = true;

            }

        }

        private async void btnCHNUsername_Click(object sender, EventArgs e)
        {

            if (tbCHNUsername.Text != string.Empty)
            {

                try
                {

                    Mojang mojang = new Mojang(new HttpClient());
                    PlayerProfile profile = await mojang.ChangeName(Properties.Settings.Default.SessionInfo.AccessToken, tbCHNUsername.Text);

                    lbCHNStatus.Text = "USERNAME CHANGED SUCCESSFULY!";
                    lbCHNStatus.ForeColor = Color.SeaGreen;
                    lbCHNStatus.Visible = true;

                }
                catch
                {

                    lbCHNStatus.Text = "YOU'VE ALREADY CHANGED YOUR NAME IN THE PAST 30 DAYS.";
                    lbCHNStatus.ForeColor = Color.Firebrick;
                    lbCHNStatus.Visible = true;

                }

            }
            else
            {

                lbCHNStatus.Text = "INPUT A NEW USERNAME FIRST.";
                lbCHNStatus.ForeColor = Color.Firebrick;
                lbCHNStatus.Visible = true;

            }

        }
    }
}
